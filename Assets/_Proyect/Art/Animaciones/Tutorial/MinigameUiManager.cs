using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class MinigameUiManager : MonoBehaviour
{
    public enum State { Inicio, EsperandoConfirmacion, Transicion, Final, Completado }

    // [Header("Paneles")] - No necesitamos paneles separados, todo está en el mismo GameObject
    // [SerializeField] private GameObject panelInicio;
    // [SerializeField] private GameObject panelTransicion;

    [Header("Animaciones")]
    [SerializeField] private Animator animatorInicio;
    [SerializeField] private Animator animatorTransicion;
    [SerializeField] private Animator animatorFinal;

    [Header("Textos de Minijuego")]
    [SerializeField] private TextMeshProUGUI minigameTitleText;
    [SerializeField] private TextMeshProUGUI minigameDescText;
    [SerializeField] private TextMeshProUGUI modifierTitleText;
    [SerializeField] private TextMeshProUGUI modifierDescText;

    [Header("Controles de Confirmación")]
    [SerializeField] private KeyCode player1ConfirmKey = KeyCode.Space;
    [SerializeField] private KeyCode player2ConfirmKey = KeyCode.Return;
    [SerializeField] private bool useGamepad = true;

    [Header("Duración de Transición")]
    [SerializeField] private float transicionDuration = 3f;

    [Header("Duración de Animación Final")]
    [SerializeField] private float finalAnimationDuration = 1f;

    [Header("Textos de Estado")]
    [SerializeField] private TextMeshProUGUI player1StatusText;
    [SerializeField] private TextMeshProUGUI player2StatusText;
    [SerializeField] private string readyText = "¡Listo!";
    [SerializeField] private string waitingText = "Esperando...";

    [Header("Eventos")]
    [SerializeField] private UnityEngine.Events.UnityEvent onCompletado;

    private State currentState = State.Inicio;
    private bool player1Ready = false;
    private bool player2Ready = false;
    private float transicionTimer = 0f;

    // Datos de minijuegos (para compatibilidad con versión anterior)
    private static readonly string[] minigameTitles = new string[]
    {
        "",           // índice 0 vacío
        "DODGE DISK", // 1
        "KING OF THE HILL", // 2
        "DNA",           // 3
        "SPACE BATTLE",           // 4
        "CHASE RUN" // 5
    };

    private static readonly string[] minigameDescs = new string[]
    {
        "",
        "¡Esquivá el disco y sobreviví!",
        "¡Dominá la zona y acumulá puntos!",
        "",
        "",
        "¡Eliminá a tu rival en el espacio!"
    };

    private static readonly string[][] modifierTitles = new string[][]
    {
        new string[] {},                                              // índice 0
        new string[] { "BONUS KILL", "BONUS DEATH", "BONUS WINNER" }, // 1
        new string[] { "COMEBACK x3", "BONUS HARDPOINT", "POINT BLEED" }, // 2
        new string[] {"GOLDEN KILL", "COMBO ROUNDS", "SIN MODIFICADOR" },                                              // 3
        new string[] {},                                              // 4
        new string[] { } // 5
    };

    private static readonly string[][] modifierDescs = new string[][]
    {
        new string[] {},
        new string[] {
            "Matar con un power up da puntos extra",
            "Morir suma puntos al rival",
            "El ganador recibe un bonus al final"
        },
        new string[] {
            "El que va perdiendo tiene multiplicador x3",
            "Más tiempo en zona = más puntos",
            "Fuera de la zona perdés puntos por segundo"
        },
        new string[] {},
        new string[] {},
        new string[] {
            "La primera kill de la ronda vale el triple",
            "Ganar rondas seguidas da multiplicador",
            "Esta ronda no tiene modificador"
        }
    };

    private void Start()
    {
        Debug.Log("MinigameUiManager: Start() llamado");

        // Cargar datos de minijuego y modificador (para compatibilidad)
        int minigameId = PlayerPrefs.GetInt("SelectedMinigame", 1);
        int modIndex = PlayerPrefs.GetInt("SelectedModifier", 0);
        SetMinigameTexts(minigameId);
        SetModifierTexts(minigameId, modIndex);

        // Inicializar estado
        InitializeState();
    }

    private void InitializeState()
    {
        // Empezar con el estado Inicio
        currentState = State.Inicio;
        player1Ready = false;
        player2Ready = false;
        transicionTimer = 0f;

        UpdateStatusTexts();
        ActivatePanelInicio();
    }

    private void ActivatePanelInicio()
    {
        Debug.Log("MinigameUiManager: ActivatePanelInicio() - animatorInicio es null? " + (animatorInicio == null));

        if (animatorInicio != null)
        {
            animatorInicio.SetTrigger("Inicio");
            Debug.Log("MinigameUiManager: Trigger 'Inicio' activado");
        }

        currentState = State.EsperandoConfirmacion;
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.EsperandoConfirmacion:
                HandleConfirmacion();
                break;
            case State.Transicion:
                HandleTransicion();
                break;
        }
    }

    private void HandleConfirmacion()
    {
        bool p1Confirm = false;
        bool p2Confirm = false;

        // Verificar teclado
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (!player1Ready)
                p1Confirm = keyboard.spaceKey.wasPressedThisFrame;

            if (!player2Ready)
                p2Confirm = keyboard.enterKey.wasPressedThisFrame;
        }

        // Verificar gamepad
        if (useGamepad)
        {
            var gamepads = Gamepad.all;
            foreach (var gamepad in gamepads)
            {
                if (gamepad != null)
                {
                    if (!player1Ready && gamepad.buttonSouth.wasPressedThisFrame)
                        p1Confirm = true;
                    if (!player2Ready && gamepad.buttonSouth.wasPressedThisFrame)
                        p2Confirm = true;
                }
            }
        }

        if (p1Confirm)
        {
            player1Ready = true;
            UpdateStatusTexts();
        }

        if (p2Confirm)
        {
            player2Ready = true;
            UpdateStatusTexts();
        }

        if (player1Ready && player2Ready)
        {
            PasarATransicion();
        }
    }

    private void PasarATransicion()
    {
        currentState = State.Transicion;
        transicionTimer = transicionDuration;

        if (animatorTransicion != null)
        {
            animatorTransicion.SetTrigger("Transicion");
        }
    }

    private void HandleTransicion()
    {
        transicionTimer -= Time.deltaTime;

        if (transicionTimer <= 0f)
        {
            PasarAFinal();
        }
    }

    private void PasarAFinal()
    {
        currentState = State.Final;

        if (animatorFinal != null)
        {
            animatorFinal.SetTrigger("Final");
        }

        // Esperar a que termine la animación Final antes de marcar como completado
        Invoke(nameof(MarcarCompletado), finalAnimationDuration);
    }

    private void MarcarCompletado()
    {
        currentState = State.Completado;
        onCompletado?.Invoke();
    }

    private void UpdateStatusTexts()
    {
        if (player1StatusText != null)
            player1StatusText.text = player1Ready ? readyText : waitingText;

        if (player2StatusText != null)
            player2StatusText.text = player2Ready ? readyText : waitingText;
    }

    // Métodos públicos para control desde el Inspector o eventos
    public void SetTransicionDuration(float duration)
    {
        transicionDuration = duration;
    }

    public void Reiniciar()
    {
        InitializeState();
    }

    public void ForzarCompletado()
    {
        PasarAFinal();
    }

    
    private void SetMinigameTexts(int id)
    {
        if (minigameTitleText != null)
            minigameTitleText.text = (id < minigameTitles.Length)
                ? minigameTitles[id] : "MINIJUEGO";

        if (minigameDescText != null)
            minigameDescText.text = (id < minigameDescs.Length)
                ? minigameDescs[id] : "";
    }

    private void SetModifierTexts(int minigameId, int modIndex)
    {
        string title = "SIN MODIFICADOR";
        string desc = "";

        // Verificar con seguridad todos los índices
        if (minigameId >= 0 && 
            minigameId < modifierTitles.Length && 
            minigameId < modifierDescs.Length &&
            modIndex >= 0 &&
            modIndex < modifierTitles[minigameId].Length &&
            modIndex < modifierDescs[minigameId].Length)
        {
            title = modifierTitles[minigameId][modIndex];
            desc = modifierDescs[minigameId][modIndex];
        }

        if (modifierTitleText != null) modifierTitleText.text = title;
        if (modifierDescText != null) modifierDescText.text = desc;
    }
}
