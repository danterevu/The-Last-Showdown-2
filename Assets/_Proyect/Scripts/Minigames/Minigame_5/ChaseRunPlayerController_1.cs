using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// ChaseRunPlayerController
//
// Movimiento plataformero completo:
//   - Movimiento horizontal con speedMultiplier (power ups)
//   - Salto con coyote time, jump buffer y jump cut
//   - Wall slide: al presionar hacia una pared en el aire se frena y cae lento.
//       Dos valores de fricción: contacto inicial (wallInitialFriction, frenada
//       fuerte) y deslizamiento continuo (wallSlideFriction, caída lenta).
//   - Wall jump: al saltar desde una pared se imprime fuerza X e Y opuestos.
//   - Extra jump: power up que añade un salto adicional en el aire.
//   - Kill zone: si sale del borde trasero de la cámara → respawn.
//   - Shield: inmunidad temporal a la kill zone.
// ─────────────────────────────────────────────────────────────────────────────

public class ChaseRunPlayerController : MonoBehaviour
{
    // ── Movimiento ────────────────────────────────────────────────────────────

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravityScale = 4f;

    // ── Salto ─────────────────────────────────────────────────────────────────

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [Tooltip("Multiplica velocityY al soltar el botón de salto (0-1). Menor = salto más corto.")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [Tooltip("Multiplicador de gravedad extra al caer. Mayor = caída más pesada.")]
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    // ── Ground Check ──────────────────────────────────────────────────────────

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    // ── Wall Slide ────────────────────────────────────────────────────────────

    [Header("Wall Slide")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.1f;

    [Tooltip("Reducción de velocityY en el PRIMER frame de contacto con la pared. " +
             "0 = sin frenada, 1 = frena completamente. Ej: 0.8")]
    [SerializeField] private float wallInitialFriction = 0.8f;

    [Tooltip("Gravedad por frame mientras se desliza por la pared (reemplaza la gravedad normal). " +
             "Menor valor = cae más lento. Ej: 2")]
    [SerializeField] private float wallSlideGravity = 2f;

    [Tooltip("Velocidad máxima de caída en wall slide (valor negativo). Ej: -2")]
    [SerializeField] private float wallSlideMaxFallSpeed = -2f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForceX = 9f;
    [SerializeField] private float wallJumpForceY = 12f;

    // ── Input ─────────────────────────────────────────────────────────────────

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] private int playerIndex = 0;

    // ── Respawn ───────────────────────────────────────────────────────────────

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.8f;
    [SerializeField] private float invulnerableTime = 1.5f;

    // ── Debug ─────────────────────────────────────────────────────────────────

    [Header("Debug (solo lectura)")]
    [SerializeField] private bool dbgGrounded;
    [SerializeField] private bool dbgWallSliding;
    [SerializeField] private int  dbgExtraJumps;

    // ── Componentes ───────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Animator animator;

    // ── Input actions ─────────────────────────────────────────────────────────

    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector2 moveInput;

    // ── Estado de movimiento ──────────────────────────────────────────────────

    private bool isGrounded;
    private bool isWallSliding;
    private bool wasWallSliding;       // para detectar el primer frame de contacto
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private int  wallSlideSide;        // -1 = pared izquierda, +1 = pared derecha

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool  jumpHeld;
    private bool  isJumping;           // true si el salto actual fue iniciado por el jugador

    // ── Extra jump ────────────────────────────────────────────────────────────

    private int baseExtraJumps = 0;    // puede cambiar con power up
    private int extraJumpsLeft = 0;    // resetea al tocar suelo / pared

    // ── Power ups ─────────────────────────────────────────────────────────────

    private float speedMultiplier  = 1f;
    private bool  killZoneImmune   = false;

    // ── Referencias de juego ──────────────────────────────────────────────────

    private ChaseRunManager manager;
    private ChaseRunCamera  chaseCamera;
    private Vector3 spawnPoint;
    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;

    // ── Respawn / muerte ──────────────────────────────────────────────────────

    private bool isDead;
    private bool isInvulnerable;

    // ── Propiedad pública ─────────────────────────────────────────────────────

    public int PlayerNumber => playerIndex + 1;

    // ═════════════════════════════════════════════════════════════════════════
    //  Inicialización
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        col      = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()  { SetupInput(); }
    private void OnDisable() { TeardownInput(); }

    private void SetupInput()
    {
        if (inputActions == null) return;

        var map = inputActions.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"[ChaseRunPlayer] Action map '{actionMapName}' no encontrado.", this);
            return;
        }

        moveAction = map.FindAction("Move");
        jumpAction = map.FindAction("Jump");

