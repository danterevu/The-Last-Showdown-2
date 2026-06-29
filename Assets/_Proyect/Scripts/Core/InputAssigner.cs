using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

public class InputAssigner : MonoBehaviour
{
    public enum InputType { None, Gamepad, Keyboard }
    public enum SelectionPhase { Player1Selecting, Player2Selecting, SelectionComplete }
    public enum Character { None, Gloppk, Chopi }
    public enum VisualType { Platform, TopDown, Ship }

    [System.Serializable]
    public class GameplayVisualSet
    {
        public VisualType type;
        public RuntimeAnimatorController animatorController;
        public Sprite idleSprite;
        [Header("HUD")]
        public RuntimeAnimatorController hudAnimatorController;
        public Sprite hudIdleSprite;
    }

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

        [Header("Gameplay")]
        public List<GameplayVisualSet> gameplayVisuals = new List<GameplayVisualSet>();

        public GameplayVisualSet GetGameplayVisual(VisualType type)
        {
            foreach (var v in gameplayVisuals)
                if (v.type == type) return v;
            return null;

        }
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
        [HideInInspector] public Character selectedCharacter = Character.None;
    }

    [System.Serializable]
    public class PlayerSlotData
    {
        public InputType inputType;
        public Gamepad gamepad;
        public Character character;
        public int internalPlayerIndex;
        public List<GameplayVisualSet> gameplayVisuals;
    }

    public static List<PlayerSlotData> assignedPlayers = new List<PlayerSlotData>();

    public static PlayerSlotData GetPlayerData(int playerIndex)
    {
        if (assignedPlayers.Count > playerIndex) return assignedPlayers[playerIndex];
        return null;
    }

    public static Gamepad GetGamepadForPlayer(int playerIndex)
    {
        var data = GetPlayerData(playerIndex);
        return data?.gamepad;
    }

    public static PlayerSlotData GetInternalPlayer(int internalIndex)
    {
        foreach (var p in assignedPlayers)
            if (p.internalPlayerIndex == internalIndex) return p;
        return null;
    }

    public static RuntimeAnimatorController GetAnimatorController(int internalIndex, VisualType type)
    {
        var data = GetInternalPlayer(internalIndex);
        if (data?.gameplayVisuals == null) return null;
        foreach (var v in data.gameplayVisuals)
            if (v.type == type) return v.animatorController;
        return null;
    }

    public static Sprite GetIdleSprite(int internalIndex, VisualType type)
    {
        var data = GetInternalPlayer(internalIndex);
        if (data?.gameplayVisuals == null) return null;
        foreach (var v in data.gameplayVisuals)
            if (v.type == type) return v.idleSprite;
        return null;
    }
    public static RuntimeAnimatorController GetHUDAnimatorController(int internalIndex, VisualType type)
    {
        var data = GetInternalPlayer(internalIndex);
        if (data?.gameplayVisuals == null) return null;
        foreach (var v in data.gameplayVisuals)
            if (v.type == type) return v.hudAnimatorController;
        return null;
    }

    public static Sprite GetHUDIdleSprite(int internalIndex, VisualType type)
    {
        var data = GetInternalPlayer(internalIndex);
        if (data?.gameplayVisuals == null) return null;
        foreach (var v in data.gameplayVisuals)
            if (v.type == type) return v.hudIdleSprite;
        return null;
    }

    public static void Clear() => assignedPlayers.Clear();

    [Header("Player Slots")]
    [SerializeField] private PlayerSlot player1Slot;
    [SerializeField] private PlayerSlot player2Slot;

    [Header("Character Visuals")]
    [SerializeField] private CharacterVisual gloppkVisual;
    [SerializeField] private CharacterVisual chopiVisual;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject startButton;
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
    private bool bothAssignedPrevious = false;

    private bool ignoreNextEnter = false;

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
        bothAssignedPrevious = false;
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

        // Start de cualquier gamepad avanza si ambos están listos
        if (currentPhase == SelectionPhase.SelectionComplete)
        {
            if (ignoreNextEnter)
            {
                ignoreNextEnter = false;
            }
            else
            {
                // P2 cancela
                bool cancelP2 = false;
                if (player2Slot.assignedInput == InputType.Keyboard && Keyboard.current != null)
                    cancelP2 = Keyboard.current.backspaceKey.wasPressedThisFrame;
                else if (player2Slot.assignedInput == InputType.Gamepad && player2Slot.assignedGamepad != null)
                    cancelP2 = player2Slot.assignedGamepad.buttonEast.wasPressedThisFrame ||
                               player2Slot.assignedGamepad.selectButton.wasPressedThisFrame;

                // P1 cancela
                bool cancelP1 = false;
                if (player1Slot.assignedInput == InputType.Keyboard && Keyboard.current != null)
                    cancelP1 = Keyboard.current.escapeKey.wasPressedThisFrame;
                else if (player1Slot.assignedInput == InputType.Gamepad && player1Slot.assignedGamepad != null)
                    cancelP1 = player1Slot.assignedGamepad.buttonEast.wasPressedThisFrame ||
                               player1Slot.assignedGamepad.selectButton.wasPressedThisFrame;

                if (cancelP2)
                {
                    CancelAssignment(player2Slot);
                    currentPhase = SelectionPhase.Player2Selecting;
                    return;
                }
                else if (cancelP1)
                {
                    CancelAssignment(player2Slot);
                    CancelAssignment(player1Slot);
                    currentPhase = SelectionPhase.Player1Selecting;
                    return;
                }

                foreach (var gp in Gamepad.all)
                {
                    if (gp != null && gp.startButton.wasPressedThisFrame)
                    {
                        LoadNextScene();
                        return;
                    }
                }

                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    LoadNextScene();
            }
        }

        UpdateTurnUI();
        UpdateUI();
    }

    private void HandlePlayerTurn(PlayerSlot slot, bool isFirstPlayer)
    {
        var keyboard = Keyboard.current;

        if (slot.assignedInput == InputType.None)
        {
            if (!slot.isSelecting)
            {
                // B / Escape sin nada = volver al menú
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (isFirstPlayer)
                        BackToMenu();
                    else
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

                // Teclado: detectar izquierda o derecha
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

                // Gamepad libre: detectar izquierda o derecha
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
                    if (!isFirstPlayer) ignoreNextEnter = true; 
                }
                else if (cancel)
                {
                    CancelSelection(slot);
                }
            }
        }
        else
        {
            // Ya confirmó → solo puede cancelar para volver atrás
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
        if (isFirstPlayer) return false;
        return player1Slot.selectedCharacter == character;
    }

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

    private void SaveSlotData(PlayerSlot slot)
    {
        while (assignedPlayers.Count < 2)
            assignedPlayers.Add(new PlayerSlotData());

        int slotIndex = slot == player1Slot ? 0 : 1;
        CharacterVisual visual = GetVisual(slot.selectedCharacter);

        assignedPlayers[slotIndex].inputType = slot.assignedInput;
        assignedPlayers[slotIndex].gamepad = slot.assignedGamepad;
        assignedPlayers[slotIndex].character = slot.selectedCharacter;
        assignedPlayers[slotIndex].internalPlayerIndex = slotIndex + 1;
        assignedPlayers[slotIndex].gameplayVisuals = visual?.gameplayVisuals;
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

        MoveVisualToCenter(slot.selectedCharacter, slot.isUsingSecondKeyboard);

        Character previouslySelected = slot.selectedCharacter;
        ResetSlot(slot);
        if (previouslySelected != Character.None)
            ResetCharacterVisual(GetVisual(previouslySelected));

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

        if (keyboardVisual != null) keyboardVisual.SetActive(!bothAssigned);
        if (keyboardVisual2 != null) keyboardVisual2.SetActive(!bothAssigned);
        if (gamepadVisual != null) gamepadVisual.SetActive(!bothAssigned);

        bothAssignedPrevious = bothAssigned;
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.transform.DOKill();
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
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadRuleta();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Select_Minigame");
        }
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