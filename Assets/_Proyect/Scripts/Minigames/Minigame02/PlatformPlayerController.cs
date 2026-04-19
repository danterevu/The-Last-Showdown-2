using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlatformPlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravityScale = 4f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.85f;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Golpe")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float selfKnockback = 4f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("DNA")]
    public bool hasDNA = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private float invulnerableTime = 2f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";

    [Header("Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private bool isDead;
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isKnockedBack = false;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld = false;

    // shield
    private bool shieldActive = false;
    private float shieldMultiplier = 1f;

    // doble salto - solo se activa con el power up
    private bool doubleJumpEnabled = false;
    private bool usedDoubleJump = true; // empieza en true para que no pueda usar sin powerup

    // gravedad pesada
    private bool heavyGravityActive = false;
    private float heavyGravityValue = 0f;

    // control espejo
    private bool mirrorActive = false;
    private PlatformPlayerController mirrorTarget = null;
    private bool isForcedMove = false;
    private Vector2 forcedMoveInput = Vector2.zero;

    // mirror jump
    private bool mirrorJumpPending = false;

    // controles invertidos
    private bool invertControls = false;

    // jetpack
    private bool jetpackActive = false;
    private float jetpackForce = 0f;

    // hook: override de velocidad total
    private bool hasRawVelocityOverride = false;
    private Vector2 rawVelocityOverride = Vector2.zero;

    [Header("PowerUp")]
    [SerializeField] private PowerUpPickup.PowerUpType currentPowerUp;
    [SerializeField] private bool hasPowerUp = false;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private Vector2 moveInput;

    private PlatformPlayerController otherPlayer;
    private Vector3 spawnPoint;
    private KingOfHill manager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable() { SetupInput(); }

    private void OnDisable()
    {
        moveAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
            jumpAction.Disable();
        }
        if (attackAction != null) { attackAction.performed -= OnAttack; attackAction.Disable(); }
        if (interactAction != null) { interactAction.performed -= OnInteract; interactAction.Disable(); }
    }

    private void SetupInput()
    {
        if (inputActions == null) return;
        var map = inputActions.FindActionMap(actionMapName);
        if (map == null) { Debug.LogError("Action map no encontrado: " + actionMapName); return; }

        moveAction = map.FindAction("Move");
        jumpAction = map.FindAction("Jump");
        attackAction = map.FindAction("Attack");
        interactAction = map.FindAction("Interact");

        moveAction?.Enable();
        if (jumpAction != null) { jumpAction.Enable(); jumpAction.performed += OnJumpPerformed; jumpAction.canceled += OnJumpCanceled; }
        if (attackAction != null) { attackAction.Enable(); attackAction.performed += OnAttack; }
        if (interactAction != null) { interactAction.Enable(); interactAction.performed += OnInteract; }
    }

    private void Update()
    {
        if (isDead) return;
        if (moveAction == null) return;
        moveInput = moveAction.ReadValue<Vector2>();
        CheckGround();

        // ESTO tiene que estar acá
        if (animator != null)
        {
            animator.SetFloat("velocityX", Mathf.Abs(moveInput.x));
            animator.SetFloat("velocityY", rb.linearVelocity.y);
            animator.SetBool("isGrounded", isGrounded);
        }
        
        if (invertControls) moveInput = -moveInput;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            // FIX double jump: solo resetear usedDoubleJump si el powerup esta activo
            // si no, mantenerlo en true para bloquear el segundo salto
            if (doubleJumpEnabled) usedDoubleJump = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
            if (isGrounded || coyoteTimeCounter > 0f) ExecuteJump();
        }


        if (mirrorJumpPending)
        {
            mirrorJumpPending = false;
            StartCoroutine(MirrorJumpCoroutine());

            // si toca el suelo dentro del buffer, salta
            if (isGrounded || coyoteTimeCounter > 0f)
                ExecuteJump();

           

        }
        if (moveInput.x > 0.01f) sr.flipX = false;
        else if (moveInput.x < -0.01f) sr.flipX = true;

          
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        rb.gravityScale = heavyGravityActive ? heavyGravityValue : gravityScale;

        // hook: prioridad maxima sobre todo lo demas
        if (hasRawVelocityOverride)
        {
            rb.linearVelocity = rawVelocityOverride;
            hasRawVelocityOverride = false;
            ApplyBetterGravity();
            return;
        }

        if (jetpackActive && jumpHeld)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jetpackForce);

        if (!isKnockedBack)
        {
            Vector2 inputToUse = isForcedMove ? forcedMoveInput : moveInput;
            rb.linearVelocity = new Vector2(inputToUse.x * moveSpeed, rb.linearVelocity.y);
            isForcedMove = false;
        }

        ApplyBetterGravity();
        ApplyMirrorControl();
    }

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isDead) return;
        jumpHeld = true;

        if (isGrounded || coyoteTimeCounter > 0f)
        {
            ExecuteJump();
            // FIX double jump: NO resetear usedDoubleJump aqui
            // Update lo resetea solo cuando doubleJumpEnabled=true
            // si lo reseteamos aqui, un salto normal desde el suelo habilita el double jump
            if (mirrorActive && mirrorTarget != null) mirrorTarget.TriggerMirrorJump();
        }
        else if (doubleJumpEnabled && !usedDoubleJump)
        {
            // segundo salto: solo disponible con el power up activo
            ExecuteJump();
            usedDoubleJump = true;
            if (mirrorActive && mirrorTarget != null) mirrorTarget.TriggerMirrorJump();
        }
        else
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) { jumpHeld = false; }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isGrounded = false;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
     
      

        if (isDead || !canAttack || otherPlayer == null) return;
        float dist = Vector2.Distance(transform.position, otherPlayer.transform.position);
        Debug.Log($"Distancia al rival: {dist} | attackRange: {attackRange}");
        if (dist <= attackRange)
        {
            animator.SetTrigger("Attack");
            StartCoroutine(KnockbackDuration());
            StartCoroutine(AttackCooldown());
        }
    }
    public void ApplyAttackHit()
    {
        if (otherPlayer == null) return;
        float dist = Vector2.Distance(transform.position, otherPlayer.transform.position);
        if (dist <= attackRange)
        {
            float dirX = otherPlayer.transform.position.x > transform.position.x ? 1f : -1f;
            Vector2 knockDir = new Vector2(dirX, 0.3f).normalized;
            otherPlayer.ReceiveKnockback(knockDir);
            rb.linearVelocity = new Vector2(-dirX * selfKnockback, selfKnockback * 0.3f);
            StartCoroutine(KnockbackDuration());
        }
    }
    public void ReceiveKnockback(Vector2 direction)
    {
        if (isInvulnerable) return;
        if (shieldActive) { otherPlayer.ReceiveKnockback(-direction * shieldMultiplier); return; }
        animator?.SetTrigger("Hurt"); // <-- agregar esto
        rb.linearVelocity = direction * knockbackForce;
        StartCoroutine(KnockbackDuration());
    }

    private IEnumerator KnockbackDuration()
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(0.3f);
        isKnockedBack = false;
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Spike") && !isDead && !isInvulnerable)
            StartCoroutine(Die());
    }

    private void CheckGround()
    {
        // si esta subiendo, no puede estar en el suelo
        if (rb.linearVelocity.y > 0.1f)
        {
            isGrounded = false;
            return;
        }

        Vector2 leftOrigin = new Vector2(col.bounds.min.x, col.bounds.min.y);
        Vector2 rightOrigin = new Vector2(col.bounds.max.x, col.bounds.min.y);

        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, groundCheckDistance, groundLayer);

        LayerMask headLayer = 1 << LayerMask.NameToLayer("PlayerHead");
        RaycastHit2D headLeft = Physics2D.Raycast(leftOrigin, Vector2.down, groundCheckDistance, headLayer);
        RaycastHit2D headRight = Physics2D.Raycast(rightOrigin, Vector2.down, groundCheckDistance, headLayer);

        bool onGround = hitLeft.collider != null || hitRight.collider != null;
        bool onHead = (headLeft.collider != null && headLeft.collider.transform.root != transform) ||
                        (headRight.collider != null && headRight.collider.transform.root != transform);

        isGrounded = onGround || onHead;
    }

    private IEnumerator Die()
    {
  
        isDead = true;
        isKnockedBack = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
      
        animator.SetTrigger("Die");
        yield return new WaitForSeconds(respawnDelay);
        sr.enabled = false;
        transform.position = spawnPoint;
        rb.gravityScale = gravityScale;
        yield return new WaitForSeconds(0.1f);
        sr.enabled = true;
        isDead = false;
        StartCoroutine(Invulnerable());
    }

    private IEnumerator Invulnerable()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        while (elapsed < invulnerableTime)
        {
            sr.enabled = !sr.enabled;
            elapsed += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
        sr.enabled = true;
        isInvulnerable = false;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log(gameObject.name + " presiono interact");
        UsePowerUp();
    }

    private void UsePowerUp()
    {
        if (!hasPowerUp || manager == null)
        {
            Debug.Log(gameObject.name + " sin powerup o manager nulo");
            return;
        }
        Debug.Log(gameObject.name + " usando: " + currentPowerUp);
        hasPowerUp = false;
        manager.ActivatePowerUp(currentPowerUp, this, otherPlayer);
    }

    public bool HasPowerUp() => hasPowerUp;
    public PowerUpPickup.PowerUpType GetCurrentPowerUp() => currentPowerUp;
    public void ReceivePowerUp(PowerUpPickup.PowerUpType type)
    {
        currentPowerUp = type;
        hasPowerUp = true;
        Debug.Log(gameObject.name + " recibio: " + type);
    }

    public void SetShield(bool active, float multiplier) { shieldActive = active; shieldMultiplier = multiplier; }

    public void SetDoubleJump(bool active)
    {
        doubleJumpEnabled = active;
        // al activar el powerup: habilitar el segundo salto
        // al desactivar: bloquearlo de nuevo
        usedDoubleJump = !active;
    }

    public void SetHeavyGravity(bool active, float gravityValue) { heavyGravityActive = active; heavyGravityValue = gravityValue; }
    public void SetMirrorControl(bool active, PlatformPlayerController target) { mirrorActive = active; mirrorTarget = target; }
    public void TriggerMirrorJump() { mirrorJumpPending = true; }

    private void ApplyMirrorControl()
    {
        if (!mirrorActive || mirrorTarget == null) return;
        mirrorTarget.ForceMove(moveInput);
    }

    public void ForceMove(Vector2 input) { isForcedMove = true; forcedMoveInput = input; }
    public void ForceJump() { if (isGrounded) ExecuteJump(); }
    public void ForceVelocity(Vector2 velocity) { isForcedMove = true; rb.linearVelocity = velocity; }
    public void ForceVelocityRaw(Vector2 velocity) { hasRawVelocityOverride = true; rawVelocityOverride = velocity; }
    public void SetInvertControls(bool active) { invertControls = active; }
    public void SetJetpack(bool active, float force) { jetpackActive = active; jetpackForce = force; }

    public Collider2D GetCollider() => col;
    public Rigidbody2D GetRigidbody() => rb;

    public void SetSpawnPoint(Vector3 point) { spawnPoint = point; }
    public void SetOtherPlayer(PlatformPlayerController other) { otherPlayer = other; }
    public void SetManager(KingOfHill m) { manager = m; }
    public void ForceRespawn() { if (!isDead) StartCoroutine(Die()); }
    public void ApplyMoveDebuff(float debuff) { moveSpeed *= debuff; }

    public bool HasDNA() => hasDNA;
    public void PickDNA() { hasDNA = true; }
    public void DropDNA() { hasDNA = false; }

    private IEnumerator MirrorJumpCoroutine()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        // simular jumpHeld para que ApplyBetterGravity no corte el salto inmediatamente
        bool prevJumpHeld = jumpHeld;
        jumpHeld = true;
        yield return new WaitForSeconds(0.25f);
        jumpHeld = prevJumpHeld;
    }
}
