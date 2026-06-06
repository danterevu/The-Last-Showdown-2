using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// Tipos de datos de diálogo
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)] public string text;
    public Sprite presenterSprite;
    public float displayDuration = 2.5f;

    [Tooltip("Si es true, al mostrar esta línea se inicializa la ruleta de minijuegos.")]
    public bool isFinalDialogue = false;
}

[System.Serializable]
public class DialogueSequence
{
    public string sequenceName;
    public Sprite defaultPresenterSprite;

    [Tooltip("Si true, al terminar la secuencia se mantiene el último sprite mostrado.")]
    public bool keepLastSprite = false;

    [Tooltip("Sprite que queda después de la secuencia cuando keepLastSprite es false. " +
             "Si está vacío se usa defaultPresenterSprite.")]
    public Sprite postSequenceSprite;

    public DialogueLine[] lines;
}

[System.Serializable]
public class MinigameReturnDialogues
{
    [Tooltip("Minijuego al que corresponden estas secuencias.")]
    public MinigameID minigameId;
    public string minigameName; // solo para el Inspector
    public DialogueSequence[] sequences;
}

[System.Serializable]
public class MinigameResultDialogue
{
    public MinigameID minigameId;
    public string minigameName;
    public DialogueSequence[] sequences;
}

// ─────────────────────────────────────────────────────────────────────────────
// Máquina de estados de la secuencia
// ─────────────────────────────────────────────────────────────────────────────

public enum RoulettePhase
{
    Intro,
    OpeningDialogue,
    MinigameRoulette,
    MinigameResultDialogue,
    ModifierRoulette,
    ModifierResultDialogue,
    Loading,
}

// ─────────────────────────────────────────────────────────────────────────────
// Script principal
// ─────────────────────────────────────────────────────────────────────────────

public class RouletteShowDialogueSystem : MonoBehaviour
{
    // ── Inspector: Cámara ─────────────────────────────────────────────────────

