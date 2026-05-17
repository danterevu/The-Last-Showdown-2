using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerControllerDNA : MonoBehaviour, IPlayerController
{
    [Header("Crush")]
    [SerializeField] private bool isCrushed = false;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravityScale = 4f;
    private float baseMoveSpeed;


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
    [SerializeField] private float knockbackForce;
    [SerializeField] private float selfKnockback; public float SelfKnockback => selfKnockback;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Hitbox")]
    [SerializeField] private PunchHitboxDNA punchHitbox;

    [Header("DNA")]
    private DNA carriedDNA;
    public bool hasDNA = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private float invulnerableTime = 2f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] private int playerIndex = 0;

    [Header("RemoteControl")]
    [SerializeField] RemoteControl rc; 

    [Header("Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private bool isDead;
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private bool isKnockedBack = false;

    [Header("PowerUp DNA")]
    [SerializeField] private DNAPowerUpPickup.DNAPowerUpType currentDNAPowerUp;
    [SerializeField] private bool hasDNAPowerUp = false;

    [Header("Shrink")]
    private float shrinkScale = 0.7f;      // tamaño al que se achica
    private float shrinkSpeedMult = 1.2f;  // multiplicador de velocidad
    private float shrinkDuration = 2f;     // duración en segundos

    private Vector3 originalScale;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld = false;

    [Header("PowerUp")]
    [SerializeField] private PowerUpPickup.PowerUpType currentPowerUp;
    [SerializeField] private bool hasPowerUp = false;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private InputAction moveAction;
    private InputAction remoteControl;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private Vector2 moveInput;

    private Vector3 spawnPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        rc = GetComponent<RemoteControl>();
        baseMoveSpeed = moveSpeed;
        originalScale = transform.localScale;
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
        remoteControl = map.FindAction("RemoteControl");

        moveAction?.Enable();
        if (jumpAction != null) { jumpAction.Enable(); jumpAction.performed += OnJumpPerformed; jumpAction.canceled += OnJumpCanceled; }
        if (attackAction != null) { attackAction.Enable(); attackAction.performed += OnAttack; }
        if (interactAction != null) { interactAction.Enable(); interactAction.performed += OnInteract; }
    }

    private void Update()
    {
        if (isDead) return;
        if (moveAction == null) return;

        moveInput = ReadFilteredMove();

        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
        {
            // Salto
            if (gp.buttonSouth.wasPressedThisFrame)
            {
                jumpHeld = true;
                if (isGrounded || coyoteTimeCounter > 0f)
                {
                    ExecuteJump();
                }
                else if (jumpBufferCounter <= 0f)
                {
                    jumpBufferCounter = jumpBufferTime;
                }
            }

            if (gp.buttonSouth.wasReleasedThisFrame)
                jumpHeld = false;

            // Ataque
            if (gp.buttonWest.wasPressedThisFrame)
                TryAttack();

            // Interact / PowerUp
            if (gp.buttonEast.wasPressedThisFrame)
            {
                Debug.Log(gameObject.name + " presiono interact (gamepad)");
                UsePowerUp();
            }
            // activar paredes / RemoteControl
            if (gp.buttonNorth.wasPressedThisFrame)
            {
                Debug.Log(gameObject.name + " presiono triangulo (gamepad)");
                rc.ActivateWall();
            }
        }

        // Mover hitbox al lado correcto
        if (punchHitbox != null)
        {
            Vector3 pos = punchHitbox.transform.localPosition;
            pos.x = IsFacingRight() ? Mathf.Abs(pos.x) : -Mathf.Abs(pos.x);
            punchHitbox.transform.localPosition = pos;
        }

        CheckGround();

        if (animator != null)
        {
            animator.SetFloat("velocityX", Mathf.Abs(moveInput.x));
            animator.SetFloat("velocityY", rb.linearVelocity.y);
            animator.SetBool("isGrounded", isGrounded);
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
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

        if (moveInput.x > 0.01f) sr.flipX = false;
        else if (moveInput.x < -0.01f) sr.flipX = true;
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        rb.gravityScale = gravityScale;

        if (!isKnockedBack)
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }

        ApplyBetterGravity();
    }
    private void LateUpdate()
    {
        if (punchHitbox != null)
        {
            // posicion
            Vector3 pos = punchHitbox.transform.localPosition;
            pos.x = IsFacingRight() ? Mathf.Abs(pos.x) : -Mathf.Abs(pos.x);
            punchHitbox.transform.localPosition = pos;

            // escala - esto soluciona el estiramiento
            Vector3 scale = punchHitbox.transform.localScale;
            scale.x = IsFacingRight() ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            punchHitbox.transform.localScale = scale;
        }
    }

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

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsCorrectDevice(context.control.device)) return;
        if (isDead) return;
        jumpHeld = true;

        if (isGrounded || coyoteTimeCounter > 0f)
            ExecuteJump();
        else
            jumpBufferCounter = jumpBufferTime;
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
        if (!IsCorrectDevice(context.control.device)) return;
        TryAttack();
    }

    private void TryAttack()
    {
        if (isDead || !canAttack || isAttacking || hasDNA) return;
        isAttacking = true;
        animator.SetTrigger("Attack");
        StartCoroutine(AttackCooldown());
    }

    // Llamado por Animation Event cuando el puño conecta
    public void ApplyAttackHit()
    {
        punchHitbox?.Activate();
    }

    // Llamado por Animation Event al terminar la animación
    public void OnAttackFinished()
    {
        isAttacking = false;
        punchHitbox?.Deactivate();
    }

    public void ReceiveKnockback(Vector2 direction)
    {
        if (isInvulnerable) return;
        animator?.SetTrigger("Hurt");
        rb.linearVelocity = direction * knockbackForce;
        StartCoroutine(KnockbackDuration());
    }
    public void ApplySelfKnockback(float dirX)
    {
        rb.linearVelocity = new Vector2(-dirX * selfKnockback, selfKnockback * 0.3f);
        //Debug.Log($"SelfKnockback ejecutado | velocidad aplicada: {-dirX * selfKnockback}, {selfKnockback * 0.3f}");
        StartCoroutine(KnockbackDuration()); // isKnockedBack = true para que FixedUpdate no lo pise
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
        isAttacking = false;
        punchHitbox?.Deactivate();
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

    public void SetCrushed(bool crushed)
    {
        isCrushed = crushed;
        animator?.SetBool("isCrushed", crushed);
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
        if (!IsCorrectDevice(context.control.device)) return;
        Debug.Log(gameObject.name + " presiono interact");
        UsePowerUp();
    }

    private bool IsCorrectDevice(InputDevice device)
    {
        if (device is Keyboard) return true;
        if (device is Gamepad gamepad)
            return gamepad == InputAssigner.GetGamepadForPlayer(playerIndex);
        return false;
    }

    // Power ups — implementar según el nuevo minijuego
    private void UsePowerUp()
    {
        if (!hasDNAPowerUp) return;
        hasDNAPowerUp = false;

        switch (currentDNAPowerUp)
        {
            case DNAPowerUpPickup.DNAPowerUpType.Shrink:
                StartCoroutine(ShrinkEffect());
                break;
        }
    }

    public bool HasPowerUp() => hasPowerUp;
    public PowerUpPickup.PowerUpType GetCurrentPowerUp() => currentPowerUp;

    public void ReceiveDNAPowerUp(DNAPowerUpPickup.DNAPowerUpType type)
    {
        currentDNAPowerUp = type;
        hasDNAPowerUp = true;
        Debug.Log(gameObject.name + " recibio DNA powerup: " + type);
    }

    public void ClearPowerUpState()
    {
        hasPowerUp = false;
        ClearActivePowerUpEffects();
    }

    public void ClearActivePowerUpEffects()
    {
        StopAllCoroutines(); // para el shrink si está activo
        transform.localScale = originalScale;
        moveSpeed = hasDNA ? baseMoveSpeed * 0.6f : baseMoveSpeed;
        hasDNAPowerUp = false;
    }

    //POWER UPS EFFECTS

    private IEnumerator ShrinkEffect()
    {
        // Achicarse
        transform.localScale = originalScale * shrinkScale;
        float speedBoost = baseMoveSpeed * shrinkSpeedMult;
        float previousSpeed = moveSpeed;
        moveSpeed = speedBoost;

        yield return new WaitForSeconds(shrinkDuration);

        // Volver al tamaño original
        transform.localScale = originalScale;
        moveSpeed = hasDNA ? baseMoveSpeed * 0.6f : baseMoveSpeed;
    }

    public bool IsFacingRight() => !sr.flipX;
    public Collider2D GetCollider() => col;
    public Rigidbody2D GetRigidbody() => rb;
    public void SetSpawnPoint(Vector3 point) { spawnPoint = point; }

    // Mutant DNA
    public bool HasDNA() => hasDNA;
    public void PickDNA(DNA dna)
    {
        hasDNA = true;
        carriedDNA = dna;
        moveSpeed = baseMoveSpeed * 0.6f;
    }

    public void DropDNA()
    {
        hasDNA = false;
        carriedDNA = null;
        moveSpeed = baseMoveSpeed;
    }

    public DNA GetCarriedDNA() => carriedDNA;

}