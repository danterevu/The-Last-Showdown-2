using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlatformPlayerController : MonoBehaviour, IPlayerController
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
    [SerializeField] private float selfKnockback = 4f; public float SelfKnockback => selfKnockback;
    [SerializeField] private float knockbackLift = 5f;     //Levantamiento
    [SerializeField] private float stunDuration = 0.8f;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Crush")]
    [SerializeField] private bool isCrushed = false;

    [Header("Effects")]
    [SerializeField] private GameObject hitParticles;

    [Header("Hitbox")]
    [SerializeField] private PunchHitbox punchHitbox;

    [Header("DNA")]
    public bool hasDNA = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private float invulnerableTime = 2f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] private int playerIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private bool isDead;
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private bool isKnockedBack = false;
    [SerializeField] private bool isStunned = false;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld = false;

    // shield
    private bool shieldActive = false;
    private float shieldMultiplier = 1f;

    // doble salto
    private bool doubleJumpEnabled = false;
    private bool usedDoubleJump = true;

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
    private GameObject _jetpackObject;
    private Animator _jetpackAnimator;

    // hook
    private bool hasRawVelocityOverride = false;
    private Vector2 rawVelocityOverride = Vector2.zero;

    private bool isBeingPulled = false;
    private Vector2 pullVelocity = Vector2.zero;

   

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
    private SpriteRenderer _jetpackSR;
    private KingOfHill manager;
    private bool jetpackFiring = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        // desactivar jetpack al inicio
        if (_jetpackObject != null) _jetpackObject.SetActive(false);
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
       
        CheckWall();

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
                    if (mirrorActive && mirrorTarget != null) mirrorTarget.TriggerMirrorJump();
                }
                else if (doubleJumpEnabled && !usedDoubleJump)
                {
                    ExecuteJump();
                    usedDoubleJump = true;
                    if (mirrorActive && mirrorTarget != null) mirrorTarget.TriggerMirrorJump();
                }
                else
                {
                    jumpBufferCounter = jumpBufferTime;
                }
            }

            if (gp.buttonSouth.wasReleasedThisFrame && !isStunned)
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
        }


        CheckGround();

        if (animator != null)
        {
            // Usar el input correcto para las animaciones (movimiento normal o forzado por espejo)
            Vector2 inputForAnimations = isForcedMove ? forcedMoveInput : moveInput;
            animator.SetFloat("velocityX", Mathf.Abs(inputForAnimations.x));
            animator.SetFloat("velocityY", rb.linearVelocity.y);
            animator.SetBool("isGrounded", isGrounded);
        }

        if (invertControls) moveInput = -moveInput;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
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
            if (isGrounded || coyoteTimeCounter > 0f)
                ExecuteJump();
        }

        // Usar el input correcto para el flip del sprite
        Vector2 inputForFlip = isForcedMove ? forcedMoveInput : moveInput;
        if (inputForFlip.x > 0.01f) sr.flipX = false;
        else if (inputForFlip.x < -0.01f) sr.flipX = true;
       
        if (_jetpackSR != null)
        {
            _jetpackSR.flipX = sr.flipX;

            Vector3 pos = _jetpackObject.transform.localPosition;

            pos.x = sr.flipX ? 0.5f : -0.5f;

            _jetpackObject.transform.localPosition = pos;
        }
        if (_jetpackSR != null) _jetpackSR.flipX = sr.flipX;
        
        // Resetear movimiento forzado al final de Update (solo se mantiene 1 frame)
        isForcedMove = false;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        
        rb.gravityScale = heavyGravityActive ? heavyGravityValue : gravityScale;
        if (hasRawVelocityOverride)
        {
            rb.linearVelocity = rawVelocityOverride;
            hasRawVelocityOverride = false;
            ApplyBetterGravity();
            return;
        }

        bool wasJetpackFiring = jetpackFiring;
        jetpackFiring = false;

        // Detectar salto mantenido tanto en teclado como en gamepad
        bool jumpHeldGamepad = false;
        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null) jumpHeldGamepad = gp.buttonSouth.isPressed;
        bool jumpHeldKeyboard = jumpAction != null && jumpAction.IsPressed();

        if (jetpackActive && (jumpHeldKeyboard || jumpHeldGamepad))
        {

                AudioManager.Instance?.PlaySFX(SoundID.PUJetpack);

            jetpackFiring = true;
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Lerp(rb.linearVelocity.y, jetpackForce, 0.25f)
            );
            if (_jetpackAnimator != null)
                _jetpackAnimator.SetBool("Fire", true);
        }
        else if (jetpackActive)
        {
            if (_jetpackAnimator != null)
                _jetpackAnimator.SetBool("Fire", false);
        }



        animator.SetBool("JetpackFire", jetpackFiring);



        if (isBeingPulled)
        {
            rb.linearVelocity = pullVelocity;
        }
        else if (!isKnockedBack && !isStunned)
        {
            Vector2 inputToUse = isForcedMove ? forcedMoveInput : moveInput;
            rb.linearVelocity = new Vector2(inputToUse.x * moveSpeed, rb.linearVelocity.y);
        }

            ApplyBetterGravity();
        ApplyMirrorControl();
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
        if (isDead || isStunned) return;
        jumpHeld = true;

        if (isGrounded || coyoteTimeCounter > 0f)
        {
            ExecuteJump();
            if (mirrorActive && mirrorTarget != null) mirrorTarget.TriggerMirrorJump();
        }
        else if (doubleJumpEnabled && !usedDoubleJump)
        {
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

        if (jetpackActive && _jetpackAnimator != null)
            _jetpackAnimator.SetTrigger("Fire");
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsCorrectDevice(context.control.device)) return;
        TryAttack();
    }

    private void TryAttack()
    {
        if (isDead || !canAttack || isAttacking) return;
        isAttacking = true;
        animator.SetTrigger("Attack");
        StartCoroutine(AttackCooldown());
    }

    // Llamado por Animation Event cuando el pu�o conecta
    public void ApplyAttackHit()
    {
        punchHitbox?.Activate();
    }

    // Llamado por Animation Event al terminar la animaci�n
    public void OnAttackFinished()
    {
        isAttacking = false;
        punchHitbox?.Deactivate();
    }

    public void ReceiveKnockback(Vector2 direction)
    {
        if (isInvulnerable) return;
        if (shieldActive) { otherPlayer.ReceiveKnockback(-direction * shieldMultiplier); return; }

        
        bool wasAttacking = isAttacking;
        isAttacking = false;           
        punchHitbox?.Deactivate();     

        animator?.SetTrigger("Hurt");
        Quaternion rotation;

        if (direction.x > 0)
        {
            rotation = Quaternion.Euler(0, 0, 180);
        }
        else
        {
            rotation = Quaternion.identity;
        }

        Vector3 hitPosition =
     transform.position + new Vector3(direction.x * 0.5f, 0f, 0f);

        Instantiate(hitParticles, hitPosition, rotation);
      
        rb.linearVelocity = new Vector2(direction.x * knockbackForce, knockbackLift); // levantamiento
        StartCoroutine(KnockbackDuration());

        if (!wasAttacking)             // solo stunearse si NO era golpe simult�neo
            StartCoroutine(StunDuration());
    }

    private IEnumerator KnockbackDuration()
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(0.3f);
        isKnockedBack = false;
    }

    private IEnumerator StunDuration()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        // Muerte por peligros
        if (other.CompareTag("Spike"))
        {
            AudioManager.Instance?.PlaySFX(SoundID.LDeath);
            StartCoroutine(Die());
        }
        else if (other.CompareTag("Saw"))
        {
            AudioManager.Instance?.PlaySFX(SoundID.SDeath);
            StartCoroutine(Die());
        }
    }

    public void SetCrushed(bool crushed)
    {
        isCrushed = crushed;
        animator?.SetBool("isCrushed", crushed);
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
    private void CheckWall()
    {
       
        bool tryingToMove = Mathf.Abs(moveInput.x) > 0.1f;
        bool blockedByWall = tryingToMove && Mathf.Abs(rb.linearVelocity.x) < 0.1f;

        animator?.SetBool("isAgainstWall", blockedByWall);
    }
    private IEnumerator Die()
    {
        isDead = true;
        isKnockedBack = false;
        isStunned = false;
        isAttacking = false;
        punchHitbox?.Deactivate();
        ClearPowerUpState();
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

    public bool IsFacingRight() => !sr.flipX;
    public bool HasPowerUp() => hasPowerUp;
    public PowerUpPickup.PowerUpType GetCurrentPowerUp() => currentPowerUp;

    public void ReceivePowerUp(PowerUpPickup.PowerUpType type)
    {
        currentPowerUp = type;
        hasPowerUp = true;
        Debug.Log(gameObject.name + " recibio: " + type);
    }

    public void SetShield(bool active, float multiplier) 
    {

        shieldActive = active; 
        shieldMultiplier = multiplier; 
    }

    public void SetDoubleJump(bool active)
    {
        doubleJumpEnabled = active;
        usedDoubleJump = !active;
    }

    public void SetHeavyGravity(bool active, float gravityValue) 
    {

        heavyGravityActive = active; 
        heavyGravityValue = gravityValue; 
    }
    public void SetMirrorControl(bool active, PlatformPlayerController target) 
    {

        mirrorActive = active; 
        mirrorTarget = target; 
    }
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
    // Reemplazar los dos SetJetpack por uno solo:
    public void SetJetpack(bool active, float force, GameObject jetpackObject = null, Animator jetpackAnimator = null)
    {
        jetpackActive = active;
        jetpackForce = force;

        if (active)
        {
            _jetpackObject = jetpackObject;
            _jetpackAnimator = jetpackAnimator;
            _jetpackSR = _jetpackObject != null ? _jetpackObject.GetComponent<SpriteRenderer>() : null;
            if (_jetpackObject != null) _jetpackObject.SetActive(true);
            AudioManager.Instance?.PlaySFX(SoundID.PUEJetpack);  
        }
        else
        {
            if (_jetpackObject != null) _jetpackObject.SetActive(false);
            _jetpackSR = null;
            _jetpackObject = null;
            _jetpackAnimator = null;
        }
    }

    public Collider2D GetCollider() => col;
    public Rigidbody2D GetRigidbody() => rb;

    public void SetSpawnPoint(Vector3 point) { spawnPoint = point; }
    public void SetOtherPlayer(PlatformPlayerController other) { otherPlayer = other; }
    public void SetManager(KingOfHill m) { manager = m; }
    public void ForceRespawn() { if (!isDead) StartCoroutine(Die()); }

    private IEnumerator MirrorJumpCoroutine()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        bool prevJumpHeld = jumpHeld;
        jumpHeld = true;
        yield return new WaitForSeconds(0.25f);
        jumpHeld = prevJumpHeld;
    }

    // Limpia efectos activos (shield, jetpack, mirror, gravedad)
    public void ClearActivePowerUpEffects()
    {
        SetShield(false, 1f);
        SetHeavyGravity(false, 0f);
        SetMirrorControl(false, null);
        SetJetpack(false, 0f);
        SetDoubleJump(false);
        SetInvertControls(false);
        isStunned = false;
        isKnockedBack = false;
        isAttacking = false;
        canAttack = true;
        isCrushed = false;
        isForcedMove = false;
        forcedMoveInput = Vector2.zero;
        moveInput = Vector2.zero;
        animator?.SetBool("isCrushed", false);
        SetPulled(false);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = gravityScale;
    }

    // Limpia power up en inventario + efectos activos (al morir)
    public void ClearPowerUpState()
    {
        hasPowerUp = false;
        ClearActivePowerUpEffects();
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

    public void SetPulled(bool active, Vector2 velocity = default)
    {
        isBeingPulled = active;
        pullVelocity = velocity;
    }
}