    [Header("Cámara")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float zoomOutOrthoSize = 8f;
    [SerializeField] private float zoomToRouletteOrthoSize = 5f;
    [SerializeField] private float zoomDuration = 1f;

    // ── Inspector: Visual general ─────────────────────────────────────────────

    [Header("Centro")]
    [SerializeField] private GameObject centerSprite;
    [SerializeField] private float centerSpritePopDuration = 0.5f;

    [Header("Luces")]
    [SerializeField] private GameObject[] lights;
    [SerializeField] private float lightsPendulumAmount = 3f;
    [SerializeField] private float lightsPendulumDuration = 1.5f;
    [SerializeField] private bool lightsAlternateDirection = true;

    // ── Inspector: Presentador ────────────────────────────────────────────────

    [Header("Presentador")]
    [SerializeField] private SpriteRenderer presenterSpriteRenderer;
    [SerializeField] private float presenterJumpHeight = 0.5f;
    [SerializeField] private float presenterJumpDuration = 0.25f;
    [SerializeField] private Transform presenterRouletteDestination;
    [SerializeField] private float presenterReturnDuration = 0.6f;
    [SerializeField] private Transform presenterCenterPosition;

    // ── Inspector: Texto de diálogo ───────────────────────────────────────────

    [Header("Texto de diálogo")]
    [Tooltip("Puntos de spawn del texto en la escena (uno se elige al azar).")]
    [SerializeField] private Transform[] textSpawnPoints;
    [Tooltip("Prefab con TextMeshPro 3D (NO UI).")]
    [SerializeField] private TextMeshPro textPrefab;
    [SerializeField] private float textFadeInDuration = 0.2f;
    [SerializeField] private float textFadeOutDuration = 0.3f;
    [SerializeField] private float typewriterSpeed = 0.05f;

    [Header("Auto-Tipo y Controles")]
    [Tooltip("Texto UI para mostrar las instrucciones (Espacio/ESC)")]
    [SerializeField] private TextMeshProUGUI hintText;
    [Tooltip("Texto de ayuda: \"(Espacio para ir al siguiente dialogo o ESC para saltear)\"")]
    [SerializeField] private string hintTextContent = "(Espacio para ir al siguiente dialogo o ESC para saltear)";
    [Tooltip("Texto UI para mostrar si el auto-tipo está activado (ON/OFF)")]
    [SerializeField] private TextMeshProUGUI autoTypeStatusText;

    // ── Inspector: Secuencias de diálogo ──────────────────────────────────────

    [Header("Diálogos de apertura (primera ronda / sin minijuego previo)")]
    [SerializeField] private DialogueSequence[] openingSequences;

    [Header("Diálogos de retorno por minijuego")]
    [SerializeField] private MinigameReturnDialogues[] returnSequencesByMinigame;

    [Header("Diálogos de resultado de minijuego")]
    [SerializeField] private MinigameResultDialogue[] minigameResultDialogues;

    // Los diálogos de resultado de modificador viven en MinigameConfig.modifiers[i].resultDialogues
    // No hay array genérico aquí — cada modificador tiene los suyos propios.

    // ── Inspector: Ruleta de minijuego ────────────────────────────────────────

    [Header("Ruleta de minijuego")]
    [SerializeField] private GameObject minigameRouletteRoot;
    [SerializeField] private MinigameSpinner minigameSpinner;
    [SerializeField] private float rouletteDropDelay = 0f;
    [SerializeField] private float rouletteDropDuration = 1.2f;
    [SerializeField] private float rouletteBounceStrength = 0.5f;
    [SerializeField] private int rouletteBounceVibrato = 5;
    [SerializeField] private float rouletteBounceDuration = 0.8f;
    [SerializeField] private GameObject rouletteCover;
    [SerializeField] private bool removeCoverAutomatically = true;
    [SerializeField] private float coverMoveAmount = 10f;
    [SerializeField] private float coverMoveDuration = 0.5f;

    // ── Inspector: Ruleta de modificadores ───────────────────────────────────

    [Header("Ruleta de modificadores")]
    [SerializeField] private GameObject modifierRouletteRoot;
    [SerializeField] private ModifiersSpinner modifiersSpinner;

    // ── Inspector: Resultado ──────────────────────────────────────────────────

    [Header("Resultado visual")]
    [SerializeField] private GameObject[] resultColorObjects;
    [SerializeField] private float colorChangeDuration = 0.5f;

    // ── Inspector: Fallback ───────────────────────────────────────────────────

    [Header("Fallback (sin spinner asignado)")]
    [SerializeField] private MinigameID[] fallbackMinigameIds;

    // ── Estado interno ────────────────────────────────────────────────────────

    private RoulettePhase currentPhase = RoulettePhase.Intro;

    private MinigameID selectedMinigameId = MinigameID.None;
    private int selectedModifierValue = 0;
    private int selectedModifierIndex = 0; // índice dentro de MinigameConfig.modifiers

    private Vector3 rouletteDestination;
    private Vector3 coverOriginalPosition;
    private Vector3 presenterOriginalPosition;

    private bool hasInitializedSpinner = false;
    private TextMeshPro activeText = null;
    private bool isTyping = false;
    private bool skipRequested = false;
    private bool nextRequested = false;
    private DialogueSequence currentDialogueSequence;
    private int currentDialogueIndex = 0;
    private bool isOpeningDialogue = false;

    // Tweens propios — solo matamos los nuestros, nunca KillAll
    private readonly List<Tween> ownTweens = new();
    private readonly List<Sequence> ownSequences = new();

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Awake()
    {
        ValidateReferences();

        if (minigameRouletteRoot != null)
        {
            rouletteDestination = minigameRouletteRoot.transform.position;
            minigameRouletteRoot.transform.position = rouletteDestination + Vector3.up * 50f;
        }

        if (rouletteCover != null)
            coverOriginalPosition = rouletteCover.transform.position;

        if (presenterSpriteRenderer != null)
            presenterOriginalPosition = presenterSpriteRenderer.transform.localPosition;

        if (centerSprite != null)
            centerSprite.transform.localScale = Vector3.zero;

        if (modifierRouletteRoot != null)
            modifierRouletteRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (minigameSpinner != null) minigameSpinner.OnSpinComplete += HandleMinigameSpinComplete;
        if (modifiersSpinner != null) modifiersSpinner.OnModifierComplete += HandleModifierComplete;
    }

    private void OnDisable()
    {
        if (minigameSpinner != null) minigameSpinner.OnSpinComplete -= HandleMinigameSpinComplete;
        if (modifiersSpinner != null) modifiersSpinner.OnModifierComplete -= HandleModifierComplete;
    }

    private void Start()
    {
        if (hintText != null)
        {
            hintText.text = hintTextContent;
            hintText.gameObject.SetActive(false);
        }

        UpdateHintTextVisibility();
        TransitionTo(RoulettePhase.Intro);
    }

    private void Update()
    {
        // Manejar teclas solo si estamos en una fase de diálogo
        if (currentPhase == RoulettePhase.OpeningDialogue || currentPhase == RoulettePhase.MinigameResultDialogue || currentPhase == RoulettePhase.ModifierResultDialogue)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isTyping)
                {
                    // Si está escribiendo, saltar el efecto de máquina de escribir
                    skipRequested = true;
                }
                else
                {
                    // Si ya terminó, ir al siguiente diálogo
                    nextRequested = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Saltar toda la secuencia de diálogos
                skipRequested = true;
                nextRequested = true;
            }
            
            // Tecla para togglear auto-tipo
            if (Input.GetKeyDown(KeyCode.T))
            {
                SettingsManager.ToggleAutoType();
                UpdateHintTextVisibility();
            }
        }
    }

    private void UpdateHintTextVisibility()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(SettingsManager.AutoTypeEnabled);
        }

        if (autoTypeStatusText != null)
        {
            autoTypeStatusText.text = SettingsManager.AutoTypeEnabled ? "AUTO-TIPO: ON (T para OFF)" : "AUTO-TIPO: OFF (T para ON)";
            autoTypeStatusText.gameObject.SetActive(true);
        }
    }

    // ── Máquina de estados ────────────────────────────────────────────────────

    private void TransitionTo(RoulettePhase next)
    {
        currentPhase = next;
        Debug.Log($"[Roulette] → {next}");

        switch (next)
        {
            case RoulettePhase.Intro:
                StartCoroutine(RunIntro());
                break;

            case RoulettePhase.OpeningDialogue:
                StartCoroutine(RunOpeningDialogue());
                break;

            case RoulettePhase.MinigameRoulette:
                StartCoroutine(RunMinigameRoulette());
                break;

            case RoulettePhase.MinigameResultDialogue:
                StartCoroutine(RunMinigameResultDialogue());
                break;

            case RoulettePhase.ModifierRoulette:
                StartCoroutine(RunModifierRoulette());
                break;

            case RoulettePhase.ModifierResultDialogue:
                StartCoroutine(RunModifierResultDialogue());
                break;

            case RoulettePhase.Loading:
                LoadMinigame();
                break;
        }
    }

    // ── Fases ─────────────────────────────────────────────────────────────────

    private IEnumerator RunIntro()
    {
        // Zoom out + pop del centro simultáneos
        if (mainCamera != null)
        {
            Tween zoomTween = mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.OutQuad);
            TrackTween(zoomTween);
        }

        if (centerSprite != null)
        {
            Tween popTween = centerSprite.transform.DOScale(Vector3.one, centerSpritePopDuration).SetEase(Ease.OutElastic);
            TrackTween(popTween);
        }

        StartLightsLoop();

        yield return new WaitForSeconds(Mathf.Max(zoomDuration, centerSpritePopDuration));

        TransitionTo(RoulettePhase.OpeningDialogue);
    }

    private IEnumerator RunOpeningDialogue()
    {
        DialogueSequence chosen = PickOpeningSequence();
        if (chosen != null)
            yield return StartCoroutine(PlayDialogueSequence(chosen, isOpening: true));

        TransitionTo(RoulettePhase.MinigameRoulette);
    }

    private IEnumerator RunMinigameRoulette()
    {
        if (rouletteDropDelay > 0f)
            yield return new WaitForSeconds(rouletteDropDelay);

        MovePresenterToRoulettePosition();

        yield return StartCoroutine(DropRoulette());

        if (removeCoverAutomatically)
            RemoveCover();

        if (mainCamera != null)
        {
            Tween zoomTween = mainCamera.DOOrthoSize(zoomToRouletteOrthoSize, zoomDuration).SetEase(Ease.InOutQuad);
            TrackTween(zoomTween);
            yield return zoomTween.WaitForCompletion();
        }

        SpinMinigameRoulette();
        // La fase siguiente se dispara por evento HandleMinigameSpinComplete
    }

    private IEnumerator RunMinigameResultDialogue()
    {
        // Zoom out antes de mostrar resultado
        if (mainCamera != null)
        {
            Tween zoomTween = mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.InOutQuad);
            TrackTween(zoomTween);
            yield return zoomTween.WaitForCompletion();
        }

        // Subir la ruleta de minijuego
        if (minigameRouletteRoot != null)
        {
            Tween exitTween = minigameRouletteRoot.transform
                .DOMoveY(minigameRouletteRoot.transform.position.y + 50f, 1f)
                .SetEase(Ease.InQuad);
            TrackTween(exitTween);
            yield return exitTween.WaitForCompletion();
            minigameRouletteRoot.SetActive(false);
        }

        // Return presenter to center
        if (presenterSpriteRenderer != null && presenterCenterPosition != null)
        {
            Tween moveBackTween = presenterSpriteRenderer.transform
                .DOMove(presenterCenterPosition.position, presenterReturnDuration)
                .SetEase(Ease.OutQuad);
            TrackTween(moveBackTween);
            yield return moveBackTween.WaitForCompletion();
        }

        DialogueSequence seq = PickResultDialogue(minigameResultDialogues, selectedMinigameId);
        if (seq != null)
            yield return StartCoroutine(PlayDialogueSequence(seq, isOpening: false));

        TransitionTo(RoulettePhase.ModifierRoulette);
    }

    private IEnumerator RunModifierRoulette()
    {
        MovePresenterToRoulettePosition();

        // Mostrar ruleta de modificadores
        if (modifierRouletteRoot != null)
        {
            modifierRouletteRoot.SetActive(true);
            Vector3 dest = modifierRouletteRoot.transform.position;
            modifierRouletteRoot.transform.position = dest + Vector3.up * 50f;

            Tween enterTween = modifierRouletteRoot.transform
                .DOMove(dest, 1f)
                .SetEase(Ease.OutBounce);
            TrackTween(enterTween);
            yield return enterTween.WaitForCompletion();
        }

        // Inicializar la ruleta de modificadores con el minijuego elegido
        if (modifiersSpinner != null)
            modifiersSpinner.Initialize(selectedMinigameId);
        else
            Debug.LogWarning("[Roulette] modifiersSpinner no asignado, se salta la fase de modificador.");

        // La fase siguiente se dispara por evento HandleModifierComplete
    }

    private IEnumerator RunModifierResultDialogue()
    {
        DialogueSequence seq = PickModifierResultDialogue();

        if (seq != null)
            yield return StartCoroutine(PlayDialogueSequence(seq, isOpening: false));

        TransitionTo(RoulettePhase.Loading);
    }

    /// Busca el diálogo específico del modificador elegido dentro del MinigameConfig.
    private DialogueSequence PickModifierResultDialogue()
    {
        if (modifiersSpinner == null) return null;

        MinigameConfig cfg = modifiersSpinner.GetConfig(selectedMinigameId);
        if (cfg == null || cfg.modifiers == null) return null;

        if (selectedModifierIndex < 0 || selectedModifierIndex >= cfg.modifiers.Length)
            return null;

        ModifierConfig mod = cfg.modifiers[selectedModifierIndex];
        if (mod.resultDialogues == null || mod.resultDialogues.Length == 0)
        {
            Debug.LogWarning($"[Roulette] El modificador '{mod.displayName}' no tiene resultDialogues asignados.");
            return null;
        }

        return mod.resultDialogues[Random.Range(0, mod.resultDialogues.Length)];
    }

    // ── Eventos de spinners ───────────────────────────────────────────────────

    private void HandleMinigameSpinComplete(int winnerId)
    {
        if (currentPhase != RoulettePhase.MinigameRoulette)
        {
            Debug.LogWarning($"[Roulette] HandleMinigameSpinComplete recibido en fase inesperada: {currentPhase}");
            return;
        }

        selectedMinigameId = (MinigameID)winnerId;
        ApplyResultColor(selectedMinigameId);

        TransitionTo(RoulettePhase.MinigameResultDialogue);
    }

    private void HandleModifierComplete(MinigameID minigame, int modifierEnumValue)
    {
        if (currentPhase != RoulettePhase.ModifierRoulette)
        {
            Debug.LogWarning($"[Roulette] HandleModifierComplete recibido en fase inesperada: {currentPhase}");
            return;
        }

        selectedModifierValue = modifierEnumValue;

        // Buscar el índice del modificador elegido dentro del MinigameConfig
        // para poder acceder a sus diálogos específicos
        MinigameConfig cfg = modifiersSpinner != null
            ? modifiersSpinner.GetConfig(minigame)
            : null;

        if (cfg != null && cfg.modifiers != null)
        {
            for (int i = 0; i < cfg.modifiers.Length; i++)
            {
                if (cfg.modifiers[i].enumValue == modifierEnumValue)
                {
                    selectedModifierIndex = i;
                    break;
                }
            }
        }

        TransitionTo(RoulettePhase.ModifierResultDialogue);
    }

    // ── Diálogos ──────────────────────────────────────────────────────────────

    private IEnumerator PlayDialogueSequence(DialogueSequence sequence, bool isOpening)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            yield break;

        currentDialogueSequence = sequence;
        currentDialogueIndex = 0;
        isOpeningDialogue = isOpening;
        Sprite lastSprite = null;
        
        // Mostrar texto de ayuda si auto-tipo está activado
        UpdateHintTextVisibility();

        while (currentDialogueIndex < sequence.lines.Length)
        {
            DialogueLine line = sequence.lines[currentDialogueIndex];

            Sprite spriteToUse = line.presenterSprite != null
                ? line.presenterSprite
                : sequence.defaultPresenterSprite;

            if (presenterSpriteRenderer != null && spriteToUse != null)
            {
                presenterSpriteRenderer.sprite = spriteToUse;
                lastSprite = spriteToUse;
            }

            PresenterJump();
            yield return StartCoroutine(ShowTextBlock(line));
            
            // Resetear requests
            nextRequested = false;
            skipRequested = false;

            // Inicializar spinner en el punto marcado como "final"
            if (line.isFinalDialogue && isOpening && !hasInitializedSpinner)
                hasInitializedSpinner = true;

            currentDialogueIndex++;
            
            // Si no es el último diálogo y auto-tipo está activado, continuar automáticamente
            if (SettingsManager.AutoTypeEnabled && currentDialogueIndex < sequence.lines.Length)
            {
                // Esperar un poquito antes del siguiente
                yield return new WaitForSeconds(0.2f);
            }
            else if (!SettingsManager.AutoTypeEnabled && currentDialogueIndex < sequence.lines.Length)
            {
                // Esperar a que el usuario presione espacio para el siguiente
                while (!nextRequested)
                {
                    yield return null;
                }
            }
        }

        // Ocultar textos de ayuda y estado
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }

        if (autoTypeStatusText != null)
        {
            autoTypeStatusText.gameObject.SetActive(false);
        }

        // Gestionar sprite post-secuencia
        if (!isOpening) yield break;

        if (sequence.keepLastSprite) yield break;

        Sprite restoreSprite = sequence.postSequenceSprite != null
            ? sequence.postSequenceSprite
            : sequence.defaultPresenterSprite;

        if (presenterSpriteRenderer != null && restoreSprite != null)
            presenterSpriteRenderer.sprite = restoreSprite;
    }

    // ── Texto ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowTextBlock(DialogueLine line)
    {
        ClearActiveText();
        skipRequested = false;

        if (textSpawnPoints == null || textSpawnPoints.Length == 0 || textPrefab == null)
        {
            yield return new WaitForSeconds(line.displayDuration);
            yield break;
        }

        Transform spawnPoint = textSpawnPoints[Random.Range(0, textSpawnPoints.Length)];
        TextMeshPro instance = Instantiate(textPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        instance.text = "";
        
        Color c = instance.color;
        instance.color = new Color(c.r, c.g, c.b, 0f);
        instance.transform.localScale = Vector3.one * 0.8f;
        activeText = instance;

        // Fade in and scale
        Sequence showSeq = DOTween.Sequence();
        showSeq.Join(instance.DOFade(1f, textFadeInDuration).SetEase(Ease.OutQuad));
        showSeq.Join(instance.transform.DOScale(Vector3.one, textFadeInDuration).SetEase(Ease.OutBack));
        TrackSequence(showSeq);
        showSeq.Play();
        
        // Wait for fade in
        yield return new WaitForSeconds(textFadeInDuration);
        
        // Typewriter effect
        isTyping = true;
        string fullText = line.text;
        for (int i = 0; i <= fullText.Length; i++)
        {
            if (skipRequested)
            {
                instance.text = fullText;
                break;
            }
            instance.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typewriterSpeed);
        }
        isTyping = false;

        // Esperar a que termine la duración o que el usuario presione espacio
        if (SettingsManager.AutoTypeEnabled)
        {
            // Si auto-tipo está activado, esperar el tiempo normal
            float remainingTime = line.displayDuration - (fullText.Length * typewriterSpeed);
            if (remainingTime > 0)
            {
                float timeWaited = 0f;
                while (timeWaited < remainingTime && !nextRequested)
                {
                    timeWaited += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else
        {
            // Si auto-tipo está desactivado, esperar hasta que el usuario presione espacio
            while (!nextRequested)
            {
                yield return null;
            }
        }

        ClearActiveText();
        yield return new WaitForSeconds(textFadeOutDuration);
    }

    private void ClearActiveText()
    {
        if (activeText == null) return;
        TextMeshPro captured = activeText;
        activeText = null;

        Tween fadeTween = captured.DOFade(0f, textFadeOutDuration).SetEase(Ease.InQuad)
            .OnComplete(() => { if (captured != null) Destroy(captured.gameObject); });
        TrackTween(fadeTween);
    }

    // ── Presentador ───────────────────────────────────────────────────────────

    private void PresenterJump()
    {
        if (presenterSpriteRenderer == null) return;
        Transform t = presenterSpriteRenderer.transform;
        t.DOKill();
        t.localPosition = presenterOriginalPosition;

        Sequence jumpSeq = DOTween.Sequence();
        jumpSeq.Append(t.DOLocalMoveY(presenterOriginalPosition.y + presenterJumpHeight, presenterJumpDuration).SetEase(Ease.OutQuad));
        jumpSeq.Append(t.DOLocalMoveY(presenterOriginalPosition.y, presenterJumpDuration).SetEase(Ease.InQuad));
        TrackSequence(jumpSeq);
        jumpSeq.Play();
    }

    private void MovePresenterToRoulettePosition()
    {
        if (presenterSpriteRenderer == null || presenterRouletteDestination == null) return;
        Tween moveTween = presenterSpriteRenderer.transform
            .DOMove(presenterRouletteDestination.position, 0.5f)
            .SetEase(Ease.OutQuad);
        TrackTween(moveTween);
    }

    // ── Luces ─────────────────────────────────────────────────────────────────

    private void StartLightsLoop()
    {
        if (lights == null || lights.Length == 0) return;

        for (int i = 0; i < lights.Length; i++)
        {
            GameObject light = lights[i];
            if (light == null) continue;

            Vector3 originalPos = light.transform.localPosition;
            float dir = (lightsAlternateDirection && i % 2 != 0) ? -1f : 1f;

            light.transform.localPosition = new Vector3(
                originalPos.x + lightsPendulumAmount * dir,
                originalPos.y,
                originalPos.z
            );

            Tween loopTween = light.transform
                .DOLocalMoveX(originalPos.x - lightsPendulumAmount * dir, lightsPendulumDuration * 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            TrackTween(loopTween);
        }
    }

    // ── Ruleta de minijuego ───────────────────────────────────────────────────

    private IEnumerator DropRoulette()
    {
        if (minigameRouletteRoot == null) yield break;

        bool done = false;

        Sequence dropSeq = DOTween.Sequence();
        dropSeq.Append(minigameRouletteRoot.transform
            .DOMove(rouletteDestination, rouletteDropDuration)
            .SetEase(Ease.OutBounce));
        dropSeq.Append(minigameRouletteRoot.transform
            .DOPunchPosition(Vector3.down * rouletteBounceStrength, rouletteBounceDuration, rouletteBounceVibrato)
            .SetEase(Ease.OutElastic));
        dropSeq.OnComplete(() => done = true);
        TrackSequence(dropSeq);
        dropSeq.Play();

        yield return new WaitUntil(() => done);
    }

    public void RemoveCover()
    {
        if (rouletteCover == null) return;
        Tween coverTween = rouletteCover.transform
            .DOMoveX(coverOriginalPosition.x + coverMoveAmount, coverMoveDuration)
            .SetEase(Ease.OutQuad);
        TrackTween(coverTween);
    }

    private void SpinMinigameRoulette()
    {
        if (minigameSpinner != null)
            minigameSpinner.InitializeSpinner();
        else
            FallbackSelectMinigame();
    }

    private void FallbackSelectMinigame()
    {
        if (fallbackMinigameIds != null && fallbackMinigameIds.Length > 0)
            selectedMinigameId = fallbackMinigameIds[Random.Range(0, fallbackMinigameIds.Length)];
        else
            selectedMinigameId = MinigameID.DodgeDisk;

        Debug.LogWarning($"[Roulette] Usando fallback: {selectedMinigameId}");
        ApplyResultColor(selectedMinigameId);
        TransitionTo(RoulettePhase.MinigameResultDialogue);
    }

    // ── Resultado visual ──────────────────────────────────────────────────────

    private void ApplyResultColor(MinigameID minigame)
    {
        if (resultColorObjects == null || resultColorObjects.Length == 0) return;

        // Buscar color en MinigameConfig del ModifiersSpinner si está disponible
        Color color = GetColorForMinigame(minigame);

        foreach (GameObject obj in resultColorObjects)
        {
            if (obj == null) continue;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Tween t = sr.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
                TrackTween(t);
            }

            UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                Tween t = img.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
                TrackTween(t);
            }
        }
    }

    private Color GetColorForMinigame(MinigameID minigame)
    {
        // El color vive en MinigameConfig. ModifiersSpinner es quien los tiene.
        // Si no hay acceso, devolvemos blanco como fallback.
        if (modifiersSpinner == null) return Color.white;

        // ModifiersSpinner expone FindConfigPublic para que otros puedan leerlo.
        MinigameConfig cfg = modifiersSpinner.GetConfig(minigame);
        return cfg != null ? cfg.resultColor : Color.white;
    }

    // ── Selección de secuencias ───────────────────────────────────────────────

    private DialogueSequence PickOpeningSequence()
    {
        MinigameID lastMinigame = GetLastPlayedMinigame();

        if (lastMinigame != MinigameID.None && returnSequencesByMinigame != null)
        {
            foreach (MinigameReturnDialogues entry in returnSequencesByMinigame)
            {
                if (entry.minigameId == lastMinigame &&
                    entry.sequences != null &&
                    entry.sequences.Length > 0)
                {
                    Debug.Log($"[Roulette] Usando retorno para {lastMinigame} ({entry.minigameName})");
                    return entry.sequences[Random.Range(0, entry.sequences.Length)];
                }
            }
        }

        if (openingSequences != null && openingSequences.Length > 0)
            return openingSequences[Random.Range(0, openingSequences.Length)];

        return null;
    }

    /// <summary>
    /// Busca una secuencia de resultado de minijuego para el ID indicado.
    /// </summary>
    private DialogueSequence PickResultDialogue(
        MinigameResultDialogue[] pool,
        MinigameID minigame)
    {
        if (pool == null) return null;

        foreach (MinigameResultDialogue entry in pool)
        {
            if (entry.minigameId == minigame &&
                entry.sequences != null &&
                entry.sequences.Length > 0)
            {
                return entry.sequences[Random.Range(0, entry.sequences.Length)];
            }
        }
        return null;
    }

    // ── Último minijuego jugado ───────────────────────────────────────────────

    /// <summary>
    /// Lee el último minijuego jugado desde GameManager.
    /// No usa PlayerPrefs como canal de comunicación entre sistemas.
    /// </summary>
    private MinigameID GetLastPlayedMinigame()
    {
        if (GameManager.Instance == null) return MinigameID.None;

        // GameManager.GetPlayedMinigames() debe ser público.
        // Si no existe todavía, agregar: public List<int> GetPlayedMinigames() => playedMinigames;
        List<int> played = GameManager.Instance.GetPlayedMinigames();
        if (played == null || played.Count == 0) return MinigameID.None;

        int lastId = played[played.Count - 1];
        return System.Enum.IsDefined(typeof(MinigameID), lastId)
            ? (MinigameID)lastId
            : MinigameID.None;
    }

    // ── Carga de escena ───────────────────────────────────────────────────────

    private void LoadMinigame()
    {
        KillOwnTweens();

        // Pasar datos a GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.SetSelectedMinigame((int)selectedMinigameId);
        else
            Debug.LogWarning("[Roulette] GameManager no encontrado al cargar minijuego.");

        // Load the actual minigame directly now that Select_Modifier is integrated
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadMinigame((int)selectedMinigameId);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Minigame_" + (int)selectedMinigameId);
    }

    // ── Gestión de tweens propios ─────────────────────────────────────────────

    /// Registra un Tween para poder matarlo en KillOwnTweens().
    private void TrackTween(Tween t)
    {
        if (t != null) ownTweens.Add(t);
    }

    /// Registra una Sequence para poder matarla en KillOwnTweens().
    private void TrackSequence(Sequence s)
    {
        if (s != null) ownSequences.Add(s);
    }

    private void KillOwnTweens()
    {
        foreach (Sequence s in ownSequences) s?.Kill();
        foreach (Tween t in ownTweens) t?.Kill();
        ownSequences.Clear();
        ownTweens.Clear();
    }

    // ── Validaciones ──────────────────────────────────────────────────────────

    private void ValidateReferences()
    {
        if (mainCamera == null) Debug.LogError("[Roulette] mainCamera no asignada.", this);
        if (minigameSpinner == null) Debug.LogWarning("[Roulette] minigameSpinner no asignado — se usará fallback.", this);
        if (modifiersSpinner == null) Debug.LogWarning("[Roulette] modifiersSpinner no asignado.", this);
        if (SceneLoader.Instance == null) Debug.LogError("[Roulette] SceneLoader no encontrado en la escena.", this);
        if (GameManager.Instance == null) Debug.LogError("[Roulette] GameManager no encontrado en la escena.", this);
    }

    // ── Destrucción ───────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        KillOwnTweens();
    }
}