using Unity.VisualScripting;
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
    [SerializeField] private InputActionAsset inputActions; //Es el archivo donde están guardados la configuracion de botones
    [SerializeField] private string actionMapName = "Player1_Platform"; //se pasa por un string cual InputActionAsset se va a usar
   
    [Header("Debug")]
    [SerializeField] private bool isGrounded;
   
    


    private Animator anim;
    private Rigidbody2D rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    

    public enum MovementMode { Platform, TopDown } //se definen dos estados de juego, estilo plataformero o top down

    private void Awake()
    {
        anim= GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActions no asignado en " + gameObject.name);
            return;
        }
        SetupInput(actionMapName); //para activar una configuracion de controles, se le pasa a al metodo SetupInput,
                                   //el string "Player1_Platform"
    }

    private void OnDisable()
    {
        moveAction?.Disable(); //el ?. significa Si existe, ejecútalo"
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump; // Lo desconecta (-=)
            jumpAction.Disable(); // desconecta los inputs
            interactAction?.Disable(); // se desactiva si existe
        }
        //cuando el jugador salta, ejecutar OnJump()
    }

    private void Update() //lectura de input, no mueve al PJ TODAVIA
    {
        if (moveAction == null) return;
        moveInput = moveAction.ReadValue<Vector2>(); //devuelve algo como: (1, 0), (-1, 0), (0, 1), (0, -1)

        UpdateAnimations(moveInput);

    }

    private void FixedUpdate() //se fija que modo de input se esta usando
    {
        if (movementMode == MovementMode.Platform)
            HandlePlatformMovement();
        else
            HandleTopDownMovement();
    }

    private void HandlePlatformMovement() //logica base de un plataformero
    {
        rb.gravityScale = gravityScale; //hay gravedad
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y); 
    }

    private void HandleTopDownMovement() //logica base de un top down
    {
        rb.gravityScale = 0f; //gravedad 0
        Vector2 normalized = moveInput.normalized; //move input normalizado
        rb.linearVelocity = new Vector2(moveInput.x * topDownSpeed, moveInput.y * topDownSpeed);
       
    }

    private void OnJump(InputAction.CallbackContext context) //maneja el salto
    {
        if (isGrounded) //si esta en el piso
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse); //en Vector.up(0 , 1) se le da fueza en Y con un impulso
    }


    private void SetupInput(string mapName)
    {
        moveAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
        }

        var map = inputActions.FindActionMap(mapName); //se guarda en un var la busqueda del string mapName: "Player1_Platform"
        if (map == null)
        {
            Debug.LogError("Action Map no encontrado: " + mapName);
            return;
        }

        moveAction = map.FindAction("Move");
        jumpAction = map.FindAction("Jump");
        interactAction = map.FindAction("Interact"); //Estas deben existir en el Input System

        interactAction?.Enable();
        moveAction?.Enable(); //se activan

        if (jumpAction != null) //si existe
        {
            jumpAction.Enable();
            jumpAction.performed += OnJump; //Cuando presionás salto, llama a OnJump()
        }
    }

    public void SetMovementMode(MovementMode mode, string mapName) //Cambia el tipo de movimiento y Cambia los controles
    {
        movementMode = mode;
        actionMapName = mapName;
        SetupInput(mapName);
    }

    public bool GetInteractPressed()
    {
        return interactAction != null && interactAction.WasPressedThisFrame(); //TRUE si el jugador presionó interact en ese frame
    }
    public void SetFrozen(bool frozen)
    {
        if (frozen) //si esta congelado, el movimiento es (0,0)
            rb.linearVelocity = Vector2.zero;

        this.enabled = !frozen;
    }
    private void UpdateAnimations(Vector2 moveInput)
    {
        if (anim == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        anim.SetBool("isMoving", isMoving);

        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);

        if (isMoving)
        {
            lastDirection = moveInput.normalized;
        }
    }
}