using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody2D))]
public class ChaseRunPlayerController : MonoBehaviour
{
    // Identidad
    [Header("Jugador")]
    [SerializeField] private int playerNumber = 1;

    // Input 
    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    // Movimiento 
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    // Salto 
    [Header("Salto")]
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private float fallMultiplier = 1.6f;
    [SerializeField] private float jumpCutMultiplier = 3f;

    //  Wall Slide 
    [Header("Wall Slide")]
    [Tooltip("Multiplicador de velocityY en el PRIMER frame de contacto. " +
             "Más alto = frena más fuerte al pegarse. Rango sugerido: 0.7–0.95")]
    [SerializeField] private float wallInitialFriction = 0.85f;

    [Tooltip("Reducción de velocityY por frame mientras se desliza  " +
             "Más alto = cae mas lento. Rango sugerido: 0.08–0.25")]
    [SerializeField] private float wallSlideFriction = 0.12f;

    [Tooltip("Velocidad máxima de caída durante el wall slide (valor negativo)")]
    [SerializeField] private float wallSlideMinFallSpeed = -2f;

    [Tooltip("Fuerza horizontal del wall jump (se aleja de la pared)")]
    [SerializeField] private float wallJumpForceX = 12f;
    [Tooltip("Fuerza vertical del wall jump")]
    [SerializeField] private float wallJumpForceY = 16f;

    //  Ground / Wall Check 
    [Header("Detección de suelo y pared")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private float checkRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    //  Respawn 
    [Header("Respawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnInvincibilityTime = 1.5f;

    //  Referencias 
    private Rigidbody2D rb;
    private ChaseRunManager manager;
    private ChaseRunCamera chaseCamera;

    //  Estado de movimiento 
    private Vector2 moveInput;
    private float velocityX;
    private float velocityY;
    private int jumpsRemaining;
    private bool isGrounded;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private bool isWallSliding;
    private bool wasWallSliding;
    private int wallSlideSide;       // -1 = izquierda, 1 = derecha
    private bool jumpBuffered;
    private float jumpBufferTimer;
    private const float JUMP_BUFFER_TIME = 0.12f;

    // Estado de power ups 
    private float speedMultiplier = 1f;
    private bool killZoneImmune = false;
    private int extraJumps = 0;

    //  Kill zone 
    private bool isInvincible;
    private ChaseRunManager.RunPhase currentPhase;

    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(ChaseRunManager mgr)
    {
        manager = mgr;
        chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();
        currentPhase = ChaseRunManager.RunPhase.PhaseY;
        jumpsRemaining = maxJumps;
        SetupInput();
    }

    private void SetupInput()
    {
        if (playerInput == null) return;

        string mapName = playerNumber == 1 ? "Player1_Platform" : "Player2_Platform";
        playerInput.SwitchCurrentActionMap(mapName);

        moveAction = playerInput.actions[mapName + "/Move"];
        jumpAction = playerInput.actions[mapName + "/Jump"];

        jumpAction.performed += OnJumpPerformed;
        jumpAction.canceled += OnJumpCanceled;
    }

    private void OnDestroy()
    {
        if (jumpAction == null) return;
        jumpAction.performed -= OnJumpPerformed;
        jumpAction.canceled -= OnJumpCanceled;
    }

    public void OnPhaseChanged(ChaseRunManager.RunPhase newPhase)
    {
        currentPhase = newPhase;
    }

   

    private void Update()
    {
        if (moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();

        // Bajar el buffer timer
        if (jumpBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0f) jumpBuffered = false;
        }

        CheckGrounded();
        CheckWalls();
        HandleWallSlide();
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyGravity();
        ApplyJumpBuffer();

        rb.linearVelocity = new Vector2(velocityX, velocityY);

        if (!isInvincible && !killZoneImmune && chaseCamera != null)
            CheckKillZone();
    }

    // Suelo y paredes 

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded && !wasGrounded)
            jumpsRemaining = maxJumps + extraJumps;
    }

    private void CheckWalls()
    {
        isTouchingWallLeft = wallCheckLeft != null &&
                              Physics2D.OverlapCircle(wallCheckLeft.position, checkRadius, wallLayer);
        isTouchingWallRight = wallCheckRight != null &&
                              Physics2D.OverlapCircle(wallCheckRight.position, checkRadius, wallLayer);
    }

    // Wall Slide 

    private void HandleWallSlide()
    {
        bool pressingLeft = moveInput.x < -0.1f && isTouchingWallLeft;
        bool pressingRight = moveInput.x > 0.1f && isTouchingWallRight;
        bool pressingIntoWall = pressingLeft || pressingRight;

        isWallSliding = pressingIntoWall && !isGrounded && velocityY < 0f;

        if (isWallSliding)
        {
            wallSlideSide = isTouchingWallLeft ? -1 : 1;

            if (!wasWallSliding)
            {
                // Primer frame: frenada inicial fuerte
                velocityY *= (1f - wallInitialFriction);
                wasWallSliding = true;
                // Pegarse a la pared resetea los saltos disponibles
                jumpsRemaining = maxJumps + extraJumps;
            }
            else
            {
                // Deslizamiento continuo: fricción suave y constante
                velocityY -= wallSlideFriction * Time.deltaTime * 60f;
            }

            velocityY = Mathf.Max(velocityY, wallSlideMinFallSpeed);
        }
        else
        {
            wasWallSliding = false;
        }
    }

    // Movimiento horizontal

    private void ApplyHorizontalMovement()
    {
        float targetX = moveInput.x * moveSpeed * speedMultiplier;
        float rate = Mathf.Abs(targetX) > 0.01f ? acceleration : deceleration;
        velocityX = Mathf.MoveTowards(velocityX, targetX, rate * Time.fixedDeltaTime);
    }

    // Gravedad manual 

    private void ApplyGravity()
    {
        if (isGrounded && velocityY < 0f)
        {
            velocityY = -0.5f;
            return;
        }

        if (isWallSliding) return; // el wall slide maneja la velocidad Y

        float grav = gravity;
        if (velocityY < 0f) grav *= fallMultiplier;

        velocityY += grav * Time.fixedDeltaTime;
    }

    //  Salto

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpBuffered = true;
        jumpBufferTimer = JUMP_BUFFER_TIME;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (velocityY > 0f)
            velocityY -= velocityY * (1f - 1f / jumpCutMultiplier);
    }

