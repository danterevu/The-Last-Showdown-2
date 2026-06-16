using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class MinigameUiManager : MonoBehaviour
{
    public enum State { Inicio, EsperandoConfirmacion, Transicion, Final, Completado }

    [System.Serializable]
    public class ModifierUiData
    {
        public string title = "MODIFICADOR";
        [TextArea] public string description = "";
    }

    [System.Serializable]
    public class MinigameUiData
    {
        public MinigameID id;
        public string title = "MINIJUEGO";
        [TextArea] public string description = "";
        [Tooltip("Mismo orden que los sectores de modificadores en la ruleta.")]
        public ModifierUiData[] modifiers = new ModifierUiData[3];
    }

    [Header("Datos de Minijuegos y Modificadores")]
    [SerializeField] private MinigameUiData[] minigamesData;

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

    private Dictionary<MinigameID, MinigameUiData> minigamesLookup;

    private void Start()
    {
        BuildLookup();

        int minigameId = PlayerPrefs.GetInt("SelectedMinigame", 1);
        int modIndex = PlayerPrefs.GetInt("SelectedModifier", 0);

        Debug.Log($"[MinigameUiManager] SelectedMinigame leido de PlayerPrefs: {minigameId} | SelectedModifier: {modIndex}");

        SetMinigameTexts((MinigameID)minigameId);
        SetModifierTexts((MinigameID)minigameId, modIndex);

        InitializeState();
    }

    private void BuildLookup()
    {
        minigamesLookup = new Dictionary<MinigameID, MinigameUiData>();
        if (minigamesData == null) return;

        foreach (var data in minigamesData)
        {
            if (data == null) continue;
            if (minigamesLookup.ContainsKey(data.id))
            {
                Debug.LogWarning($"[MinigameUiManager] MinigameID duplicado en minigamesData: {data.id}");
                continue;
            }
            minigamesLookup.Add(data.id, data);
        }
    }

    private void InitializeState()
    {
        currentState = State.Inicio;
        player1Ready = false;
        player2Ready = false;
        transicionTimer = 0f;

        UpdateStatusTexts();
        ActivatePanelInicio();
    }

    private void ActivatePanelInicio()
    {
        if (animatorInicio != null)
            animatorInicio.SetTrigger("Inicio");

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

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (!player1Ready)
                p1Confirm = keyboard.spaceKey.wasPressedThisFrame;

            if (!player2Ready)
                p2Confirm = keyboard.enterKey.wasPressedThisFrame;
        }

        if (useGamepad)
        {
            Gamepad p1Gamepad = InputAssigner.GetGamepadForPlayer(0);
            Gamepad p2Gamepad = InputAssigner.GetGamepadForPlayer(1);

            if (!player1Ready && p1Gamepad != null && p1Gamepad.buttonSouth.wasPressedThisFrame)
                p1Confirm = true;

            if (!player2Ready && p2Gamepad != null && p2Gamepad.buttonSouth.wasPressedThisFrame)
                p2Confirm = true;
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

    private void SetMinigameTexts(MinigameID id)
    {
        if (!minigamesLookup.TryGetValue(id, out MinigameUiData data))
        {
            Debug.LogWarning($"[MinigameUiManager] No se encontró data para MinigameID: {id}");
            if (minigameTitleText != null) minigameTitleText.text = "MINIJUEGO";
            if (minigameDescText != null) minigameDescText.text = "";
            return;
        }

        if (minigameTitleText != null) minigameTitleText.text = data.title;
        if (minigameDescText != null) minigameDescText.text = data.description;
    }

    private void SetModifierTexts(MinigameID id, int modIndex)
    {
        string title = "SIN MODIFICADOR";
        string desc = "";

        if (minigamesLookup.TryGetValue(id, out MinigameUiData data) &&
            data.modifiers != null &&
            modIndex >= 0 &&
            modIndex < data.modifiers.Length &&
            data.modifiers[modIndex] != null)
        {
            title = data.modifiers[modIndex].title;
            desc = data.modifiers[modIndex].description;
        }
        else
        {
            Debug.LogWarning($"[MinigameUiManager] No se encontró modificador {modIndex} para MinigameID: {id}");
        }

        if (modifierTitleText != null) modifierTitleText.text = title;
        if (modifierDescText != null) modifierDescText.text = desc;
    }
}