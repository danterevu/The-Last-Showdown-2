using UnityEngine;
using UnityEngine.InputSystem;

/// SETUP requerido en el Inspector:
///   - Rigidbody2D en el mismo GameObject (se configura automáticamente)
///   - inputActionAsset: arrastrar PlayerInputActions_1 o _2 según el jugador
///   - isPlayer1: true = usa map "Player1_Platform", false = "Player2_Platform"
///   - El sprite de la nave debe apuntar hacia ARRIBA en su textura (offset = -90°)
///     Si apunta a la DERECHA, cambiar rotationOffset a 0 en el Inspector

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

    [SerializeField] private bool isPlayer1 = true;

    //  ESTADO INTERNO
    
    private Rigidbody2D rb;
    private InputAction moveAction;

    // Vector de velocidad que mantenemos manualmente (no usamos rb.linearDamping)
    private Vector2 velocity;

    // Dirección snapeada a 8 ángulos discretos
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
        // Input y rotación en Update para máxima responsividad visual
        ReadInput();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        // Física en FixedUpdate para consistencia con el motor
        ApplyMovement();
    }

    private void OnDestroy()
    {
        // Siempre liberar el action map al destruir el objeto
        moveAction?.actionMap?.Disable();
    }

    //  SETUP
    
    private void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;  // espacio = sin gravedad
        rb.linearDamping = 0f;  // manejamos el damping manualmente
        rb.angularDamping = 0f;
        rb.freezeRotation = true; // la rotación la controlamos desde Transform
    }

    private void SetupInput()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError($"[SpaceShipController] {gameObject.name}: falta el InputActionAsset.");
            return;
        }

        // Buscar el mapa correcto según el jugador
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

        Vector2 raw = moveAction.ReadValue<Vector2>();
        hasInput = raw.magnitude > 0.15f; // dead zone para evitar drift de gamepad

        if (hasInput)
            inputDirection = SnapToEightDirections(raw);
        // Si no hay input, conservamos la última dirección para referencias externas
        // pero hasInput = false evita que se aplique aceleración
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
        //  1 ACELERACIÓN 
        // Solo se aplica cuando hay input activo
        // Cada frame sumas una "empujada" en la dirección actual
        // Como se acumula en velocity, la nave gana velocidad gradualmente

        if (hasInput)
        {
            velocity += inputDirection * acceleration * Time.fixedDeltaTime;
        }

        // 2 DAMPING EXPONENCIAL
        
        float drag = hasInput ? dragWhileMoving : dragWhenIdle;
        float dampingThisFrame = Mathf.Exp(-drag * Time.fixedDeltaTime);
        velocity *= dampingThisFrame;

        // 3 CLAMP DE VELOCIDAD MÁXIMA 
        // Sin esto, la nave seguiría acelerando indefinidamente
        // Usamos sqrMagnitude para evitar la raíz cuadrada en la comparación

        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        // 4 APLICAR AL RIGIDBODY 
        // Asignamos nuestra velocity mantenida manualmente al rigidbody

        rb.linearVelocity = velocity;
    }

    //  ROTACIÓN

    private void UpdateRotation()
    {
        if (!hasInput) return; // la nave mantiene su orientación al soltar

        
        float targetAngle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg
                           + rotationOffset;
        float currentAngle = transform.eulerAngles.z;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle,
                                                rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    
    //  API PÚBLICA (para el minijuego o efectos externos)
    

    /// Velocidad actual de la nave.
    public Vector2 GetVelocity() => velocity;

    /// Fuerza una velocidad específica (útil para knockback o portales).
   
    public void SetVelocity(Vector2 v)
    {
        velocity = v;
        rb.linearVelocity = v;
    }

    /// Detiene la nave completamente (útil para respawn).
    public void ForceStop()
    {
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    /// Aplica un impulso instantáneo sin reemplazar la velocidad actual.
    public void AddImpulse(Vector2 impulse)
    {
        velocity += impulse;
        // El clamp se aplicará automáticamente en el próximo FixedUpdate
    }

    /// ¿El jugador está presionando alguna dirección este frame?
    public bool IsMoving => hasInput;

    /// Dirección snapeada actual (útil para el minijuego o efectos de partículas).
    public Vector2 InputDirection => inputDirection;

    public bool IsPlayer1 => isPlayer1;
}