    private void ApplyJumpBuffer()
    {
        if (!jumpBuffered) return;

        if (isWallSliding)
        {
            DoWallJump();
            jumpBuffered = false;
        }
        else if (isGrounded || jumpsRemaining > 0)
        {
            DoJump();
            jumpBuffered = false;
        }
    }

    private void DoJump()
    {
        velocityY = jumpForce;
        jumpsRemaining = Mathf.Max(0, jumpsRemaining - 1);
        isGrounded = false;
    }

    private void DoWallJump()
    {
        velocityX = -wallSlideSide * wallJumpForceX;
        velocityY = wallJumpForceY;
        isWallSliding = false;
        wasWallSliding = false;
        jumpsRemaining = Mathf.Max(0, maxJumps + extraJumps - 1);
    }

    // Kill Zone 

    private void CheckKillZone()
    {
        float killBound = chaseCamera.GetKillZoneBound();

        bool dead = currentPhase == ChaseRunManager.RunPhase.PhaseY
            ? transform.position.y < killBound
            : transform.position.x < killBound;

        if (dead) Respawn();
    }

    private void Respawn()
    {
        if (spawnPoint == null) return;

        transform.position = spawnPoint.position;
        velocityX = 0f;
        velocityY = 0f;
        rb.linearVelocity = Vector2.zero;
        jumpsRemaining = maxJumps + extraJumps;
        isWallSliding = false;
        wasWallSliding = false;

        StartCoroutine(InvincibilityFrames());
    }

    private System.Collections.IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(respawnInvincibilityTime);
        isInvincible = false;
    }

    // Spawn point (actualizado por SpawnPointUpdater) 

    public void SetSpawnPoint(Vector3 pos)
    {
        if (spawnPoint != null) spawnPoint.position = pos;
    }

    // Meta 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
            manager?.PlayerReachedGoal(playerNumber);
    }

    //  Power Up 

    public void ApplySpeedMultiplier(float multiplier) => speedMultiplier = multiplier;
    public void SetKillZoneImmunity(bool immune) => killZoneImmune = immune;

    public void AddExtraJump()
    {
        extraJumps++;
        jumpsRemaining++;
    }

    public void RemoveExtraJump()
    {
        extraJumps = Mathf.Max(0, extraJumps - 1);
    }

    //  Getters 

    public int PlayerNumber => playerNumber;
    public bool IsGrounded => isGrounded;
    public bool IsWallSliding => isWallSliding;
}
