using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class ChaseRunPlayerController : MonoBehaviour
{
    // Movimiento

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravityScale = 4f;

    // Salto 

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [Tooltip("Multiplica velocityY al soltar el botón (0-1). Menor = salto más corto.")]
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [Tooltip("Multiplicador de gravedad extra al caer. Mayor = caída más pesada.")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    // Ground Check 

    [Header("Ground Check")]
    [Tooltip("Tamaño del box de detección de suelo. Ajustar al ancho del collider body del player.")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    [Tooltip("Cuánto baja el centro del box desde el borde inferior del collider.")]
    [SerializeField] private float groundCheckOffset = 0.02f;
    [SerializeField] private LayerMask groundLayer;

    // Wall Check 

    [Header("Wall Check")]
    [Tooltip("Distancia del raycast lateral más allá del borde del collider.")]
    [SerializeField] private float wallCheckDistance = 0.12f;

    // Wall Slide 

    [Header("Wall Slide")]
    [Tooltip("Frenada al primer contacto con la pared (0=nada, 1=frena del todo).")]
    [SerializeField] private float wallInitialFriction = 0.75f;
    [Tooltip("Gravedad artificial durante el deslizamiento. Menor = cae más lento.")]
    [SerializeField] private float wallSlideGravity = 2.5f;
    [Tooltip("Velocidad máxima de caída en pared (negativo).")]
    [SerializeField] private float wallSlideMaxFallSpeed = -2.5f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForceX = 8f;
    [SerializeField] private float wallJumpForceY = 11f;
    [Tooltip("Tiempo tras el wall jump donde el input horizontal no puede anular el impulso.")]
    [SerializeField] private float wallJumpInputLockTime = 0.15f;

    // Input 

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] private int playerIndex = 0;

    // Respawn 

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.6f;
    [SerializeField] private float invulnerableTime = 1.5f;

    //Debug (solo lectura en inspector)

    [Header("Debug (solo lectura)")]
    [SerializeField] private bool dbgGrounded;
    [SerializeField] private bool dbgWallL;
    [SerializeField] private bool dbgWallR;
    [SerializeField] private bool dbgWallSliding;
    [SerializeField] private int dbgExtraJumps;

    // Componentes

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D bodyCol;       // el collider principal (body)
    private Animator animator;

    // Input

    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector2 moveInput;

    // Estado

    private bool isGrounded;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private bool isWallSliding;
    private bool wasWallSliding;
    private int wallSlideSide;           // -1 izq, +1 der — se bloquea al entrar

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld;
    private bool isJumping;
    private bool jumpCutApplied;         // solo aplicar jump cut una vez por salto

    private float wallJumpInputLockTimer; // bloquea input X tras wall jump

    // Extra jumps 

    private int baseExtraJumps = 0;
    private int extraJumpsLeft = 0;

    // Power ups 

    private float speedMultiplier = 1f;
    private bool killZoneImmune = false;

    // Referencias 

    private ChaseRunManager manager;
    private ChaseRunCamera chaseCamera;
    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;

    // Respawn 

    private bool isDead;
    private bool isInvulnerable;

    // Propiedad pública

    public int PlayerNumber => playerIndex + 1;

    
    //  Inicialización
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Tomar el collider marcado como NO trigger (el body)
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
        isInvulnerable = false;
        speedMultiplier = 1f;
        killZoneImmune = false;
        extraJumpsLeft = baseExtraJumps;
    }

    public void OnPhaseChanged(ChaseRunManager.RunPhase newPhase) => currentPhase = newPhase;

    
    //  Update
  

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
            isJumping = false;
            jumpCutApplied = false;
            extraJumpsLeft = baseExtraJumps;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Resetear extra jumps al pegarse a la pared
        if (isWallSliding)
            extraJumpsLeft = baseExtraJumps;

        // Wall jump input lock timer
        if (wallJumpInputLockTimer > 0f)
            wallJumpInputLockTimer -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
            TryExecuteBufferedJump();
        }

        // Flip sprite
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

    
    //  FixedUpdate
   

    private void FixedUpdate()
    {
        if (isDead) return;

        if (isWallSliding)
        {
            ApplyWallSlidePhysics();
        }
        else
        {
            rb.gravityScale = gravityScale;
            ApplyHorizontalMove();
            ApplyBetterGravity();
        }

        if (!isInvulnerable && !killZoneImmune && chaseCamera != null)
            CheckKillZone();
    }

    
    //  Ground Check
   

    private void CheckGround()
    {
        if (rb.linearVelocity.y > 0.5f) { isGrounded = false; return; }
        if (bodyCol == null) return;

        // Inset horizontal para no rozar paredes laterales con las esquinas
        float innerMargin = 0.08f;
        float boxW = Mathf.Max(0.05f, bodyCol.bounds.size.x - innerMargin * 2f);

        Vector2 center = new Vector2(
            bodyCol.bounds.center.x,
            bodyCol.bounds.min.y - groundCheckOffset
        );

        isGrounded = Physics2D.OverlapBox(center, new Vector2(boxW, groundCheckSize.y), 0f, groundLayer) != null;
    }

    

    private void CheckWalls()
    {
        if (bodyCol == null) return;

        float halfW = bodyCol.bounds.extents.x;
        float height = bodyCol.bounds.size.y;
        Vector2 center = bodyCol.bounds.center;

        // Tres puntos de altura: 75%, 50% y 25% del collider desde abajo
        // El punto al 25% lo usamos solo si NO estamos en suelo (evita
        // detectar el tile de esquina cuando estamos parados encima)
        Vector2 high = center + Vector2.up * height * 0.25f;   // 75% desde abajo
        Vector2 mid = center;                                   // 50%
        Vector2 low = center - Vector2.up * height * 0.25f;   // 25% desde abajo

        float dist = halfW + wallCheckDistance;

        if (isGrounded)
        {
            // Parado encima (imagen 2): solo raycasts alto y medio
            // El bajo rozaría el tile del suelo lateral y daría false positive
            isTouchingWallLeft = Physics2D.Raycast(high, Vector2.left, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.left, dist, groundLayer);
            isTouchingWallRight = Physics2D.Raycast(high, Vector2.right, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.right, dist, groundLayer);
        }
        else
        {
            // En el aire (imagen 1): los tres raycasts
            isTouchingWallLeft = Physics2D.Raycast(high, Vector2.left, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.left, dist, groundLayer)
                               || Physics2D.Raycast(low, Vector2.left, dist, groundLayer);
            isTouchingWallRight = Physics2D.Raycast(high, Vector2.right, dist, groundLayer)
                               || Physics2D.Raycast(mid, Vector2.right, dist, groundLayer)
                               || Physics2D.Raycast(low, Vector2.right, dist, groundLayer);
        }
    }

    //  Wall Slide lógica

    private void HandleWallSlide()
    {
        bool pressingLeft = moveInput.x < -0.1f && isTouchingWallLeft;
        bool pressingRight = moveInput.x > 0.1f && isTouchingWallRight;

        // Wall slide solo si estamos en el aire Y cayendo (o quietos en Y)
        // isGrounded aquí impide entrar en wall slide parado encima de una pared
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

    // Wall slide física 
    private void ApplyWallSlidePhysics()
    {
        rb.gravityScale = 0f;
        float vy = rb.linearVelocity.y;

        if (!wasWallSliding)
        {
            // Primer frame: frenada inicial fuerte
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

    
    //  Movimiento horizontal y gravedad
  

    private void ApplyHorizontalMove()
    {
        // Si acabamos de hacer wall jump, no dejar que el input anule el impulso X
        float inputX = (wallJumpInputLockTimer > 0f) ? 0f : moveInput.x;
        rb.linearVelocity = new Vector2(inputX * moveSpeed * speedMultiplier, rb.linearVelocity.y);
    }

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // Caída más pesada
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && isJumping && !jumpCutApplied)
        {
            // Jump cut — solo una vez
            jumpCutApplied = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    
    //  Saltos
    

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsCorrectDevice(ctx.control.device) || isDead) return;
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

    
    //  Kill Zone & Respawn
    

    private void CheckKillZone()
    {
        if (isDead) return;

        float killBound = chaseCamera.GetKillZoneBound();
        bool dead = currentPhase == ChaseRunManager.RunPhase.PhaseY
            ? transform.position.y < killBound
            : transform.position.x < killBound;

        if (dead) StartCoroutine(DoRespawn());
    }

    private IEnumerator DoRespawn()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // Ocultar al jugador durante el delay
        sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Spawnear en la posición del runner (siempre dentro de cámara)
        if (chaseCamera != null && chaseCamera.Runner != null)
        {
            Vector3 runnerPos = chaseCamera.Runner.position;

            // Separar ligeramente a los dos jugadores para que no se superpongan
            float offset = (playerIndex == 0) ? -0.6f : 0.6f;

            transform.position = currentPhase == ChaseRunManager.RunPhase.PhaseY
                ? new Vector3(runnerPos.x + offset, runnerPos.y, 0f)
                : new Vector3(runnerPos.x, runnerPos.y + offset, 0f);
        }

        rb.gravityScale = gravityScale;
        isDead = false;
        sr.enabled = true;

        StartCoroutine(DoInvulnerable());
    }

    private IEnumerator DoInvulnerable()
    {
        isInvulnerable = true;
        float elapsed = 0f;

        while (elapsed < invulnerableTime)
        {
            sr.enabled = !sr.enabled;
            elapsed += 0.12f;
            yield return new WaitForSeconds(0.12f);
        }

        sr.enabled = true;
        isInvulnerable = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
            manager?.PlayerReachedGoal(PlayerNumber);
    }

   
    //  Input
    

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

    



    public void SetSpawnPoint(Vector3 point) => _ = point; // mantenido por compatibilidad, no se usa
    public void ApplySpeedMultiplier(float mult) => speedMultiplier = mult;
    public void SetKillZoneImmunity(bool immune) => killZoneImmune = immune;

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

        // Wall check rays (cyan = aéreo, amarillo = parado)
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

        // El raycast bajo solo se usa en el aire — mostrarlo atenuado si está en suelo
        Gizmos.color = isGrounded ? new Color(0f, 1f, 1f, 0.2f) : Color.cyan;
        Gizmos.DrawRay(low, Vector3.left * hw);
        Gizmos.DrawRay(low, Vector3.right * hw);
    }
#endif
}