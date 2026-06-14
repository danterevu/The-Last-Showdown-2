using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

public class InputAssigner : MonoBehaviour
{
    public enum InputType { None, Gamepad, Keyboard }
    public enum SelectionPhase { Player1Selecting, Player2Selecting, SelectionComplete }
    public enum Character { None, Gloppk, Chopi } // Gloppk = izquierda, Chopi = derecha

    [System.Serializable]
    public class CharacterVisual
    {
        public GameObject slotObject;
        public SpriteRenderer characterSprite;
        public Sprite idleSprite;
        public Sprite selectingKeyboardSprite;
        public Sprite selectingGamepadSprite;
        public Sprite confirmedKeyboardSprite;
        public Sprite confirmedGamepadSprite;
        public Color inactiveColor = Color.gray;
        public Color activeColor = Color.white;
        public TextMeshProUGUI statusText;
        public Vector3 inactiveScale = Vector3.one;
        public Vector3 activeScale = Vector3.one * 1.2f;
        public float animationDuration = 0.3f;
        public Ease easeType = Ease.OutBack;
    }
    
    [System.Serializable]
    public class PlayerSlot
    {
        [HideInInspector] public InputType assignedInput = InputType.None;
        [HideInInspector] public Gamepad assignedGamepad;
        [HideInInspector] public bool isSelecting = false;
        [HideInInspector] public InputType currentSelectionType = InputType.None;
        [HideInInspector] public Gamepad currentSelectionGamepad = null;
        [HideInInspector] public bool isLocked = false;
        [HideInInspector] public bool isUsingSecondKeyboard = false;
        [HideInInspector] public Character selectedCharacter = Character.None; // qué eligió
    }

    [System.Serializable]
    public class PlayerSlotData
    {
        public InputType inputType;
        public Gamepad gamepad;
        public Character character; // personaje elegido
        public int internalPlayerIndex; // índice interno real (1=Gloppk, 2=Chopi)
    }

    public static List<PlayerSlotData> assignedPlayers = new List<PlayerSlotData>();

    public static PlayerSlotData GetPlayerData(int playerIndex)
    {
        if (assignedPlayers.Count > playerIndex)
            return assignedPlayers[playerIndex];
        return null;
    }

    public static Gamepad GetGamepadForPlayer(int playerIndex)
    {
        var data = GetPlayerData(playerIndex);
        return data?.gamepad;
    }

    /// <summary>
    /// Devuelve el input del jugador que controla internamente a Player1 (Gloppk).
    /// Usalo en minijuegos si necesitás saber qué mando/teclado mueve a Player1.
    /// </summary>
    public static PlayerSlotData GetInternalPlayer(int internalIndex)
    {
        foreach (var p in assignedPlayers)
            if (p.internalPlayerIndex == internalIndex) return p;
        return null;
    }

    public static void Clear() => assignedPlayers.Clear();

    [Header("Player Slots")]
    [SerializeField] private PlayerSlot player1Slot; // primer jugador en elegir
    [SerializeField] private PlayerSlot player2Slot; // segundo jugador en elegir

    [Header("Character Visuals")]
    [SerializeField] private CharacterVisual gloppkVisual;
    [SerializeField] private CharacterVisual chopiVisual;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private string player1PromptText = "Jugador 1: Elige tu personaje con A/D o gamepad";
    [SerializeField] private string player2PromptText = "Jugador 2: Elige tu personaje con flechas o gamepad";
    [SerializeField] private GameObject player1ControlsImage;
    [SerializeField] private GameObject player2ControlsImage;

    [Header("Visual Objects")]
    [SerializeField] private GameObject keyboardVisual;
    [SerializeField] private GameObject keyboardVisual2;
    [SerializeField] private GameObject gamepadVisual;

    [Header("Spawn Points")]
    [SerializeField] private Transform centerSpawnPoint;
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    private SelectionPhase currentPhase = SelectionPhase.Player1Selecting;
    private List<Gamepad> usedGamepads = new List<Gamepad>();
    private bool isControlsPanelManuallyClosed = false;
    private bool forceControlsPanelOpen = false;
    private bool hasAutoOpenedControlsPanel = false;
    private bool bothAssignedPrevious = false;

