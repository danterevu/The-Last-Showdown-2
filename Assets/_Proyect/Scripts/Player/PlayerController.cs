using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f; public float MoveSpeed => moveSpeed;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravityScale = 3f;

    [Header("TopDown Movement")]
    [SerializeField] private float topDownSpeed = 5f;

    [Header("Movement Mode")]
    [SerializeField] private MovementMode movementMode = MovementMode.Platform;
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector2 lastDirection;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1_Platform";
    [SerializeField] private int playerIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool controlsInverted;
    [SerializeField] private bool isFrozen; // reemplaza el this.enabled = false

    private Animator anim;
    private Rigidbody2D rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;

    public enum MovementMode { Platform, TopDown, TopDownWithGravity }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActions no asignado en " + gameObject.name);
            return;
        }
        SetupInput(actionMapName);
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
        }
        interactAction?.Disable();
    }

    private void Update()
    {
        if (moveAction == null) return;

        // Debug temporal
        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        Debug.Log($"Jugador {playerIndex + 1} | Gamepad: {(gp != null ? gp.displayName : "NULL")} | Total asignados: {InputAssigner.AssignedCount}");

        moveInput = isFrozen ? Vector2.zero : ReadFilteredMove();
        UpdateAnimations(moveInput);
    }

    private void FixedUpdate()
    {
        if (isFrozen)
        {
            // mantener velocidad en 0 mientras está congelado
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (movementMode == MovementMode.Platform)
            HandlePlatformMovement();
        else if (movementMode == MovementMode.TopDown)
            HandleTopDownMovement();
        else if (movementMode == MovementMode.TopDownWithGravity)
            HandleTopDownWithGravity();
    }

    private void HandlePlatformMovement()
    {
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    private void HandleTopDownWithGravity()
    {
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    private void HandleTopDownMovement()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(moveInput.x * topDownSpeed, moveInput.y * topDownSpeed);
    }

    private Vector2 ReadFilteredMove()
    {
        Vector2 result = Vector2.zero;

        // Leer del gamepad asignado a este jugador
        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            Vector2 dpad = gp.dpad.ReadValue();
            result = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
            if (result.sqrMagnitude > 0.01f)
                return controlsInverted ? -result : result;
        }

        // Fallback teclado: P1 = WASD, P2 = flechas
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

        if (result.sqrMagnitude > 0.01f)
            result = result.normalized;

        return controlsInverted ? -result : result;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isFrozen) return; // no saltar si está congelado
        if (!IsCorrectDevice(context.control.device)) return;

        if (isGrounded)
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private bool IsCorrectDevice(InputDevice device)
    {
        if (device is Keyboard)
            return playerIndex == 0;

        if (device is Gamepad gamepad)
            return gamepad == InputAssigner.GetGamepadForPlayer(playerIndex);

        return false;
    }

    private void SetupInput(string mapName)
    {
        moveAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
        }
        interactAction?.Disable();

        var map = inputActions.FindActionMap(mapName);
        if (map == null)
        {
            Debug.LogError("Action Map no encontrado: " + mapName);
            return;
        }

        moveAction = map.FindAction("Move");
        jumpAction = map.FindAction("Jump");
        interactAction = map.FindAction("Interact");

        moveAction?.Enable();
        interactAction?.Enable();

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJump;
        }

        Debug.Log($"Jugador {playerIndex + 1}: input configurado con mapa '{mapName}'");
    }

    public void SetMovementMode(MovementMode mode, string mapName)
    {
        movementMode = mode;
        actionMapName = mapName;
        SetupInput(mapName);
    }

    public bool GetInteractPressed()
    {
        if (interactAction == null) return false;

        // Teclado
        if (Keyboard.current != null)
        {
            if (playerIndex == 0 && Keyboard.current.eKey.wasPressedThisFrame) return true;
            if (playerIndex == 1 && Keyboard.current.lKey.wasPressedThisFrame) return true;
        }

        // Gamepad — círculo (buttonEast) para ambos jugadores
        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null && gp.buttonEast.wasPressedThisFrame) return true;

        return false;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (frozen) rb.linearVelocity = Vector2.zero;
    }

    public void SetInvertControls(bool inverted)
    {
        controlsInverted = inverted;
    }

    private void UpdateAnimations(Vector2 input)
    {
        if (anim == null) return;

        bool isMoving = input.sqrMagnitude > 0.01f;
        anim.SetBool("isMoving", isMoving);
        anim.SetFloat("MoveX", input.x);
        anim.SetFloat("MoveY", input.y);

        if (isMoving)
            lastDirection = input.normalized;
    }
}