        moveAction?.Enable();
        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled  += OnJumpCanceled;
        }
    }

    private void TeardownInput()
    {
        moveAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled  -= OnJumpCanceled;
            jumpAction.Disable();
        }
    }

    /// <summary>Llamado por ChaseRunManager al iniciar el minijuego.</summary>
    public void Initialize(ChaseRunManager mgr)
    {
        manager      = mgr;
        chaseCamera  = Object.FindFirstObjectByType<ChaseRunCamera>();
        currentPhase = ChaseRunManager.RunPhase.PhaseY;
        isDead       = false;
        isInvulnerable = false;
        speedMultiplier = 1f;
        killZoneImmune  = false;
        extraJumpsLeft  = baseExtraJumps;
    }

    /// <summary>Llamado por ChaseRunManager al cambiar de fase.</summary>
    public void OnPhaseChanged(ChaseRunManager.RunPhase newPhase)
    {
        currentPhase = newPhase;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Update — input, checks, timers, animaciones
    // ═════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (isDead) return;

        moveInput = ReadFilteredMove();

        CheckGround();
        CheckWalls();
        HandleWallSlide();

        // Coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isJumping         = false;
            extraJumpsLeft    = baseExtraJumps;   // resetear extra jumps al pisar suelo
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Al pegarse a una pared se resetea el extra jump también
        if (isWallSliding)
            extraJumpsLeft = baseExtraJumps;

        // Jump buffer
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
            TryExecuteBufferedJump();
        }

        // Flip sprite según dirección
        if (moveInput.x > 0.01f)       sr.flipX = false;
        else if (moveInput.x < -0.01f) sr.flipX = true;

        // Animaciones
        if (animator != null)
        {
            animator.SetFloat("velocityX", Mathf.Abs(moveInput.x));
            animator.SetFloat("velocityY", rb.linearVelocity.y);
            animator.SetBool("isGrounded",   isGrounded);
            animator.SetBool("isWallSliding", isWallSliding);
        }

        // Debug
        dbgGrounded    = isGrounded;
        dbgWallSliding = isWallSliding;
        dbgExtraJumps  = extraJumpsLeft;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  FixedUpdate — física
    // ═════════════════════════════════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (isDead) return;

        rb.gravityScale = gravityScale;

        // Movimiento horizontal (no aplicar si está en wall slide para no despegarlo)
        if (!isWallSliding)
        {
            rb.linearVelocity = new Vector2(
                moveInput.x * moveSpeed * speedMultiplier,
                rb.linearVelocity.y
            );
        }

        // Wall slide — reemplaza gravedad normal
        if (isWallSliding)
        {
            rb.gravityScale = 0f;   // la manejamos manualmente
            float vy = rb.linearVelocity.y;

            if (!wasWallSliding)
            {
                // Primer frame: frenada inicial fuerte si está cayendo
                if (vy < 0f)
                    vy *= (1f - wallInitialFriction);
                wasWallSliding = true;
            }
            else
            {
                // Deslizamiento continuo: aplicar wallSlideGravity reducida
                vy -= wallSlideGravity * Time.fixedDeltaTime;
            }

            vy = Mathf.Max(vy, wallSlideMaxFallSpeed);
            rb.linearVelocity = new Vector2(0f, vy);
        }
        else
        {
            wasWallSliding = false;
            ApplyBetterGravity();
        }

        // Kill zone
        if (!isInvulnerable && !killZoneImmune && !isDead && chaseCamera != null)
            CheckKillZone();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Ground & Wall checks
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckGround()
    {
        // No puede estar en el suelo si está subiendo
        if (rb.linearVelocity.y > 0.1f) { isGrounded = false; return; }

        Vector2 left  = new Vector2(col.bounds.min.x + 0.02f, col.bounds.min.y);
        Vector2 right = new Vector2(col.bounds.max.x - 0.02f, col.bounds.min.y);

        bool hitLeft  = Physics2D.Raycast(left,  Vector2.down, groundCheckDistance, groundLayer);
        bool hitRight = Physics2D.Raycast(right, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = hitLeft || hitRight;
    }

    private void CheckWalls()
    {
        // No detectar paredes en el suelo
        if (isGrounded)
        {
            isTouchingWallLeft  = false;
            isTouchingWallRight = false;
            return;
        }

        Vector2 center = col.bounds.center;
        float halfH    = col.bounds.extents.y * 0.6f;
        float halfW    = col.bounds.extents.x;

        isTouchingWallLeft =
            Physics2D.Raycast(center + Vector2.up * halfH, Vector2.left, halfW + wallCheckDistance, wallLayer) ||
            Physics2D.Raycast(center - Vector2.up * halfH, Vector2.left, halfW + wallCheckDistance, wallLayer);

        isTouchingWallRight =
            Physics2D.Raycast(center + Vector2.up * halfH, Vector2.right, halfW + wallCheckDistance, wallLayer) ||
            Physics2D.Raycast(center - Vector2.up * halfH, Vector2.right, halfW + wallCheckDistance, wallLayer);
    }

    private void HandleWallSlide()
    {
        bool pressingLeft  = moveInput.x < -0.1f && isTouchingWallLeft;
        bool pressingRight = moveInput.x >  0.1f && isTouchingWallRight;

        // Solo entrar en wall slide si está cayendo (o quieto vertical)
        isWallSliding = (pressingLeft || pressingRight) && !isGrounded && rb.linearVelocity.y <= 0.1f;

        if (isWallSliding)
            wallSlideSide = isTouchingWallLeft ? -1 : 1;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Gravedad mejorada (solo cuando NO hay wall slide)
    // ═════════════════════════════════════════════════════════════════════════

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // Caída más pesada
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && isJumping)
        {
            // Jump cut: soltar botón acorta el salto
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Saltos
    // ═════════════════════════════════════════════════════════════════════════

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsCorrectDevice(ctx.control.device)) return;
        if (isDead) return;

        jumpHeld = true;

        if (isWallSliding)
        {
            ExecuteWallJump();
        }
        else if (isGrounded || coyoteTimeCounter > 0f)
        {
            ExecuteJump();
        }
        else if (extraJumpsLeft > 0)
        {
            extraJumpsLeft--;
            ExecuteJump();
        }
        else
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        jumpHeld = false;
    }

    private void TryExecuteBufferedJump()
    {
        if (isWallSliding)
        {
            ExecuteWallJump();
            jumpBufferCounter = 0f;
        }
        else if (isGrounded || coyoteTimeCounter > 0f)
        {
            ExecuteJump();
            jumpBufferCounter = 0f;
        }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity   = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter   = 0f;
        jumpBufferCounter   = 0f;
        isJumping           = true;
    }

    private void ExecuteWallJump()
    {
        // -wallSlideSide: si estaba en la pared derecha (+1), sale hacia la izquierda (-1) y viceversa
        rb.linearVelocity = new Vector2(-wallSlideSide * wallJumpForceX, wallJumpForceY);

        isWallSliding       = false;
        wasWallSliding      = false;
        isTouchingWallLeft  = false;
        isTouchingWallRight = false;
        coyoteTimeCounter   = 0f;
        jumpBufferCounter   = 0f;
        isJumping           = true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Kill Zone & Respawn
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckKillZone()
    {
        float killBound = chaseCamera.GetKillZoneBound();

        bool dead = currentPhase == ChaseRunManager.RunPhase.PhaseY
            ? transform.position.y < killBound
            : transform.position.x < killBound;

        if (dead && !isDead)
            StartCoroutine(Respawn());
    }

    private System.Collections.IEnumerator Respawn()
    {
        isDead                = true;
        rb.linearVelocity     = Vector2.zero;
        rb.gravityScale       = 0f;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPoint;
        rb.gravityScale    = gravityScale;
        isDead             = false;

        StartCoroutine(Invulnerable());
    }

    private System.Collections.IEnumerator Invulnerable()
    {
        isInvulnerable = true;
        float elapsed  = 0f;

        while (elapsed < invulnerableTime)
        {
            sr.enabled =  !sr.enabled;
            elapsed    += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }

        sr.enabled     = true;
        isInvulnerable = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
            manager?.PlayerReachedGoal(PlayerNumber);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Input — lectura filtrada (gamepad / teclado)
    // ═════════════════════════════════════════════════════════════════════════

    private Vector2 ReadFilteredMove()
    {
        Vector2 result = Vector2.zero;

        // Gamepad asignado
        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            Vector2 dpad  = gp.dpad.ReadValue();
            result = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
            if (result.sqrMagnitude > 0.01f) return result;
        }

        // Teclado fallback
        if (Keyboard.current != null)
        {
            if (playerIndex == 0)
            {
                if (Keyboard.current.dKey.isPressed)          result.x += 1f;
                if (Keyboard.current.aKey.isPressed)          result.x -= 1f;
                if (Keyboard.current.wKey.isPressed)          result.y += 1f;
                if (Keyboard.current.sKey.isPressed)          result.y -= 1f;
            }
            else
            {
                if (Keyboard.current.rightArrowKey.isPressed) result.x += 1f;
                if (Keyboard.current.leftArrowKey.isPressed)  result.x -= 1f;
                if (Keyboard.current.upArrowKey.isPressed)    result.y += 1f;
                if (Keyboard.current.downArrowKey.isPressed)  result.y -= 1f;
            }
        }

        return result.sqrMagnitude > 0.01f ? result.normalized : Vector2.zero;
    }

    private bool IsCorrectDevice(InputDevice device)
    {
        if (device is Keyboard) return true;
        if (device is Gamepad gp) return gp == InputAssigner.GetGamepadForPlayer(playerIndex);
        return false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API pública — llamada por power ups y SpawnPointUpdater
    // ═════════════════════════════════════════════════════════════════════════

    public void SetSpawnPoint(Vector3 point)          => spawnPoint = point;
    public void ApplySpeedMultiplier(float mult)      => speedMultiplier = mult;
    public void SetKillZoneImmunity(bool immune)      => killZoneImmune = immune;

    /// <summary>Añade un extra jump disponible (power up).</summary>
    public void AddExtraJump()
    {
        baseExtraJumps++;
        extraJumpsLeft = Mathf.Max(extraJumpsLeft, baseExtraJumps);
    }

    /// <summary>Quita el extra jump al expirar el power up.</summary>
    public void RemoveExtraJump()
    {
        baseExtraJumps = Mathf.Max(0, baseExtraJumps - 1);
        extraJumpsLeft = Mathf.Min(extraJumpsLeft, baseExtraJumps);
    }
}
