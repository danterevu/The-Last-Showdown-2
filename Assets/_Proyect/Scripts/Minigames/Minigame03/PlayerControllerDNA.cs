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
    private float baseJumpForce;
    private float baseGravityScale;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.85f;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Golpe")]
    [SerializeField] public float knockbackForce;
    [SerializeField] private float selfKnockback; public float SelfKnockback => selfKnockback;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Crate")]
    [SerializeField] private Transform crateHoldPoint; // punto donde se sostiene la caja
    [SerializeField] private LayerMask crateLayer;
    private Crate heldCrate = null;
    private bool isStunned = false;
    private float stunTimer = 0f;

    [Header("Manos del Jugador")]
    [SerializeField] private GameObject hands;
    [SerializeField] private Animator handsAnimator;
    [SerializeField] private float throwAnimationDelay = 0.5f;

    [Header("Hitbox")]
    [SerializeField] private PunchHitboxDNA punchHitbox;

    [Header("DNA")]
    private DNA carriedDNA;
    public bool hasDNA = false;
    [SerializeField] private Transform dnaHoldPoint;   // Asignar en Inspector
    [SerializeField] private Vector3 dnaHoldOffsetRight;
    [SerializeField] private Vector3 dnaHoldOffsetLeft;


    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private float invulnerableTime = 2f;
    private Vector3 spawnPointPlayers;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] public int playerIndex = 0;

    [Header("RemoteControl")]
    [SerializeField] RemoteControl rc;

    [Header("Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private bool isDead;
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private bool isKnockedBack = false;
    [SerializeField] private bool isFrozen;

    [Header("PowerUp DNA")]
    [SerializeField] private DNAPowerUpPickup.DNAPowerUpType currentDNAPowerUp;
    [SerializeField] private bool hasDNAPowerUp = false;
    public static event System.Action<PlayerControllerDNA, DNAPowerUpPickup.DNAPowerUpType> OnPowerUpGained;
    public static event System.Action<PlayerControllerDNA> OnPowerUpUsed;

    [Header("Shrink")]
    [SerializeField] private float shrinkScale = 0.7f;      // tama�o al que se achica
    [SerializeField] private float shrinkSpeedMult = 1.2f;  // multiplicador de velocidad
    [SerializeField] private float shrinkDuration = 2f;     // duraci�n en segundos
    [SerializeField] private float shrinkAnimationDelay = 0.3f; // Delay antes de cambiar la escala
    [SerializeField] private float growAnimationDelay = 0.3f;   // Delay antes de volver a la escala normal

    [Header("Mine")]
    [SerializeField] private GameObject minePrefab;

    [Header("Berserk")]
    // [SerializeField] private BerserkHitbox berserkHitbox;
    [SerializeField] private float berserkDuration = 3f;
    [SerializeField] private float berserkSpeedMult = 1.2f;
    [SerializeField] private float berserkScale = 1.25f;
    [SerializeField] private float berserkStunDuration = 1.5f;
    [SerializeField] private bool isBerserk = false;


    [Header("SlimeShot")]
    [SerializeField] private GameObject slimeProjectilePrefab;
    [SerializeField] private float slimeDuration = 2f;
    [SerializeField] private float slimeSpeedMult = 0.5f;      // 50% mas lento
    [SerializeField] private float slimeJumpForceReduction = 0.3f; // salta al 30% de lo normal
    [SerializeField] private float slimeGravityMult = 2.5f;    // mas pesado
    [SerializeField] private bool isSlimed = false;
    private Coroutine slimeCoroutine;

    [Header("Shield")]
    [SerializeField] private GameObject shieldVFX;
    private bool shieldActive = false;
    private float shieldMultiplier = 1f;
    private float shieldDuration = 3f;

    private Vector3 originalScale;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld = false;


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
        baseJumpForce = jumpForce;
        baseGravityScale = gravityScale;

        // Aseguramos que las manos empiecen desactivadas
        SetHandsActive(false);
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
        if (isDead || isFrozen) return;
        if (moveAction == null) return;

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                canAttack = true;
                Debug.Log(gameObject.name + " recuperado del stun");
            }
            return; // No procesar movimiento ni ataques
        }

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
                PerformInteract();
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

        if (dnaHoldPoint != null)
        {
            dnaHoldPoint.localPosition = IsFacingRight() ? dnaHoldOffsetRight : dnaHoldOffsetLeft;
        }
    }

    private void FixedUpdate()
    {
        if (isFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (isStunned) return;
        rb.gravityScale = gravityScale;

        if (!isKnockedBack)
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
        ApplyBetterGravity();
    }

    private void LateUpdate()
    {
        if (punchHitbox != null) //para orientar la hitbox de la pi�a
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
        /* if (berserkHitbox != null) //para orientar la hitbox del berserk
         {
             Vector3 pos = berserkHitbox.transform.localPosition;
             pos.x = IsFacingRight() ? Mathf.Abs(pos.x) : -Mathf.Abs(pos.x);
             berserkHitbox.transform.localPosition = pos;
         }*/

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
        if (hasDNA)
        {
            ThrowDNA();
            return;
        }
        if (!canAttack || isAttacking || hasDNA || isStunned || heldCrate != null) return;
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
        animator?.SetTrigger("Hurt");
        rb.linearVelocity = direction * knockbackForce;
        StartCoroutine(KnockbackDuration());
    }

    public void ReceiveKnockback(Vector2 direction, float force)
    {
        if (isInvulnerable) return;
        animator?.SetTrigger("Hurt");
        rb.linearVelocity = direction * force;
        StartCoroutine(KnockbackDuration());
    }

    public void ReceiveKnockback(Vector2 direction, float force, PlayerControllerDNA attacker)
    {
        if (isInvulnerable) return;

        // Si el escudo está activo y hay un atacante, redirigir el knockback hacia él
        if (shieldActive && attacker != null && attacker != this)
        {
            // Aplicar knockback al atacante SIN activar su animación "Hurt" (solo impulso)
            attacker.ApplyForcedKnockback(-direction, force * shieldMultiplier);
            // El defensor no recibe knockback ni stun
            return;
        }

        // --- Comportamiento normal (sin escudo o sin atacante) ---
        // INTERRUMPIR ATAQUE si estaba atacando
        if (isAttacking)
        {
            isAttacking = false;
            punchHitbox?.Deactivate();
            // Opcional: detener animación de ataque si es necesario
            animator.ResetTrigger("Attack");
        }

        animator?.SetTrigger("Hurt");
        StartCoroutine(ResetHurtTrigger());
        rb.linearVelocity = direction * force;
        StartCoroutine(KnockbackDuration());
    }

    // Aplica solo fuerza de knockback, sin animación, sin stun, sin afectar canAttack
    public void ApplyForcedKnockback(Vector2 direction, float force)
    {
        if (isAttacking)
        {
            isAttacking = false;
            punchHitbox?.Deactivate();
            animator.ResetTrigger("Attack");
        }
        rb.linearVelocity = direction * force;
        StartCoroutine(KnockbackDuration());
    }

    public void ApplySelfKnockback(float dirX)
    {
        if (isAttacking)
        {
            isAttacking = false;
            punchHitbox?.Deactivate();
            animator.ResetTrigger("Attack");
        }
        rb.linearVelocity = new Vector2(-dirX * selfKnockback, selfKnockback * 0.3f);
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

        isGrounded = onGround || onHead; //aura
    }

    public void SetCrushed(bool crushed)
    {
        isCrushed = crushed;
        animator?.SetBool("isCrushed", crushed);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!IsCorrectDevice(context.control.device)) return;
        PerformInteract();
    }

    public void PerformInteract()
    {
        if (isDead || isStunned) return;

        // PRIORIDAD ABSOLUTA: si tengo caja, la suelto o lanzo
        if (heldCrate != null)
        {
            float playerSpeed = Mathf.Abs(rb.linearVelocity.x);
            Crate currentCrate = heldCrate;
            heldCrate = null;

            if (playerSpeed < 0.5f)
            {
                currentCrate.DropAtPlace();
                SetHandsActive(false);
            }
            else
            {
                float dirX = IsFacingRight() ? 1f : -1f;
                currentCrate.Throw(new Vector2(dirX, 0f), playerSpeed);
                Debug.Log($"[MANOS] Throw trigger activado - Hands Animator: {(handsAnimator != null ? "Asignado" : "NULL")}");
                handsAnimator?.SetTrigger("Throw");
                StartCoroutine(HideHandsAfterDelay());
            }
            return; //  IMPORTANTE: salir aqu� para no procesar power-up ni agarrar otra caja
        }

        // Si no tengo caja, intento agarrar una cercana
        if (TryGrabNearbyCrate())
            return; // si agarr� caja, no usar power-up

        // Si no hay caja cerca, usar power-up (si lo tiene)
        if (hasDNAPowerUp)
        {
            UsePowerUp();
        }
    }

    private bool TryGrabNearbyCrate()
    {
        if (hasDNA) return false;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.8f, crateLayer);
        foreach (var col in colliders)
        {
            Crate crate = col.GetComponent<Crate>();
            if (crate != null && crate.TryPickUp(this))
            {
                heldCrate = crate;
                SetHandsActive(true);
      
                handsAnimator?.SetTrigger("Grab");
                return true;
            }
        }
        return false;
    }

    private void SetHandsActive(bool active)
    {
        if (hands != null) hands.SetActive(active);
    }

    private bool IsCorrectDevice(InputDevice device)
    {
        if (device is Keyboard) return true;
        if (device is Gamepad gamepad)
            return gamepad == InputAssigner.GetGamepadForPlayer(playerIndex);
        return false;
    }

    // Power ups a implementar segun el nuevo minijuego
    private void UsePowerUp()
    {
        if (!hasDNAPowerUp || heldCrate != null) return;
        OnPowerUpUsed?.Invoke(this);
        hasDNAPowerUp = false;

        Debug.Log($"🎮 {gameObject.name} activated powerup: {currentDNAPowerUp}");
        
        switch (currentDNAPowerUp)
        {
            case DNAPowerUpPickup.DNAPowerUpType.Shrink:
                Debug.Log("  → Shrink powerup activated!");
                StartCoroutine(ShrinkEffect());
                break;
            case DNAPowerUpPickup.DNAPowerUpType.Mine:
                Debug.Log("  → Mine powerup activated!");
                PlaceMine();
                break;
            case DNAPowerUpPickup.DNAPowerUpType.RemoteControl:
                Debug.Log("  → Remote Control powerup activated! Closing doors...");
                UseRemoteControl();
                break;
            case DNAPowerUpPickup.DNAPowerUpType.Berserk:
                Debug.Log("  → Berserk powerup activated!");
                StartCoroutine(BerserkEffect());
                break;
            case DNAPowerUpPickup.DNAPowerUpType.SlimeShot:
                Debug.Log("  → Slime Shot powerup activated!");
                ShootSlime();
                break;
            case DNAPowerUpPickup.DNAPowerUpType.Shield:
                Debug.Log("  → Shield powerup activated!");
                StartCoroutine(ShieldEffect());
                break;
        }
    }

    public bool HasPowerUp() => hasDNAPowerUp;
    public DNAPowerUpPickup.DNAPowerUpType GetCurrentPowerUp() => currentDNAPowerUp;

    public void ReceiveDNAPowerUp(DNAPowerUpPickup.DNAPowerUpType type)
    {
        currentDNAPowerUp = type;
        hasDNAPowerUp = true;
        OnPowerUpGained?.Invoke(this, type);
        Debug.Log(gameObject.name + " recibio DNA powerup: " + type);
    }

    public void ClearPowerUpState()
    {
        hasDNAPowerUp = false;
        ClearActivePowerUpEffects();
        OnPowerUpUsed?.Invoke(this);
    }

    public void ClearActivePowerUpEffects()
    {
        // Si tiene caja, soltarla
        if (heldCrate != null)
        {
            heldCrate.DropAtPlace();
            heldCrate = null;
        }

        // Restaurar valores originales de salto y gravedad
        jumpForce = baseJumpForce;
        gravityScale = baseGravityScale;

        // Detener solo las corrutinas específicas, NO todas (StopAllCoroutines es peligroso)
        if (slimeCoroutine != null)
        {
            StopCoroutine(slimeCoroutine);
            slimeCoroutine = null;
        }

        // Restaurar escala y velocidad base
        transform.localScale = originalScale;
        moveSpeed = hasDNA ? baseMoveSpeed * 0.6f : baseMoveSpeed;

        // Salir del modo Berserk si estaba activo
        if (isBerserk)
        {
            isBerserk = false;
            // No es necesario volver a restaurar escala y velocidad porque ya se hizo arriba
        }

        isSlimed = false;
        jumpForce = baseJumpForce;
        gravityScale = baseGravityScale;
        moveSpeed = hasDNA ? baseMoveSpeed * 0.6f : baseMoveSpeed;

        SetShield(false, 1f);
    }

    private IEnumerator ResetHurtTrigger()
    {
        yield return new WaitForSeconds(0.1f); // Ajusta según la duración de la animación de daño
        animator?.ResetTrigger("Hurt");
    }

    //CAJAAAAAAA (Crate)

    public void Stun(float duration)
    {
        if (isInvulnerable) return;
        // Si ya esta stuneado, reiniciamos el timer al m�ximo entre el que le queda y el nuevo
        if (isStunned)
        {
            stunTimer = Mathf.Max(stunTimer, duration);
        }
        else
        {
            isStunned = true;
            stunTimer = duration;
            rb.linearVelocity = Vector2.zero;
            canAttack = false;
            if (heldCrate != null)
            {
                heldCrate.DropAtPlace();
                heldCrate = null;
            }
            animator?.SetTrigger("Stun");
        }
    }

    public void AddStunTime(float extra)
    {
        if (!isStunned) return;
        stunTimer += extra;
        Debug.Log(gameObject.name + " +" + extra + "s de stun extra. Total restante: " + stunTimer);
    }

    //POWER UPS EFFECTS

    public void SetShield(bool active, float multiplier)
    {
        shieldActive = active;
        shieldMultiplier = multiplier;
        
        if (shieldVFX != null)
        {
            shieldVFX.SetActive(active);
        }
    }
    private IEnumerator ShrinkEffect()
    {
        // Achicarse
        // animator?.SetTrigger("Shrink"); // Comentado: trigger no existe en Animator
        yield return new WaitForSeconds(shrinkAnimationDelay); // Esperar a que termine la animación de shrink
        transform.localScale = originalScale * shrinkScale;
        float speedBoost = baseMoveSpeed * shrinkSpeedMult;
        float previousSpeed = moveSpeed;
        moveSpeed = speedBoost;

        yield return new WaitForSeconds(shrinkDuration);

        // Volver al tamanio original
        // animator?.SetTrigger("Grow"); // Comentado: trigger no existe en Animator
        yield return new WaitForSeconds(growAnimationDelay); // Esperar a que termine la animación de grow
        transform.localScale = originalScale;
        moveSpeed = hasDNA ? baseMoveSpeed * 0.6f : baseMoveSpeed;
    }

    private IEnumerator ShieldEffect()
    {
        SetShield(true, 1.5f);   // multiplicador 1.5, puedes ajustarlo
        yield return new WaitForSeconds(shieldDuration);
        SetShield(false, 1f);
    }

    private void PlaceMine()
    {
        if (minePrefab == null) return;
        GameObject mine = Instantiate(minePrefab, transform.position, Quaternion.identity);
        mine.GetComponent<Mine>().Init(playerIndex + 1); // 1 o 2
    }

    public void ReceiveMineHit(Vector2 direction, float force, float stunDuration)
    {
        if (isInvulnerable) return;
        animator?.SetTrigger("Hurt");
        StartCoroutine(ResetHurtTrigger());
        rb.linearVelocity = direction * force;
        StartCoroutine(MineKnockback(stunDuration));
    }

    private IEnumerator MineKnockback(float stunDuration)
    {
        isKnockedBack = true;
        // bloquear input durante el stun
        float elapsed = 0f;
        while (elapsed < stunDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        isKnockedBack = false;
    }

    private void UseRemoteControl()
    {
        WallManager.Instance?.DeactivateAll();
        Debug.Log(gameObject.name + " cerró las puertas con el Remote Control");
    }

    private IEnumerator BerserkEffect()
    {
        isBerserk = true;
        animator?.SetBool("IsBerserk", true);
        transform.localScale = originalScale * berserkScale;
        moveSpeed = baseMoveSpeed * berserkSpeedMult;
        yield return new WaitForSeconds(berserkDuration);
        isBerserk = false;
        animator?.SetBool("IsBerserk", false);
        transform.localScale = originalScale;
        moveSpeed = hasDNA ? baseMoveSpeed * 0.6f : baseMoveSpeed;
       // ClearPowerUpState();  // Esto limpia el estado y notifica al HUD
    }

    public void ReceiveBerserkHit(Vector2 direction)
    {
        animator?.SetTrigger("Hurt");
        StartCoroutine(ResetHurtTrigger());
        rb.linearVelocity = direction * knockbackForce;
        StartCoroutine(BerserkStun());
    }

    private IEnumerator BerserkStun()
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(berserkStunDuration);
        isKnockedBack = false;
    }

    private void ShootSlime()
    {
        animator?.SetTrigger("SlimeShot");
    }

    // Llamado por Animation Event cuando debe disparar el slime
    public void FireSlime()
    {
        if (slimeProjectilePrefab == null) return;

        // Direcci�n seg�n para donde mira el jugador
        float dirX = IsFacingRight() ? 1f : -1f; //para donde veo? seguro que para el bulto de chori
        Vector2 direction = new Vector2(dirX, 0f);

        Quaternion rotation = Quaternion.Euler(0f, 0f, IsFacingRight() ? 180f : -180f);

        Vector3 spawnPos = transform.position + new Vector3(dirX * 0.5f, 0f, 0f);
        GameObject proj = Instantiate(slimeProjectilePrefab, spawnPos, rotation);
        proj.GetComponent<SlimeProjectile>().Init(direction, playerIndex + 1);
    }

    public void ApplySlimeEffect()
    {
        if (isSlimed) return;
        slimeCoroutine = StartCoroutine(SlimeEffect());
    }

    private IEnumerator SlimeEffect()
    {
        isSlimed = true;

        // Guardar valores originales
        float originalSpeed = moveSpeed;
        float originalJumpForce = jumpForce;
        float originalGravity = gravityScale;

        // Aplicar efecto
        moveSpeed = baseMoveSpeed * slimeSpeedMult;
        jumpForce = originalJumpForce * slimeJumpForceReduction;
        gravityScale = originalGravity * slimeGravityMult;

        yield return new WaitForSeconds(slimeDuration);

        // Restaurar
        moveSpeed = hasDNA ? baseMoveSpeed * 0.75f : baseMoveSpeed;
        jumpForce = originalJumpForce;
        gravityScale = originalGravity;
        isSlimed = false;
    }  

    public bool IsFacingRight() => !sr.flipX;

    // Mutant DNA
    public bool HasDNA() => hasDNA;
    public void PickDNA(DNA dna)
    {
        if (IsCarryingSomething()) return;
        if (hasDNA) return;
        hasDNA = true;
        carriedDNA = dna;
        dna.PickUp(this);
       // moveSpeed = baseMoveSpeed * 0.6f;
    }
    public void DropDNA()
    {
        if (!hasDNA) return;
        Debug.Log($"{gameObject.name} DropDNA - antes: hasDNA={hasDNA}, moveSpeed={moveSpeed}");
        hasDNA = false;
        if (carriedDNA != null)
        {
            carriedDNA.Drop();
            carriedDNA = null;
        }
        moveSpeed = baseMoveSpeed;
    }
    private void ThrowDNA()
    {
        if (!hasDNA) return;
        float playerSpeed = Mathf.Abs(rb.linearVelocity.x);
        float dirX = IsFacingRight() ? 1f : -1f;
        Vector2 direction = new Vector2(dirX, 0f);
        carriedDNA.Throw(direction, playerSpeed, playerIndex + 1); // playerIndex 0->1, 1->2
        carriedDNA = null;
        hasDNA = false;
        moveSpeed = baseMoveSpeed;
        
        /*if (playerSpeed >= 0.5f)
        {
            Debug.Log($"[MANOS] Throw DNA trigger activado - Hands Animator: {(handsAnimator != null ? "Asignado" : "NULL")}");
            handsAnimator?.SetTrigger("Throw");
            StartCoroutine(HideHandsAfterDelay());
        }
        else
        {
            SetHandsActive(false);
        }*/
    }

    private IEnumerator HideHandsAfterDelay()
    {
        yield return new WaitForSeconds(throwAnimationDelay);
        SetHandsActive(false);
    }
    public Transform GetCrateHoldPoint() => crateHoldPoint;
    public bool IsStunned() => isStunned;
    public DNA GetCarriedDNA() => carriedDNA;
    public bool IsBerserk() => isBerserk;
    public Transform GetDNAHoldPoint() => dnaHoldPoint;
    public bool IsShieldActive() => shieldActive;
    public bool IsCarryingSomething() => heldCrate != null || hasDNA;
    public bool IsCarryingCrate() => heldCrate != null;

    public void ForceDropCrate()
    {
        if (heldCrate != null)
        {
            heldCrate.DropAtPlace(); // Esto suelta la caja en el lugar actual del jugador
            heldCrate = null;
            SetHandsActive(false);
        }
    }

    public void SetSpawnPoint(Vector3 point)
    {
        spawnPoint = point;
    }
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (frozen) rb.linearVelocity = Vector2.zero;
    }
}