    private void OnEnable() => InitializeAll();
    private void Start() => InitializeAll();

    private CharacterVisual GetVisual(Character character)
    {
        if (character == Character.Gloppk) return gloppkVisual;
        if (character == Character.Chopi) return chopiVisual;
        return null;
    }

    private void InitializeAll()
    {
        assignedPlayers.Clear();
        usedGamepads.Clear();
        currentPhase = SelectionPhase.Player1Selecting;
        ResetSlot(player1Slot);
        ResetSlot(player2Slot);
        ResetCharacterVisual(gloppkVisual);
        ResetCharacterVisual(chopiVisual);
        isControlsPanelManuallyClosed = true;
        forceControlsPanelOpen = false;
        hasAutoOpenedControlsPanel = false;
        bothAssignedPrevious = false;
        if (controlsPanel != null) controlsPanel.SetActive(false);
        InitializeTurnUI();
        UpdateUI();
        InitializeVisuals();
        while (assignedPlayers.Count < 2)
            assignedPlayers.Add(new PlayerSlotData());
    }
    
    private void ResetCharacterVisual(CharacterVisual visual)
    {
        if (visual == null) return;
        
        if (visual.slotObject != null)
            visual.slotObject.transform.DOScale(visual.inactiveScale, visual.animationDuration).SetEase(visual.easeType);

        if (visual.characterSprite != null)
        {
            if (visual.idleSprite != null) visual.characterSprite.sprite = visual.idleSprite;
            visual.characterSprite.color = visual.inactiveColor;
        }

        if (visual.statusText != null) visual.statusText.text = "";
    }

    private void InitializeVisuals()
    {
        SetVisualAtCenter(keyboardVisual);
        SetVisualAtCenter(keyboardVisual2);
        SetVisualAtCenter(gamepadVisual);
    }

    private void SetVisualAtCenter(GameObject visual)
    {
        if (visual != null && centerSpawnPoint != null)
        {
            visual.SetActive(true);
            visual.transform.position = centerSpawnPoint.position;
        }
    }

    private void InitializeTurnUI()
    {
        if (player1ControlsImage != null) player1ControlsImage.SetActive(false);
        if (player2ControlsImage != null) player2ControlsImage.SetActive(false);
    }

