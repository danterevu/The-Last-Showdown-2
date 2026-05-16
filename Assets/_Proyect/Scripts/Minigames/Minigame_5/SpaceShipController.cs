using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceShipController : MonoBehaviour
{

    //  INSPECTOR

    [Header("Aceleración")]
    [Tooltip("Fuerza de empuje por segundo mientras hay input")]
    [SerializeField] private float acceleration = 14f;

    [Header("Velocidad")]
    [Tooltip("Velocidad máxima que la nave puede alcanzar")]
    [SerializeField] private float maxSpeed = 9f;

    [Header("Inercia / Damping")]
    [Tooltip("Coeficiente de damping mientras hay input (valor bajo = más deslizamiento)")]
    [SerializeField] private float dragWhileMoving = 1.2f;

    [Tooltip("Coeficiente de damping al soltar el joystick (valor alto = frena más rápido)")]
    [SerializeField] private float dragWhenIdle = 3.5f;

    [Header("Rotación")]
    [Tooltip("Velocidad de rotación del sprite en grados/segundo")]
    [SerializeField] private float rotationSpeed = 540f;

    [Tooltip("Offset del sprite: -90 si apunta ARRIBA, 0 si apunta a la DERECHA")]
    [SerializeField] private float rotationOffset = -90f;

    [Header("Input")]
    [Tooltip("Arrastrar aquí el asset PlayerInputActions_1 o _2")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] int playerIndex;

    [SerializeField] private bool isPlayer1 = true;

    //  ESTADO INTERNO

    private Rigidbody2D rb;
    private InputAction moveAction;

    // Vector de velocidad que mantenemos manualmente (no usamos rb.linearDamping)
    private Vector2 velocity;

    // Dirección snapeada a 8 angulos discretos
    private Vector2 inputDirection;

    // ¿Hay input este frame?
    private bool hasInput;

    //  UNITY LIFECYCLE

    private void Awake()
    {
        SetupRigidbody();
        SetupInput();
    }

    private void Update()
    {
        ReadInput();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void OnDestroy()
    {
        moveAction?.actionMap?.Disable();
    }

    //  SETUP

    private void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
    }

    private void SetupInput()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError($"[SpaceShipController] {gameObject.name}: falta el InputActionAsset.");
            return;
        }

        string mapName = isPlayer1 ? "Player1_TopDown" : "Player2_TopDown";
        var map = inputActionAsset.FindActionMap(mapName);

        if (map == null)
        {
            Debug.LogError($"[SpaceShipController] No se encontró el ActionMap '{mapName}'.");
            return;
        }

        moveAction = map.FindAction("Move");

        if (moveAction == null)
        {
            Debug.LogError($"[SpaceShipController] No se encontró la acción 'Move' en '{mapName}'.");
            return;
        }

        map.Enable();
    }

    //  INPUT

    private void ReadInput()
    {
        if (moveAction == null) return;

        Vector2 raw = Vector2.zero;

        // Leer gamepad asignado a este jugador
        //playerIndex = isPlayer1 ? 0 : 1;
        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            Vector2 dpad = gp.dpad.ReadValue();
            raw = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
        }

        // Fallback teclado si no hay input de gamepad
        if (raw.sqrMagnitude < 0.15f)
            raw = moveAction.ReadValue<Vector2>();

        hasInput = raw.magnitude > 0.15f;

        if (hasInput)
            inputDirection = SnapToEightDirections(raw);
    }

    private Vector2 SnapToEightDirections(Vector2 input)
    {
        float angleDeg = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angleDeg / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    //  MOVIMIENTO

    private void ApplyMovement()
    {
        if (hasInput)
            velocity += inputDirection * acceleration * Time.fixedDeltaTime;

        float drag = hasInput ? dragWhileMoving : dragWhenIdle;
        float dampingThisFrame = Mathf.Exp(-drag * Time.fixedDeltaTime);
        velocity *= dampingThisFrame;

        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        rb.linearVelocity = velocity;
    }

    //  ROTACIÓN

    private void UpdateRotation()
    {
        if (!hasInput) return;

        float targetAngle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg
                           + rotationOffset;
        float currentAngle = transform.eulerAngles.z;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle,
                                                rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

   

    /// Velocidad actual de la nave.
    public Vector2 GetVelocity() => velocity;

    /// Fuerza una velocidad específica.
    public void SetVelocity(Vector2 v)
    {
        velocity = v;
        rb.linearVelocity = v;
    }

    /// Detiene la nave completamente.
    public void ForceStop()
    {
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    /// Aplica un impulso instantáneo sin reemplazar la velocidad actual.
    public void AddImpulse(Vector2 impulse)
    {
        velocity += impulse;
    }

   

    /// Velocidad máxima actual (puede ser modificada por efectos externos).
    public float MaxSpeed => maxSpeed;

    /// Aceleración actual (puede ser modificada por efectos externos).
    public float Acceleration => acceleration;

    /// Permite a efectos externos (SlowField, etc.) cambiar la velocidad máxima en runtime.
    public void SetMaxSpeed(float value) => maxSpeed = value;

    /// Permite a efectos externos (SlowField, etc.) cambiar la aceleración en runtime.
    public void SetAcceleration(float value) => acceleration = value;

    /// ¿El jugador está presionando alguna dirección este frame?
    public bool IsMoving => hasInput;

    /// Dirección snapeada actual.
    public Vector2 InputDirection => inputDirection;

    public bool IsPlayer1 => isPlayer1;
}
