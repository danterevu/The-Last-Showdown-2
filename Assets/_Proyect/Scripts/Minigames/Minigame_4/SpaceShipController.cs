using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceShipController : MonoBehaviour
{
    [Header("Aceleracion")]
    [Tooltip("Fuerza de empuje por segundo mientras hay input")]
    [SerializeField] private float acceleration = 14f;
    private float originalAcceleration;

    [Header("Velocidad")]
    [Tooltip("Velocidad maxima que la nave puede alcanzar")]
    [SerializeField] private float maxSpeed = 9f;
    private float originalMaxSpeed;

    [Header("Inercia / Damping")]
    [Tooltip("Coeficiente de damping mientras hay input (valor bajo = mas deslizamiento)")]
    [SerializeField] private float dragWhileMoving = 1.2f;

    [Tooltip("Coeficiente de damping al soltar el joystick (valor alto = frena mas rapido)")]
    [SerializeField] private float dragWhenIdle = 3.5f;

    [Header("Rotacion")]
    [Tooltip("Velocidad de rotacion del sprite en grados/segundo")]
    [SerializeField] private float rotationSpeed = 540f;

    [Tooltip("Offset del sprite: -90 si apunta ARRIBA, 0 si apunta a la DERECHA")]
    [SerializeField] private float rotationOffset = -90f;

    [Header("Input")]
    [Tooltip("Arrastrar aqui el asset PlayerInputActions_1 o _2")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] int playerIndex;

    [SerializeField] private bool isPlayer1 = true;

    [Header("Visuales - Rocket Sabotage")]
    [SerializeField] private ParticleSystem rocketParticles;
    [SerializeField] private GameObject explosionVfxPrefab;

    [Header("Visuales - Propulsion Normal")]
    [SerializeField] private ParticleSystem propulsionParticles;
    [SerializeField] private float minSpeedForParticles = 2f;
    [SerializeField] private float maxEmissionRate = 50f;

    [Header("Stun - Nave Cruzadora")]
    [Tooltip("Velocidad de rotacion forzada en grados/segundo durante el stun (efecto carton)")]
    [SerializeField] private float stunSpinSpeed = 720f;
    [Tooltip("Cuantos segundos antes del fin del stun empieza a desacelerarse la rotacion")]
    [SerializeField] private float stunSpinFadeTime = 0.4f;

    private Rigidbody2D rb;
    private InputAction moveAction;
    private Vector2 velocity;
    private Vector2 inputDirection;
    private bool hasInput;
    private bool isRocketSabotageActive;
    public bool isInSlowField { get; private set; }

    // Stun
    private bool isStunned = false;
    private Coroutine stunCoroutine;
    private float currentSpinSpeed = 0f;

    public bool IsStunned => isStunned;

    private void Awake()
    {
        SetupRigidbody();
        SetupInput();
        originalMaxSpeed = maxSpeed;
        originalAcceleration = acceleration;
    }

    private void Update()
    {
        if (!isStunned)
        {
            ReadInput();
            UpdateRotation();
        }
        else
        {
            // Durante el stun: girar el sprite como carton
            hasInput = false;
            transform.Rotate(0f, 0f, currentSpinSpeed * Time.deltaTime);
        }

        UpdatePropulsionParticles();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // -------------------------------------------------------------------------
    //  STUN API PUBLICA
    // -------------------------------------------------------------------------

    /// Aplica stun a la nave: la inmoviliza, la hace girar como carton.
    /// La fuerza de empuje la aplica el que llama (SpaceCrossingShip).
    public void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    /// Cancela el stun inmediatamente (por si se necesita limpiar al cambiar de zona).
    public void CancelStun()
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        EndStun();
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        currentSpinSpeed = stunSpinSpeed;
        ForceStop();

        HideAllParticles();

        float elapsed = 0f;
        float fadeStart = duration - stunSpinFadeTime;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Fade de la rotacion en los ultimos stunSpinFadeTime segundos
            if (elapsed >= fadeStart)
            {
                float t = (elapsed - fadeStart) / stunSpinFadeTime;
                currentSpinSpeed = Mathf.Lerp(stunSpinSpeed, 0f, t);
            }

            // Mientras esta stunneado, velocity = 0 (no puede moverse)
            velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;

            yield return null;
        }

        stunCoroutine = null;
        EndStun();
    }

    private void EndStun()
    {
        isStunned = false;
        currentSpinSpeed = 0f;
        // Resetear rotacion al angulo correcto segun inputDirection que tenia antes
        // (si no habia input, queda como esta, que es aceptable)
    }

    // -------------------------------------------------------------------------
    //  RESTO DEL CODIGO ORIGINAL SIN CAMBIOS
    // -------------------------------------------------------------------------

    private void UpdatePropulsionParticles()
    {
        if (propulsionParticles == null) return;

        float speed = velocity.magnitude;
        var emission = propulsionParticles.emission;

        if (speed > minSpeedForParticles && hasInput && !isStunned)
        {
            if (!propulsionParticles.isPlaying)
                propulsionParticles.Play();

            float emissionRate = Mathf.Lerp(0f, maxEmissionRate, (speed - minSpeedForParticles) / (maxSpeed - minSpeedForParticles));
            emission.rateOverTime = emissionRate;
        }
        else
        {
            if (propulsionParticles.isPlaying)
                propulsionParticles.Stop();
        }
    }

    private void OnDestroy()
    {
        moveAction?.actionMap?.Disable();
    }

    private void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
    }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRocketSabotageActive) return;

        GameObject other = collision.gameObject;
        BreakableAsteroid breakableAsteroid = other.GetComponent<BreakableAsteroid>();
        if (breakableAsteroid != null)
        {
            Destroy(other);
        }

        InteractiveAsteroid interactiveAsteroid = other.GetComponent<InteractiveAsteroid>();
        if (interactiveAsteroid != null)
        {
            Destroy(other);
        }

        SplittableObject splittable = other.GetComponent<SplittableObject>();
        if (splittable != null)
        {
            Vector2 hitDir = (other.transform.position - transform.position).normalized;
            splittable.Split(hitDir);
        }

        DieFromRocketSabotage();
    }

    private void DieFromRocketSabotage()
    {
        SlowField.RemoveShipFromAllSlowFields(this);
        ResetSpeedToOriginal();
        DeactivateRocketSabotage();

        GetComponent<Explodable>()?.Explode();

        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        int hitPlayer = isPlayer1 ? 1 : 2;
        int killerPlayer = isPlayer1 ? 2 : 1;
        SpaceMinigame.Instance?.RegisterKill(killerPlayer, hitPlayer);
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
            Debug.LogError($"[SpaceShipController] No se encontro el ActionMap '{mapName}'.");
            return;
        }

        moveAction = map.FindAction("Move");

        if (moveAction == null)
        {
            Debug.LogError($"[SpaceShipController] No se encontro la accion 'Move' en '{mapName}'.");
            return;
        }

        map.Enable();
    }

    private void ReadInput()
    {
        if (moveAction == null) return;

        Vector2 raw = Vector2.zero;

        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            Vector2 dpad = gp.dpad.ReadValue();
            raw = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
        }

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

    private void ApplyMovement()
    {
        // Si esta stunneado, no aplicar movimiento
        if (isStunned)
        {
            velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (hasInput)
            velocity += inputDirection * acceleration * Time.fixedDeltaTime;

        float drag = hasInput ? dragWhileMoving : dragWhenIdle;
        float dampingThisFrame = Mathf.Exp(-drag * Time.fixedDeltaTime);
        velocity *= dampingThisFrame;

        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        rb.linearVelocity = velocity;
    }

    private void UpdateRotation()
    {
        if (!hasInput) return;

        float targetAngle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg + rotationOffset;
        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    public Vector2 GetVelocity() => velocity;

    public void SetVelocity(Vector2 v)
    {
        velocity = v;
        rb.linearVelocity = v;
    }

    public void ForceStop()
    {
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public void HideAllParticles()
    {
        if (propulsionParticles != null)
        {
            propulsionParticles.Stop();
            propulsionParticles.Clear();
        }

        if (rocketParticles != null)
        {
            rocketParticles.Stop();
            rocketParticles.Clear();
        }
    }

    public void AddImpulse(Vector2 impulse)
    {
        velocity += impulse;
    }

    public float MaxSpeed => maxSpeed;
    public float Acceleration => acceleration;
    public void SetMaxSpeed(float value) => maxSpeed = value;
    public void SetAcceleration(float value) => acceleration = value;
    public bool IsMoving => hasInput;
    public Vector2 InputDirection => inputDirection;
    public bool IsPlayer1 => isPlayer1;

    public void SetInSlowField(bool value)
    {
        isInSlowField = value;
    }

    public void ResetSpeedToOriginal()
    {
        maxSpeed = originalMaxSpeed;
        acceleration = originalAcceleration;
        isInSlowField = false;
    }
}