    private void UpdateTurnUI()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            if (currentPhase == SelectionPhase.Player1Selecting)
                promptText.text = player1PromptText;
            else if (currentPhase == SelectionPhase.Player2Selecting)
                promptText.text = player2PromptText;
            else
                promptText.gameObject.SetActive(false);
        }

        if (player1ControlsImage != null)
            player1ControlsImage.SetActive(currentPhase == SelectionPhase.Player1Selecting &&
                                           player1Slot.assignedInput == InputType.None);
        if (player2ControlsImage != null)
            player2ControlsImage.SetActive(currentPhase == SelectionPhase.Player2Selecting &&
                                           player2Slot.assignedInput == InputType.None);
    }

    private void Update()
    {
        if (currentPhase == SelectionPhase.Player1Selecting)
            HandlePlayerTurn(player1Slot, isFirstPlayer: true);
        else if (currentPhase == SelectionPhase.Player2Selecting)
            HandlePlayerTurn(player2Slot, isFirstPlayer: false);

        UpdateTurnUI();
        UpdateUI();
    }

    // ─── Lógica unificada de turno ────────────────────────────────────

    private void HandlePlayerTurn(PlayerSlot slot, bool isFirstPlayer)
    {
        var keyboard = Keyboard.current;

        // Y / Triangle = toggle controles (siempre)
        foreach (var gp in Gamepad.all)
        {
            if (gp != null && gp.buttonNorth.wasPressedThisFrame)
            {
                ToggleControlsPanel();
                return;
            }
        }

        if (slot.assignedInput == InputType.None)
        {
            if (!slot.isSelecting)
            {
                // B / Escape sin nada = volver / deshacer al anterior
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    HandleBackPress(isFirstPlayer);
                    return;
                }
                foreach (var gp in Gamepad.all)
                {
                    if (gp != null && gp.buttonEast.wasPressedThisFrame)
                    {
                        HandleBackPress(isFirstPlayer);
                        return;
                    }
                }

                // ── Teclado: detectar izquierda o derecha ──
                if (keyboard != null)
                {
                    bool leftKey = isFirstPlayer ? keyboard.aKey.wasPressedThisFrame
                                                  : keyboard.leftArrowKey.wasPressedThisFrame;
                    bool rightKey = isFirstPlayer ? keyboard.dKey.wasPressedThisFrame
                                                  : keyboard.rightArrowKey.wasPressedThisFrame;

                    if (leftKey)
                    {
                        Character desiredChar = Character.Gloppk;
                        if (!IsCharacterTaken(desiredChar, isFirstPlayer))
                        {
                            slot.selectedCharacter = desiredChar;
                            StartSelection(slot, InputType.Keyboard, null, !isFirstPlayer);
                            return;
                        }
                    }
                    if (rightKey)
                    {
                        Character desiredChar = Character.Chopi;
                        if (!IsCharacterTaken(desiredChar, isFirstPlayer))
                        {
                            slot.selectedCharacter = desiredChar;
                            StartSelection(slot, InputType.Keyboard, null, !isFirstPlayer);
                            return;
                        }
                    }
                }

                // ── Gamepad libre: detectar izquierda o derecha ──
                foreach (var gamepad in Gamepad.all)
                {
                    if (gamepad == null || usedGamepads.Contains(gamepad)) continue;

                    bool left = gamepad.leftStick.left.wasPressedThisFrame || gamepad.dpad.left.wasPressedThisFrame;
                    bool right = gamepad.leftStick.right.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame;

                    if (left)
                    {
                        Character desiredChar = Character.Gloppk;
                        if (!IsCharacterTaken(desiredChar, isFirstPlayer))
                        {
                            slot.selectedCharacter = desiredChar;
                            StartSelection(slot, InputType.Gamepad, gamepad, false);
                            return;
                        }
                    }
                    if (right)
                    {
                        Character desiredChar = Character.Chopi;
                        if (!IsCharacterTaken(desiredChar, isFirstPlayer))
                        {
                            slot.selectedCharacter = desiredChar;
                            StartSelection(slot, InputType.Gamepad, gamepad, false);
                            return;
                        }
                    }
                }
            }
            else
            {
                // Está seleccionando → confirmar o cancelar
                bool confirm = false;
                bool cancel = false;

                if (slot.currentSelectionType == InputType.Keyboard && keyboard != null)
                {
                    confirm = isFirstPlayer ? keyboard.spaceKey.wasPressedThisFrame
                                           : keyboard.enterKey.wasPressedThisFrame;
                    cancel = isFirstPlayer ? keyboard.escapeKey.wasPressedThisFrame
                                           : keyboard.backspaceKey.wasPressedThisFrame;
                }
                else if (slot.currentSelectionType == InputType.Gamepad &&
                         slot.currentSelectionGamepad != null)
                {
                    confirm = slot.currentSelectionGamepad.buttonSouth.wasPressedThisFrame ||
                              slot.currentSelectionGamepad.startButton.wasPressedThisFrame;
                    cancel = slot.currentSelectionGamepad.buttonEast.wasPressedThisFrame ||
                              slot.currentSelectionGamepad.selectButton.wasPressedThisFrame;
                }

                if (confirm)
                {
                    ConfirmSelection(slot);
                    currentPhase = isFirstPlayer ? SelectionPhase.Player2Selecting
                                                 : SelectionPhase.SelectionComplete;
                }
                else if (cancel)
                {
                    CancelSelection(slot);
                }
            }
        }
        else
        {
            // Ya confirmó → solo puede cancelar
            bool cancel = false;

            if (slot.assignedInput == InputType.Keyboard && keyboard != null)
                cancel = isFirstPlayer ? keyboard.escapeKey.wasPressedThisFrame
                                       : keyboard.backspaceKey.wasPressedThisFrame;
            else if (slot.assignedInput == InputType.Gamepad && slot.assignedGamepad != null)
                cancel = slot.assignedGamepad.buttonEast.wasPressedThisFrame ||
                         slot.assignedGamepad.selectButton.wasPressedThisFrame;

            if (cancel)
            {
                CancelAssignment(slot);
                if (!isFirstPlayer) currentPhase = SelectionPhase.Player1Selecting;
            }
            else
            {
                currentPhase = isFirstPlayer ? SelectionPhase.Player2Selecting
                                             : SelectionPhase.SelectionComplete;
            }
        }
    }

    private void HandleBackPress(bool isFirstPlayer)
    {
        if (isFirstPlayer)
        {
            BackToMenu();
        }
        else
        {
            // Deshace la elección de P1 y vuelve a su turno
            if (player1Slot.assignedInput != InputType.None)
            {
                CancelAssignment(player1Slot);
                currentPhase = SelectionPhase.Player1Selecting;
            }
            else
            {
                BackToMenu();
            }
        }
    }
    
    private bool IsCharacterTaken(Character character, bool isFirstPlayer)
    {
        // Player 1 can choose anything
        if (isFirstPlayer) return false;
        // Player 2 can't choose what Player 1 already chose
        return player1Slot.selectedCharacter == character;
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private void StartSelection(PlayerSlot slot, InputType type, Gamepad gamepad, bool isSecondKeyboard)
    {
        slot.isSelecting = true;
        slot.currentSelectionType = type;
        slot.currentSelectionGamepad = gamepad;
        slot.isUsingSecondKeyboard = isSecondKeyboard;

        if (type == InputType.Gamepad && gamepad != null)
            usedGamepads.Add(gamepad);

        CharacterVisual visual = GetVisual(slot.selectedCharacter);
        if (visual == null) return;

        MoveVisualToPlayer(slot.selectedCharacter, type, isSecondKeyboard);

        if (visual.slotObject != null)
            visual.slotObject.transform.DOScale(visual.activeScale, visual.animationDuration).SetEase(visual.easeType);

        if (visual.characterSprite != null)
        {
            visual.characterSprite.color = visual.activeColor;
            if (type == InputType.Keyboard && visual.selectingKeyboardSprite != null)
                visual.characterSprite.sprite = visual.selectingKeyboardSprite;
            else if (type == InputType.Gamepad && visual.selectingGamepadSprite != null)
                visual.characterSprite.sprite = visual.selectingGamepadSprite;
        }

        if (visual.statusText != null)
            visual.statusText.text = GetStatusText(slot, type, isConfirmed: false);
    }

    private void ConfirmSelection(PlayerSlot slot)
    {
        slot.assignedInput = slot.currentSelectionType;
        slot.assignedGamepad = slot.currentSelectionGamepad;
        slot.isSelecting = false;
        slot.isLocked = false;

        CharacterVisual visual = GetVisual(slot.selectedCharacter);
        if (visual != null)
        {
            if (visual.characterSprite != null)
            {
                visual.characterSprite.DOColor(Color.white, 0.1f)
                    .OnComplete(() => visual.characterSprite.DOColor(visual.activeColor, 0.1f))
                    .SetLoops(2, LoopType.Yoyo);

                if (slot.assignedInput == InputType.Keyboard && visual.confirmedKeyboardSprite != null)
                    visual.characterSprite.sprite = visual.confirmedKeyboardSprite;
                else if (slot.assignedInput == InputType.Gamepad && visual.confirmedGamepadSprite != null)
                    visual.characterSprite.sprite = visual.confirmedGamepadSprite;
            }

            if (visual.statusText != null)
                visual.statusText.text = GetStatusText(slot, slot.assignedInput, isConfirmed: true);
        }

        SaveSlotData(slot);
    }

    /// <summary>
    /// Guarda el slot en assignedPlayers.
    /// internalPlayerIndex: Gloppk = 1, Chopi = 2 (coincide con los tags Player1/Player2)
    /// </summary>
    private void SaveSlotData(PlayerSlot slot)
    {
        while (assignedPlayers.Count < 2)
            assignedPlayers.Add(new PlayerSlotData());

        // El slot de "primer jugador en elegir" va al índice 0,
        // el segundo al índice 1 — esto no cambia.
        // Lo que cambia es internalPlayerIndex según el personaje elegido.
        int slotIndex = slot == player1Slot ? 0 : 1;

        assignedPlayers[slotIndex].inputType = slot.assignedInput;
        assignedPlayers[slotIndex].gamepad = slot.assignedGamepad;
        assignedPlayers[slotIndex].character = slot.selectedCharacter;
        assignedPlayers[slotIndex].internalPlayerIndex = slot.selectedCharacter == Character.Gloppk ? 1 : 2;
    }

    private void CancelSelection(PlayerSlot slot)
    {
        if (slot.currentSelectionType == InputType.Gamepad && slot.currentSelectionGamepad != null)
            usedGamepads.Remove(slot.currentSelectionGamepad);

        MoveVisualToCenter(slot.selectedCharacter, slot.isUsingSecondKeyboard);

        CharacterVisual visual = GetVisual(slot.selectedCharacter);
        slot.isSelecting = false;
        slot.currentSelectionType = InputType.None;
        slot.currentSelectionGamepad = null;
        slot.isUsingSecondKeyboard = false;
        Character previouslySelected = slot.selectedCharacter;
        slot.selectedCharacter = Character.None;

        if (visual != null)
        {
            if (visual.slotObject != null)
                visual.slotObject.transform.DOScale(visual.inactiveScale, visual.animationDuration).SetEase(visual.easeType);

            if (visual.characterSprite != null)
            {
                if (visual.idleSprite != null) visual.characterSprite.sprite = visual.idleSprite;
                visual.characterSprite.color = visual.inactiveColor;
            }

            if (visual.statusText != null) visual.statusText.text = "";
        }
    }

    private void CancelAssignment(PlayerSlot slot)
    {
        if (slot.assignedInput == InputType.Gamepad && slot.assignedGamepad != null)
            usedGamepads.Remove(slot.assignedGamepad);

        CharacterVisual visual = GetVisual(slot.selectedCharacter);
        MoveVisualToCenter(slot.selectedCharacter, slot.isUsingSecondKeyboard);
        
        Character previouslySelected = slot.selectedCharacter;
        ResetSlot(slot);
        if (previouslySelected != Character.None)
        {
            ResetCharacterVisual(GetVisual(previouslySelected));
        }

        while (assignedPlayers.Count < 2)
            assignedPlayers.Add(new PlayerSlotData());

        int index = slot == player1Slot ? 0 : 1;
        assignedPlayers[index] = new PlayerSlotData();
    }

    private void ResetSlot(PlayerSlot slot)
    {
        slot.assignedInput = InputType.None;
        slot.assignedGamepad = null;
        slot.isSelecting = false;
        slot.currentSelectionType = InputType.None;
        slot.currentSelectionGamepad = null;
        slot.isLocked = false;
        slot.isUsingSecondKeyboard = false;
        slot.selectedCharacter = Character.None;
    }

    private void MoveVisualToPlayer(Character character, InputType type, bool isSecondKeyboard)
    {
        GameObject visual = type == InputType.Keyboard
            ? (isSecondKeyboard ? keyboardVisual2 : keyboardVisual)
            : gamepadVisual;

        Transform target = character == Character.Gloppk ? player1SpawnPoint : player2SpawnPoint;

        if (visual != null && target != null)
        {
            visual.SetActive(true);
            visual.transform.position = target.position;
        }
    }

    private void MoveVisualToCenter(Character character, bool isSecondKeyboard)
    {
        PlayerSlot slot = player1Slot.selectedCharacter == character ? player1Slot : 
                           player2Slot.selectedCharacter == character ? player2Slot : null;
        if (slot == null) return;
        
        InputType type = slot.isSelecting ? slot.currentSelectionType : slot.assignedInput;
        GameObject visual = type == InputType.Keyboard
            ? (isSecondKeyboard ? keyboardVisual2 : keyboardVisual)
            : gamepadVisual;

        if (visual != null && centerSpawnPoint != null)
            visual.transform.position = centerSpawnPoint.position;
    }

    private string GetStatusText(PlayerSlot slot, InputType type, bool isConfirmed)
    {
        bool isFirst = slot == player1Slot;
        if (type == InputType.Keyboard)
        {
            if (isFirst)
                return isConfirmed ? "¡Listo!\nESC = Cancelar" : "ESPACIO = Listo\nESC = Cancelar";
            return isConfirmed ? "¡Listo!\nBACKSPACE = Cancelar" : "ENTER = Listo\nBACKSPACE = Cancelar";
        }
        return isConfirmed ? "¡Listo!\nB = Cancelar" : "A = Listo\nB = Cancelar";
    }

    private void UpdateUI()
    {
        bool bothAssigned = player1Slot.assignedInput != InputType.None &&
                            player2Slot.assignedInput != InputType.None;

        if (bothAssigned && !hasAutoOpenedControlsPanel)
        {
            isControlsPanelManuallyClosed = false;
            hasAutoOpenedControlsPanel = true;
        }

        if (startButton != null)
        {
            if (bothAssigned != bothAssignedPrevious)
            {
                startButton.transform.DOKill();
                startButton.transform.localScale = Vector3.one;
            }
            startButton.SetActive(bothAssigned);
            if (bothAssigned && !bothAssignedPrevious)
            {
                startButton.transform.DOScale(Vector3.one * 1.1f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => startButton.transform.DOScale(Vector3.one, 0.2f));
            }
        }

        if (controlsPanel != null)
            controlsPanel.SetActive(!isControlsPanelManuallyClosed &&
                                    (bothAssigned || forceControlsPanelOpen));

        if (keyboardVisual != null) keyboardVisual.SetActive(!bothAssigned);
        if (keyboardVisual2 != null) keyboardVisual2.SetActive(!bothAssigned);
        if (gamepadVisual != null) gamepadVisual.SetActive(!bothAssigned);

        bothAssignedPrevious = bothAssigned;
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.transform.DOKill();
        if (controlsPanel != null) controlsPanel.transform.DOKill();
        if (gloppkVisual != null)
        {
            if (gloppkVisual.slotObject != null) gloppkVisual.slotObject.transform.DOKill();
            if (gloppkVisual.characterSprite != null) gloppkVisual.characterSprite.DOKill();
        }
        if (chopiVisual != null)
        {
            if (chopiVisual.slotObject != null) chopiVisual.slotObject.transform.DOKill();
            if (chopiVisual.characterSprite != null) chopiVisual.characterSprite.DOKill();
        }
        if (promptText != null) promptText.DOKill();
        if (keyboardVisual != null) keyboardVisual.transform.DOKill();
        if (keyboardVisual2 != null) keyboardVisual2.transform.DOKill();
        if (gamepadVisual != null) gamepadVisual.transform.DOKill();
    }

    public void LoadNextScene()
    {
        if (player1Slot.assignedInput != InputType.None &&
            player2Slot.assignedInput != InputType.None)
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadRuleta();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Select_Minigame");
        }
    }

    public void CloseControlsPanel()
    {
        isControlsPanelManuallyClosed = true;
        forceControlsPanelOpen = false;
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void OpenControlsPanel()
    {
        isControlsPanelManuallyClosed = false;
        forceControlsPanelOpen = true;
        UpdateUI();
    }

    public void ToggleControlsPanel()
    {
        if (controlsPanel != null && controlsPanel.activeSelf)
            CloseControlsPanel();
        else
            OpenControlsPanel();
    }

    public void BackToMenu()
    {
        GameManager.Instance?.ResetGame();
        AudioManager.Instance?.StopMusic();
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}