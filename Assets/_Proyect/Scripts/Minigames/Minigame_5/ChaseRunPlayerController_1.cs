using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// ChaseRunPlayerController  v3
//
// Cambios respecto a v2:
//   - FixedUpdate delega a BubbleRespawn cuando IsInBubble == true:
//       aplica BubbleRespawn.BubbleVelocity con gravedad = 0.
//       Así el movimiento de burbuja usa el mismo pipeline de física,
//       sin conflicto entre dos scripts tocando Rigidbody2D.
//   - SetFrozen / SetDead ya NO bloquean Update() — la flag "frozen"
//     congela física pero deja que Update() siga leyendo input.
//     (Necesario para que GetMoveInput() devuelva valores reales
//      en cuanto BubbleRespawn reactive al jugador.)
//   - Separamos isDead (muerte real, pausa todo) de isFrozen
//     (solo congela physics, mantiene input).
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
    [Tooltip("Multiplica velocityY al soltar el botón (0-1). Menor = salto más corto.")]
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [Tooltip("Multiplicador de gravedad extra al caer. Mayor = caída más pesada.")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    // ── Ground Check ──────────────────────────────────────────────────────────

    [Header("Ground Check")]
    [Tooltip("Tamaño del box de detección de suelo.")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    [Tooltip("Cuánto baja el centro del box desde el borde inferior del collider.")]
    [SerializeField] private float groundCheckOffset = 0.02f;
    [SerializeField] private LayerMask groundLayer;

    // ── Wall Check ────────────────────────────────────────────────────────────

    [Header("Wall Check")]
    [Tooltip("Distancia del raycast lateral más allá del borde del collider.")]
    [SerializeField] private float wallCheckDistance = 0.12f;

    // ── Wall Slide ────────────────────────────────────────────────────────────

    [Header("Wall Slide")]
    [SerializeField] private float wallInitialFriction = 0.75f;
    [SerializeField] private float wallSlideGravity = 2.5f;
    [SerializeField] private float wallSlideMaxFallSpeed = -2.5f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForceX = 8f;
    [SerializeField] private float wallJumpForceY = 11f;
    [SerializeField] private float wallJumpInputLockTime = 0.15f;

    // ── Input ─────────────────────────────────────────────────────────────────

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] private int playerIndex = 0;

    // ── Debug ─────────────────────────────────────────────────────────────────

    [Header("Debug (solo lectura)")]
    [SerializeField] private bool dbgGrounded;
    [SerializeField] private bool dbgWallL;
    [SerializeField] private bool dbgWallR;
    [SerializeField] private bool dbgWallSliding;
    [SerializeField] private int dbgExtraJumps;

    // ── Componentes ───────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D bodyCol;
    private Animator animator;
    private BubbleRespawn bubbleRespawn;

    // ── Input ─────────────────────────────────────────────────────────────────

    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector2 moveInput;

    // ── Estado ────────────────────────────────────────────────────────────────

    private bool isGrounded;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private bool isWallSliding;
    private bool wasWallSliding;
    private int wallSlideSide;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld;
    private bool isJumping;
    private bool jumpCutApplied;

    private float wallJumpInputLockTimer;

    // ── Extra jumps ───────────────────────────────────────────────────────────

    private int baseExtraJumps = 0;
    private int extraJumpsLeft = 0;

    // ── Power ups ─────────────────────────────────────────────────────────────

    private float speedMultiplier = 1f;
    private bool killZoneImmune = false;

    // ── Referencias ───────────────────────────────────────────────────────────

    private ChaseRunManager manager;
    private ChaseRunCamera chaseCamera;
    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;

    // ── Estado interno mejorado ───────────────────────────────────────────────
    //
    //  isDead   → muerte real (Update Y FixedUpdate pausados).
    //  isFrozen → solo congela physics; Update() sigue corriendo para leer input.
    //             BubbleRespawn usa esto durante el delay de muerte y lo libera
    //             justo antes de activar la burbuja.
    //  isInvulnerable / killZoneImmune → inmunidad a kill zone y power-ups.

    private bool isDead = false;
    private bool isFrozen = false;
    private bool isInvulnerable = false;

    // ── Propiedad pública ─────────────────────────────────────────────────────

    public int PlayerNumber => playerIndex + 1;

    // ═════════════════════════════════════════════════════════════════════════
    //  Inicialización
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        bubbleRespawn = GetComponent<BubbleRespawn>();

        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols)
            if (!c.isTrigger) { bodyCol = c; break; }

        if (bodyCol == null)
            Debug.LogError("[ChaseRunPlayer] No se encontró un Collider2D no-trigger (body).", this);
    }

    private void OnEnable() => SetupInput();
    private void OnDisable() => TeardownInput();

    private void SetupInput()
    {
        if (inputActions == null) return;
        var map = inputActions.FindActionMap(actionMapName);
        if (map == null) { Debug.LogError($"[ChaseRunPlayer] ActionMap '{actionMapName}' no encontrado."); return; }

        moveAction = map.FindAction("Move");
        jumpAction = map.FindAction("Jump");
        moveAction?.Enable();
        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;
        }
    }

    private void TeardownInput()
    {
        moveAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
            jumpAction.Disable();
        }
    }

    public void Initialize(ChaseRunManager mgr)
    {
        manager = mgr;
        chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();
        currentPhase = ChaseRunManager.RunPhase.PhaseY;
        isDead = false;
        isFrozen = false;
        isInvulnerable = false;
        speedMultiplier = 1f;
        killZoneImmune = false;
        extraJumpsLeft = baseExtraJumps;
    }

    public void OnPhaseChanged(ChaseRunManager.RunPhase newPhase) => currentPhase = newPhase;

    // ═════════════════════════════════════════════════════════════════════════
    //  Update — lee input y actualiza estado; siempre corre salvo isDead real.
    // ═════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (isDead) return;

        // Leer input siempre (incluso cuando isFrozen o IsInBubble)
        // para que GetMoveInput() tenga datos frescos para BubbleRespawn.
        moveInput = ReadFilteredMove();

        // Cuando está en burbuja, no procesar lógica de plataformero normal
        if (isFrozen || (bubbleRespawn != null && bubbleRespawn.IsInBubble)) return;

        CheckGround();
        CheckWalls();
        HandleWallSlide();

        // Coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isJumping = false;
            jumpCutApplied = false;
            extraJumpsLeft = baseExtraJumps;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isWallSliding) extraJumpsLeft = baseExtraJumps;

        if (wallJumpInputLockTimer > 0f) wallJumpInputLockTimer -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
            TryExecuteBufferedJump();
        }

        // Flip sprite (solo fuera de burbuja)
        if (moveInput.x > 0.01f) sr.flipX = false;
        else if (moveInput.x < -0.01f) sr.flipX = true;

        // Animaciones
        if (animator != null)
        {
            animator.SetFloat("velocityX", Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat("velocityY", rb.linearVelocity.y);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isWallSliding", isWallSliding);
        }

        // Debug
        dbgGrounded = isGrounded;
        dbgWallL = isTouchingWallLeft;
        dbgWallR = isTouchingWallRight;
        dbgWallSliding = isWallSliding;
        dbgExtraJumps = extraJumpsLeft;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  FixedUpdate
    // ═════════════════════════════════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (isDead || isFrozen) return;

        // ── Modo burbuja: delegar movimiento a BubbleRespawn ──────────────────
        if (bubbleRespawn != null && bubbleRespawn.IsInBubble)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = bubbleRespawn.BubbleVelocity;
            return;
        }

        // ── Modo normal ───────────────────────────────────────────────────────
        rb.gravityScale = gravityScale;

        if (isWallSliding)
            ApplyWallSlidePhysics();
        else
        {
            ApplyHorizontalMove();
            ApplyBetterGravity();
        }

        if (!isInvulnerable && !killZoneImmune && chaseCamera != null)
            CheckKillZone();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Ground Check
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckGround()
    {
        if (rb.linearVelocity.y > 0.5f) { isGrounded = false; return; }
        if (bodyCol == null) return;

        float innerMargin = 0.08f;
        float boxW = Mathf.Max(0.05f, bodyCol.bounds.size.x - innerMargin * 2f);

        Vector2 center = new Vector2(
            bodyCol.bounds.center.x,
            bodyCol.bounds.min.y - groundCheckOffset
        );

        isGrounded = Physics2D.OverlapBox(center, new Vector2(boxW, groundCheckSize.y), 0f, groundLayer) != null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Wall Check
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckWalls()
    {
        if (bodyCol == null) return;

        float halfW = bodyCol.bounds.extents.x;
        float height = bodyCol.bounds.size.y;
        Vector2 center = bodyCol.bounds.center;

        Vector2 high = center + Vector2.up * height * 0.25f;
        Vector2 mid = center;
        Vector2 low = center - Vector2.up * height * 0.25f;

        float dist = halfW + wallCheckDistance;

        if (isGrounded)
        {
            isTouchingWallLeft = Physics2D.Raycast(high, Vector2.left, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.left, dist, groundLayer);
            isTouchingWallRight = Physics2D.Raycast(high, Vector2.right, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.right, dist, groundLayer);
        }
        else
        {
            isTouchingWallLeft = Physics2D.Raycast(high, Vector2.left, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.left, dist, groundLayer)
                               || Physics2D.Raycast(low, Vector2.left, dist, groundLayer);
            isTouchingWallRight = Physics2D.Raycast(high, Vector2.right, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.right, dist, groundLayer)
                               || Physics2D.Raycast(low, Vector2.right, dist, groundLayer);
        }
    }

    // ── Wall Slide lógica ─────────────────────────────────────────────────────

    private void HandleWallSlide()
    {
        bool pressingLeft = moveInput.x < -0.1f && isTouchingWallLeft;
        bool pressingRight = moveInput.x > 0.1f && isTouchingWallRight;

        bool canSlide = (pressingLeft || pressingRight)
                     && !isGrounded
                     && rb.linearVelocity.y <= 0.1f;

        if (canSlide && !isWallSliding)
        {
            isWallSliding = true;
            wasWallSliding = false;
            wallSlideSide = isTouchingWallLeft ? -1 : 1;
        }
        else if (!canSlide)
        {
            isWallSliding = false;
            wasWallSliding = false;
        }
    }

    // ── Wall slide física ─────────────────────────────────────────────────────

    private void ApplyWallSlidePhysics()
    {
        rb.gravityScale = 0f;
        float vy = rb.linearVelocity.y;

        if (!wasWallSliding)
        {
            if (vy < 0f) vy *= (1f - wallInitialFriction);
            wasWallSliding = true;
        }
        else
        {
            vy -= wallSlideGravity * Time.fixedDeltaTime;
        }

        vy = Mathf.Max(vy, wallSlideMaxFallSpeed);
        rb.linearVelocity = new Vector2(0f, vy);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Movimiento horizontal y gravedad
    // ═════════════════════════════════════════════════════════════════════════

    private void ApplyHorizontalMove()
    {
        float inputX = (wallJumpInputLockTimer > 0f) ? 0f : moveInput.x;
        rb.linearVelocity = new Vector2(inputX * moveSpeed * speedMultiplier, rb.linearVelocity.y);
    }

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && isJumping && !jumpCutApplied)
        {
            jumpCutApplied = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Saltos
    // ═════════════════════════════════════════════════════════════════════════

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        // Ignorar salto si está en burbuja o muerto
        if (!IsCorrectDevice(ctx.control.device) || isDead || isFrozen) return;
        if (bubbleRespawn != null && bubbleRespawn.IsInBubble) return;

        jumpHeld = true;

        if (isWallSliding)
            ExecuteWallJump();
        else if (isGrounded || coyoteTimeCounter > 0f)
            ExecuteJump();
        else if (extraJumpsLeft > 0)
        {
            extraJumpsLeft--;
            ExecuteJump();
        }
        else
            jumpBufferCounter = jumpBufferTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx) => jumpHeld = false;

    private void TryExecuteBufferedJump()
    {
        if (isWallSliding) { ExecuteWallJump(); jumpBufferCounter = 0f; }
        else if (isGrounded || coyoteTimeCounter > 0f) { ExecuteJump(); jumpBufferCounter = 0f; }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isJumping = true;
        jumpCutApplied = false;
    }

    private void ExecuteWallJump()
    {
        rb.linearVelocity = new Vector2(-wallSlideSide * wallJumpForceX, wallJumpForceY);
        wallJumpInputLockTimer = wallJumpInputLockTime;

        isWallSliding = false;
        wasWallSliding = false;
        isTouchingWallLeft = false;
        isTouchingWallRight = false;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isJumping = true;
        jumpCutApplied = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Kill Zone
    // ═════════════════════════════════════════════════════════════════════════

    private void CheckKillZone()
    {
        if (isDead) return;
        if (bubbleRespawn != null && bubbleRespawn.IsInBubble) return;

        float killBound = chaseCamera.GetKillZoneBound();
        bool dead = currentPhase == ChaseRunManager.RunPhase.PhaseY
            ? transform.position.y < killBound
            : transform.position.x < killBound;

        if (dead)
        {
            if (bubbleRespawn != null)
                bubbleRespawn.TriggerRespawn();
            else
                StartCoroutine(FallbackRespawn());
        }
    }

    private IEnumerator FallbackRespawn()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        sr.enabled = false;

        yield return new WaitForSeconds(0.8f);

        if (chaseCamera != null)
        {
            float topBound = chaseCamera.GetTopBound();
            transform.position = new Vector3(chaseCamera.CenterX, topBound, 0f);
        }

        rb.gravityScale = gravityScale;
        isDead = false;
        sr.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
            manager?.PlayerReachedGoal(PlayerNumber);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Input
    // ═════════════════════════════════════════════════════════════════════════

    private Vector2 ReadFilteredMove()
    {
        Vector2 result = Vector2.zero;

        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            Vector2 dpad = gp.dpad.ReadValue();
            result = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
            if (result.sqrMagnitude > 0.01f) return result;
        }

        if (Keyboard.current != null)
        {
            if (playerIndex == 0)
            {
                if (Keyboard.current.dKey.isPressed) result.x += 1f;
                if (Keyboard.current.aKey.isPressed) result.x -= 1f;
                if (Keyboard.current.wKey.isPressed) result.y += 1f;
                if (Keyboard.current.sKey.isPressed) result.y -= 1f;
            }
            else
            {
                if (Keyboard.current.rightArrowKey.isPressed) result.x += 1f;
                if (Keyboard.current.leftArrowKey.isPressed) result.x -= 1f;
                if (Keyboard.current.upArrowKey.isPressed) result.y += 1f;
                if (Keyboard.current.downArrowKey.isPressed) result.y -= 1f;
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
    //  API pública
    // ═════════════════════════════════════════════════════════════════════════

    public void SetSpawnPoint(Vector3 point) => _ = point;
    public void ApplySpeedMultiplier(float mult) => speedMultiplier = mult;
    public void SetKillZoneImmunity(bool immune) => killZoneImmune = immune;

    // ── API para BubbleRespawn ────────────────────────────────────────────────

    /// <summary>
    /// Muerte REAL: pausa Update() y FixedUpdate() completamente.
    /// Solo usar si el jugador muere sin burbuja.
    /// </summary>
    public void SetDead(bool dead)
    {
        isDead = dead;
        if (dead)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    /// <summary>
    /// Congela physics pero MANTIENE Update() activo para leer input.
    /// BubbleRespawn usa esto durante el delay de respawn.
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (frozen)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
        else
        {
            // gravityScale lo maneja FixedUpdate según el estado actual
        }
    }

    /// <summary>Inmunidad total — kill zone y power ups negativos no afectan.</summary>
    public void SetImmune(bool immune)
    {
        isInvulnerable = immune;
        killZoneImmune = immune;
    }

    /// <summary>Expone el input actual para que BubbleRespawn calcule la velocidad.</summary>
    public Vector2 GetMoveInput() => moveInput;

    public void AddExtraJump()
    {
        baseExtraJumps++;
        extraJumpsLeft = Mathf.Max(extraJumpsLeft, baseExtraJumps);
    }

    public void RemoveExtraJump()
    {
        baseExtraJumps = Mathf.Max(0, baseExtraJumps - 1);
        extraJumpsLeft = Mathf.Min(extraJumpsLeft, baseExtraJumps);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (bodyCol == null) return;

        // Ground check box (verde)
        float innerMargin = 0.08f;
        float boxW = Mathf.Max(0.05f, bodyCol.bounds.size.x - innerMargin * 2f);
        Gizmos.color = isGrounded ? Color.green : new Color(0f, 1f, 0f, 0.4f);
        Vector3 gc = new Vector3(bodyCol.bounds.center.x, bodyCol.bounds.min.y - groundCheckOffset, 0f);
        Gizmos.DrawWireCube(gc, new Vector3(boxW, groundCheckSize.y, 0f));

        // Wall check rays
        float hw = bodyCol.bounds.extents.x + wallCheckDistance;
        float height = bodyCol.bounds.size.y;
        Vector3 c = bodyCol.bounds.center;

        Vector3 high = c + Vector3.up * height * 0.25f;
        Vector3 mid = c;
        Vector3 low = c - Vector3.up * height * 0.25f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(high, Vector3.left * hw);
        Gizmos.DrawRay(high, Vector3.right * hw);
        Gizmos.DrawRay(mid, Vector3.left * hw);
        Gizmos.DrawRay(mid, Vector3.right * hw);

        Gizmos.color = isGrounded ? new Color(0f, 1f, 1f, 0.2f) : Color.cyan;
        Gizmos.DrawRay(low, Vector3.left * hw);
        Gizmos.DrawRay(low, Vector3.right * hw);
    }
#endif
}