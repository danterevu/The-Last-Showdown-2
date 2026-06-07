using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class InputAssigner : MonoBehaviour
{
    public enum InputType { None, Gamepad, Keyboard }
    public enum SelectionPhase { WaitingForAnyInput, WaitingForPlayerInput, SelectionComplete }

    [System.Serializable]
    public class PlayerSlot
    {
        public int playerIndex;
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
        [HideInInspector] public InputType assignedInput = InputType.None;
        [HideInInspector] public Gamepad assignedGamepad;
        [HideInInspector] public bool isSelecting = false;
        [HideInInspector] public InputType currentSelectionType = InputType.None;
        [HideInInspector] public Gamepad currentSelectionGamepad = null;
        [HideInInspector] public bool isLocked = false;
        [HideInInspector] public bool isUsingSecondKeyboard = false;
    }

    [System.Serializable]
    public class PlayerSlotData
    {
        public InputType inputType;
        public Gamepad gamepad;
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

    public static void Clear() => assignedPlayers.Clear();

    [Header("Player Slots")]
    [SerializeField] private PlayerSlot player1Slot;
    [SerializeField] private PlayerSlot player2Slot;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private string initialPrompt = "Aprieta cualquier tecla en el teclado o en el Mando";

    [Header("Visual Objects")]
    [SerializeField] private GameObject keyboardVisual;
    [SerializeField] private GameObject keyboardVisual2;
    [SerializeField] private GameObject gamepadVisual;

    [Header("Spawn Points")]
    [SerializeField] private Transform centerSpawnPoint;
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    private SelectionPhase currentPhase = SelectionPhase.WaitingForAnyInput;
    private bool keyboardAlreadyUsed = false;
    private List<Gamepad> usedGamepads = new List<Gamepad>();
    private bool isControlsPanelManuallyClosed = false;

    private void OnEnable()
    {
        assignedPlayers.Clear();
        usedGamepads.Clear();
        keyboardAlreadyUsed = false;
        currentPhase = SelectionPhase.WaitingForAnyInput;
        ResetSlot(player1Slot);
        ResetSlot(player2Slot);
        isControlsPanelManuallyClosed = true;
        if (controlsPanel != null) controlsPanel.SetActive(false);
        UpdateUI();
        InitializeVisuals();
        
        // Inicializar assignedPlayers con 2 elementos
        while (assignedPlayers.Count < 2)
        {
            assignedPlayers.Add(new PlayerSlotData());
        }
    }
    // AHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
    private void Start()
    {
        assignedPlayers.Clear();
        usedGamepads.Clear();
        keyboardAlreadyUsed = false;
        currentPhase = SelectionPhase.WaitingForAnyInput;
        ResetSlot(player1Slot);
        ResetSlot(player2Slot);
        isControlsPanelManuallyClosed = true;
        if (controlsPanel != null) controlsPanel.SetActive(false);
        UpdateUI();
        InitializeVisuals();
        
        // Inicializar assignedPlayers con 2 elementos
        while (assignedPlayers.Count < 2)
        {
            assignedPlayers.Add(new PlayerSlotData());
        }
    }

    private void InitializeVisuals()
    {
        // Colocar todos los visuales en el centro al principio
        if (keyboardVisual != null && centerSpawnPoint != null)
        {
            keyboardVisual.SetActive(true);
            keyboardVisual.transform.position = centerSpawnPoint.position;
        }

        if (keyboardVisual2 != null && centerSpawnPoint != null)
        {
            keyboardVisual2.SetActive(true);
            keyboardVisual2.transform.position = centerSpawnPoint.position;
        }

        if (gamepadVisual != null && centerSpawnPoint != null)
        {
            gamepadVisual.SetActive(true);
            gamepadVisual.transform.position = centerSpawnPoint.position;
        }
    }

    private void Update()
    {
        if (currentPhase == SelectionPhase.WaitingForAnyInput)
        {
            CheckForAnyInput();
        }
        else if (currentPhase == SelectionPhase.WaitingForPlayerInput)
        {
            HandlePlayer1Input();
            HandlePlayer2Input();
            UpdateUI();
        }
    }

    private void CheckForAnyInput()
    {
        var keyboard = Keyboard.current;
        var gamepads = Gamepad.all;

        // Check for any keyboard input
        if (keyboard != null)
        {
            if (keyboard.anyKey.wasPressedThisFrame)
            {
                StartPlayerSelection();
                return;
            }
        }

        // Check for any gamepad input
        foreach (var gamepad in gamepads)
        {
            if (gamepad != null)
            {
                // Check all button controls on the gamepad
                bool anyPressed = false;
                anyPressed |= gamepad.buttonSouth.wasPressedThisFrame;
                anyPressed |= gamepad.buttonEast.wasPressedThisFrame;
                anyPressed |= gamepad.buttonWest.wasPressedThisFrame;
                anyPressed |= gamepad.buttonNorth.wasPressedThisFrame;
                anyPressed |= gamepad.dpad.up.wasPressedThisFrame;
                anyPressed |= gamepad.dpad.down.wasPressedThisFrame;
                anyPressed |= gamepad.dpad.left.wasPressedThisFrame;
                anyPressed |= gamepad.dpad.right.wasPressedThisFrame;
                anyPressed |= gamepad.leftStick.up.wasPressedThisFrame;
                anyPressed |= gamepad.leftStick.down.wasPressedThisFrame;
                anyPressed |= gamepad.leftStick.left.wasPressedThisFrame;
                anyPressed |= gamepad.leftStick.right.wasPressedThisFrame;
                anyPressed |= gamepad.startButton.wasPressedThisFrame;
                anyPressed |= gamepad.selectButton.wasPressedThisFrame;
                
                if (anyPressed)
                {
                    StartPlayerSelection();
                    return;
                }
            }
        }
    }

    private void StartPlayerSelection()
    {
        currentPhase = SelectionPhase.WaitingForPlayerInput;
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // Show both players
        if (player1Slot.characterSprite != null)
            player1Slot.characterSprite.gameObject.SetActive(true);
        if (player2Slot.characterSprite != null)
            player2Slot.characterSprite.gameObject.SetActive(true);
    }

    private void HandlePlayer1Input()
    {
        if (player1Slot.isLocked) return;

        var keyboard = Keyboard.current;
        var gamepads = Gamepad.all;

        if (player1Slot.assignedInput == InputType.None)
        {
            if (!player1Slot.isSelecting)
            {
                // Keyboard input for P1: A or Left Arrow
                if (keyboard != null && !keyboardAlreadyUsed)
                {
                    if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                    {
                        StartSelection(player1Slot, InputType.Keyboard, null, false);
                        return;
                    }
                }

                // Gamepad input for P1
                foreach (var gamepad in gamepads)
                {
                    if (gamepad != null && !usedGamepads.Contains(gamepad))
                    {
                        if (gamepad.leftStick.left.wasPressedThisFrame || gamepad.dpad.left.wasPressedThisFrame)
                        {
                            StartSelection(player1Slot, InputType.Gamepad, gamepad, false);
                            return;
                        }
                    }
                }
            }
            else
            {
                // Confirm or cancel for P1
                bool confirmPressed = false;
                bool cancelPressed = false;

                if (player1Slot.currentSelectionType == InputType.Keyboard && keyboard != null)
                {
                    confirmPressed = keyboard.spaceKey.wasPressedThisFrame;
                    cancelPressed = keyboard.escapeKey.wasPressedThisFrame;
                }
                else if (player1Slot.currentSelectionType == InputType.Gamepad && player1Slot.currentSelectionGamepad != null)
                {
                    confirmPressed = player1Slot.currentSelectionGamepad.buttonSouth.wasPressedThisFrame || player1Slot.currentSelectionGamepad.startButton.wasPressedThisFrame;
                    cancelPressed = player1Slot.currentSelectionGamepad.buttonEast.wasPressedThisFrame || player1Slot.currentSelectionGamepad.selectButton.wasPressedThisFrame;
                }

                if (confirmPressed)
                {
                    ConfirmSelection(player1Slot);
                }
                else if (cancelPressed)
                {
                    CancelSelection(player1Slot);
                }
            }
        }
        else
        {
            // Cancel assignment for P1
            bool cancelPressed = false;
            if (player1Slot.assignedInput == InputType.Keyboard && keyboard != null)
            {
                cancelPressed = keyboard.escapeKey.wasPressedThisFrame;
            }
            else if (player1Slot.assignedInput == InputType.Gamepad && player1Slot.assignedGamepad != null)
            {
                cancelPressed = player1Slot.assignedGamepad.buttonEast.wasPressedThisFrame || player1Slot.assignedGamepad.selectButton.wasPressedThisFrame;
            }

            if (cancelPressed)
            {
                CancelAssignment(player1Slot);
            }
        }
    }

    private void HandlePlayer2Input()
    {
        if (player2Slot.isLocked) return;

        var keyboard = Keyboard.current;
        var gamepads = Gamepad.all;

        if (player2Slot.assignedInput == InputType.None)
        {
            if (!player2Slot.isSelecting)
            {
                // Keyboard input for P2: D or Right Arrow
                if (keyboard != null && !keyboardAlreadyUsed)
                {
                    if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                    {
                        StartSelection(player2Slot, InputType.Keyboard, null, false);
                        return;
                    }
                }

                // Gamepad input for P2
                foreach (var gamepad in gamepads)
                {
                    if (gamepad != null && !usedGamepads.Contains(gamepad))
                    {
                        if (gamepad.leftStick.right.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame)
                        {
                            StartSelection(player2Slot, InputType.Gamepad, gamepad, false);
                            return;
                        }
                    }
                }
            }
            else
            {
                // Confirm or cancel for P2
                bool confirmPressed = false;
                bool cancelPressed = false;

                if (player2Slot.currentSelectionType == InputType.Keyboard && keyboard != null)
                {
                    confirmPressed = keyboard.enterKey.wasPressedThisFrame;
                    cancelPressed = keyboard.backspaceKey.wasPressedThisFrame;
                }
                else if (player2Slot.currentSelectionType == InputType.Gamepad && player2Slot.currentSelectionGamepad != null)
                {
                    confirmPressed = player2Slot.currentSelectionGamepad.buttonSouth.wasPressedThisFrame || player2Slot.currentSelectionGamepad.startButton.wasPressedThisFrame;
                    cancelPressed = player2Slot.currentSelectionGamepad.buttonEast.wasPressedThisFrame || player2Slot.currentSelectionGamepad.selectButton.wasPressedThisFrame;
                }

                if (confirmPressed)
                {
                    ConfirmSelection(player2Slot);
                }
                else if (cancelPressed)
                {
                    CancelSelection(player2Slot);
                }
            }
        }
        else
        {
            // Cancel assignment for P2
            bool cancelPressed = false;
            if (player2Slot.assignedInput == InputType.Keyboard && keyboard != null)
            {
                cancelPressed = keyboard.backspaceKey.wasPressedThisFrame;
            }
            else if (player2Slot.assignedInput == InputType.Gamepad && player2Slot.assignedGamepad != null)
            {
                cancelPressed = player2Slot.assignedGamepad.buttonEast.wasPressedThisFrame || player2Slot.assignedGamepad.selectButton.wasPressedThisFrame;
            }

            if (cancelPressed)
            {
                CancelAssignment(player2Slot);
            }
        }
    }

    private void StartSelection(PlayerSlot slot, InputType type, Gamepad gamepad, bool isSecondKeyboard = false)
    {
        slot.isSelecting = true;
        slot.currentSelectionType = type;
        slot.currentSelectionGamepad = gamepad;
        slot.isUsingSecondKeyboard = isSecondKeyboard;

        if (type == InputType.Keyboard)
        {
            keyboardAlreadyUsed = true;
        }
        else if (type == InputType.Gamepad && gamepad != null)
        {
            usedGamepads.Add(gamepad);
        }

        // Mover visual al jugador
        MoveVisualToPlayer(slot, type, isSecondKeyboard);

        if (slot.slotObject != null)
        {
            slot.slotObject.transform.DOScale(slot.activeScale, slot.animationDuration).SetEase(slot.easeType);
        }

        if (slot.characterSprite != null)
        {
            slot.characterSprite.color = slot.activeColor;
            if (type == InputType.Keyboard && slot.selectingKeyboardSprite != null)
            {
                slot.characterSprite.sprite = slot.selectingKeyboardSprite;
            }
            else if (type == InputType.Gamepad && slot.selectingGamepadSprite != null)
            {
                slot.characterSprite.sprite = slot.selectingGamepadSprite;
            }
        }

        if (slot.statusText != null)
            slot.statusText.text = "¿Listo?";
    }

    private void MoveVisualToPlayer(PlayerSlot slot, InputType type, bool isSecondKeyboard = false)
    {
        GameObject visual;
        if (type == InputType.Keyboard)
        {
            visual = isSecondKeyboard ? keyboardVisual2 : keyboardVisual;
        }
        else
        {
            visual = gamepadVisual;
        }
        
        Transform targetPoint = slot == player1Slot ? player1SpawnPoint : player2SpawnPoint;

        if (visual != null && targetPoint != null)
        {
            visual.SetActive(true);
            visual.transform.position = targetPoint.position;
        }
    }

    private void MoveVisualToCenter(PlayerSlot slot, bool isSecondKeyboard = false)
    {
        InputType type = slot.isSelecting ? slot.currentSelectionType : slot.assignedInput;
        GameObject visual;
        
        if (type == InputType.Keyboard)
        {
            visual = isSecondKeyboard ? keyboardVisual2 : keyboardVisual;
        }
        else
        {
            visual = gamepadVisual;
        }

        if (visual != null && centerSpawnPoint != null)
        {
            visual.transform.position = centerSpawnPoint.position;
        }
    }

    private void ConfirmSelection(PlayerSlot slot)
    {
        slot.assignedInput = slot.currentSelectionType;
        slot.assignedGamepad = slot.currentSelectionGamepad;
        slot.isSelecting = false;
        slot.isLocked = true;

        if (slot.characterSprite != null)
        {
            slot.characterSprite.DOColor(Color.white, 0.1f)
                .OnComplete(() => slot.characterSprite.DOColor(slot.activeColor, 0.1f))
                .SetLoops(2, LoopType.Yoyo);

            if (slot.assignedInput == InputType.Keyboard && slot.confirmedKeyboardSprite != null)
            {
                slot.characterSprite.sprite = slot.confirmedKeyboardSprite;
            }
            else if (slot.assignedInput == InputType.Gamepad && slot.confirmedGamepadSprite != null)
            {
                slot.characterSprite.sprite = slot.confirmedGamepadSprite;
            }
        }

        if (slot.statusText != null)
            slot.statusText.text = "¡Listo!";

        // Asegurarnos de que assignedPlayers tiene al menos 2 elementos
        while (assignedPlayers.Count < 2)
        {
            assignedPlayers.Add(new PlayerSlotData());
        }

        // Asignar a índice fijo: player1Slot = 0, player2Slot = 1
        int index = slot == player1Slot ? 0 : 1;
        assignedPlayers[index].inputType = slot.assignedInput;
        assignedPlayers[index].gamepad = slot.assignedGamepad;
    }

    private void CancelSelection(PlayerSlot slot)
    {
        if (slot.currentSelectionType == InputType.Keyboard)
        {
            keyboardAlreadyUsed = false;
        }
        else if (slot.currentSelectionType == InputType.Gamepad && slot.currentSelectionGamepad != null)
        {
            usedGamepads.Remove(slot.currentSelectionGamepad);
        }

        // Mover visual de vuelta al centro
        MoveVisualToCenter(slot, slot.isUsingSecondKeyboard);

        slot.isSelecting = false;
        slot.currentSelectionType = InputType.None;
        slot.currentSelectionGamepad = null;
        slot.isUsingSecondKeyboard = false;

        if (slot.slotObject != null)
        {
            slot.slotObject.transform.DOScale(slot.inactiveScale, slot.animationDuration).SetEase(slot.easeType);
        }

        if (slot.characterSprite != null && slot.idleSprite != null)
        {
            slot.characterSprite.sprite = slot.idleSprite;
            slot.characterSprite.color = slot.inactiveColor;
        }

        if (slot.statusText != null)
            slot.statusText.text = "";
    }

    private void CancelAssignment(PlayerSlot slot)
    {
        if (slot.assignedInput == InputType.Keyboard)
        {
            keyboardAlreadyUsed = false;
        }
        else if (slot.assignedInput == InputType.Gamepad && slot.assignedGamepad != null)
        {
            usedGamepads.Remove(slot.assignedGamepad);
        }

        // Mover visual de vuelta al centro
        MoveVisualToCenter(slot, slot.isUsingSecondKeyboard);

        ResetSlot(slot);

        // Asegurarnos de que assignedPlayers tiene al menos 2 elementos
        while (assignedPlayers.Count < 2)
        {
            assignedPlayers.Add(new PlayerSlotData());
        }

        // Limpiar el índice correspondiente
        int index = slot == player1Slot ? 0 : 1;
        assignedPlayers[index].inputType = InputType.None;
        assignedPlayers[index].gamepad = null;
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

        if (slot.slotObject != null)
        {
            slot.slotObject.transform.DOScale(slot.inactiveScale, slot.animationDuration).SetEase(slot.easeType);
        }

        if (slot.characterSprite != null && slot.idleSprite != null)
        {
            slot.characterSprite.gameObject.SetActive(true);
            slot.characterSprite.sprite = slot.idleSprite;
            slot.characterSprite.color = slot.inactiveColor;
        }

        if (slot.statusText != null)
            slot.statusText.text = "";
    }

    private void UpdateUI()
    {
        // Show/hide initial prompt
        if (promptText != null)
        {
            promptText.gameObject.SetActive(currentPhase == SelectionPhase.WaitingForAnyInput);
            if (currentPhase == SelectionPhase.WaitingForAnyInput)
                promptText.text = initialPrompt;
        }

        // Show/hide start button and controls panel
        bool bothAssigned = player1Slot.assignedInput != InputType.None && 
                           player2Slot.assignedInput != InputType.None;

        if (startButton != null)
        {
            startButton.SetActive(bothAssigned);
            if (bothAssigned)
            {
                startButton.transform.DOScale(Vector3.one * 1.1f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => startButton.transform.DOScale(Vector3.one, 0.2f));
            }
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(bothAssigned && !isControlsPanelManuallyClosed);
        }
    }

    public void LoadNextScene()
    {
        if (player1Slot.assignedInput != InputType.None && 
            player2Slot.assignedInput != InputType.None)
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
            SceneLoader.Instance.LoadRuleta();
        }
    }

    public void CloseControlsPanel()
    {
        if (controlsPanel != null)
        {
            isControlsPanelManuallyClosed = true;
            controlsPanel.SetActive(false);
        }
    }

    public void OpenControlsPanel()
    {
        if (controlsPanel != null)
        {
            isControlsPanelManuallyClosed = false;
            UpdateUI();
        }
    }

    public void UseTwoKeyboards()
    {
        if (currentPhase == SelectionPhase.WaitingForAnyInput)
        {
            StartPlayerSelection();
        }

        // Asegurarnos de que assignedPlayers tiene al menos 2 elementos
        while (assignedPlayers.Count < 2)
        {
            assignedPlayers.Add(new PlayerSlotData());
        }

        // Asignar teclado a Player 1 (A/D)
        if (player1Slot.assignedInput == InputType.None && !player1Slot.isSelecting)
        {
            StartSelection(player1Slot, InputType.Keyboard, null, false);
            ConfirmSelection(player1Slot);
        }

        // Asignar teclado a Player 2 (Flechas)
        if (player2Slot.assignedInput == InputType.None && !player2Slot.isSelecting)
        {
            StartSelection(player2Slot, InputType.Keyboard, null, true);
            ConfirmSelection(player2Slot);
        }
    }

    public void BackToMenu()
    {
        GameManager.Instance?.ResetGame();
        AudioManager.Instance?.StopMusic();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
    }
}
