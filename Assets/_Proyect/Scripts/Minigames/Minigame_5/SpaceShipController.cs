using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceShipController : MonoBehaviour
{
    //  INSPECTOR

    [Header("Aceleraci�n")]
    [Tooltip("Fuerza de empuje por segundo mientras hay input")]
    [SerializeField] private float acceleration = 14f;

    [Header("Velocidad")]
    [Tooltip("Velocidad m�xima que la nave puede alcanzar")]
    [SerializeField] private float maxSpeed = 9f;

    [Header("Inercia / Damping")]
    [Tooltip("Coeficiente de damping mientras hay input (valor bajo = m�s deslizamiento)")]
    [SerializeField] private float dragWhileMoving = 1.2f;

    [Tooltip("Coeficiente de damping al soltar el joystick (valor alto = frena m�s r�pido)")]
    [SerializeField] private float dragWhenIdle = 3.5f;

    [Header("Rotaci�n")]
    [Tooltip("Velocidad de rotaci�n del sprite en grados/segundo")]
    [SerializeField] private float rotationSpeed = 540f;

    [Tooltip("Offset del sprite: -90 si apunta ARRIBA, 0 si apunta a la DERECHA")]
    [SerializeField] private float rotationOffset = -90f;

    [Header("Input")]
    [Tooltip("Arrastrar aqu� el asset PlayerInputActions_1 o _2")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] int playerIndex;

    [SerializeField] private bool isPlayer1 = true;

    [Header("Visuales - Rocket Sabotage")]
    [SerializeField] private ParticleSystem rocketParticles;

    [Header("Rocket Sabotage - Muerte por velocidad")]
    [SerializeField] private float lethalSpeed = 15f;
    [SerializeField] private GameObject explosionVfxPrefab;

    //  ESTADO INTERNO

    private Rigidbody2D rb;
    private InputAction moveAction;

    // Vector de velocidad que mantenemos manualmente (no usamos rb.linearDamping)
    private Vector2 velocity;

    // Direcci�n snapeada a 8 angulos discretos
    private Vector2 inputDirection;

    // �Hay input este frame?
    private bool hasInput;

    private bool isRocketSabotageActive;
    public bool isInSlowField { get; private set; }

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
        CheckRocketSabotageCollision();
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

    //  ROCKET SABOTAGE

    public void ActivateRocketSabotage()
    {
        if (isRocketSabotageActive) return;
        isRocketSabotageActive = true;

        if (rocketParticles != null)
        {
            rocketParticles.Play();
        }
    }

    public void DeactivateRocketSabotage()
    {
        if (!isRocketSabotageActive) return;
        isRocketSabotageActive = false;

        if (rocketParticles != null)
        {
            rocketParticles.Stop();
        }
    }

    private void CheckRocketSabotageCollision()
    {
        if (!isRocketSabotageActive) return;

        float currentSpeed = velocity.magnitude;
        if (currentSpeed >= lethalSpeed)
        {
            // Check collision with anything (asteroides, bordes, etc.)
            // Por ahora, se muere si choca con algo con esa velocidad
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRocketSabotageActive) return;

        float currentSpeed = velocity.magnitude;
        if (currentSpeed >= lethalSpeed)
        {
            DieFromRocketSabotage();
        }
    }

    private void DieFromRocketSabotage()
    {
        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        int hitPlayer = isPlayer1 ? 1 : 2;
        int killerPlayer = isPlayer1 ? 2 : 1;
        SpaceMinigame.Instance?.RegisterKill(killerPlayer, hitPlayer);
        CameraShake.Instance?.Shake(0.2f, 0.15f);

        // Opcionalmente: destruir nave o reiniciar
        // Por ahora solo desactivamos y luego el SpaceMinigame se encarga
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
            Debug.LogError($"[SpaceShipController] No se encontr� el ActionMap '{mapName}'.");
            return;
        }

        moveAction = map.FindAction("Move");

        if (moveAction == null)
        {
            Debug.LogError($"[SpaceShipController] No se encontr� la acci�n 'Move' en '{mapName}'.");
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

    //  ROTACI�N

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

    /// Fuerza una velocidad espec�fica.
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

    /// Aplica un impulso instant�neo sin reemplazar la velocidad actual.
    public void AddImpulse(Vector2 impulse)
    {
        velocity += impulse;
    }

   

    /// Velocidad m�xima actual (puede ser modificada por efectos externos).
    public float MaxSpeed => maxSpeed;

    /// Aceleraci�n actual (puede ser modificada por efectos externos).
    public float Acceleration => acceleration;

    /// Permite a efectos externos (SlowField, etc.) cambiar la velocidad m�xima en runtime.
    public void SetMaxSpeed(float value) => maxSpeed = value;

    /// Permite a efectos externos (SlowField, etc.) cambiar la aceleraci�n en runtime.
    public void SetAcceleration(float value) => acceleration = value;

    /// �El jugador est� presionando alguna direcci�n este frame?
    public bool IsMoving => hasInput;

    /// Direcci�n snapeada actual.
    public Vector2 InputDirection => inputDirection;

    public bool IsPlayer1 => isPlayer1;

    public void SetInSlowField(bool value)
    {
        isInSlowField = value;
    }
}
