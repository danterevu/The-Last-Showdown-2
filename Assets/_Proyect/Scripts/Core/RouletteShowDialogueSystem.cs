using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)] public string text;
    public Sprite presenterSprite;
    public float displayDuration = 2.5f;
    public bool isFinalDialogue = false;
}

[System.Serializable]
public class DialogueSequence
{
    public string sequenceName;
    public Sprite defaultPresenterSprite;

    [Tooltip("Si true, al terminar la secuencia se mantiene el ultimo sprite mostrado. " +
             "Si false, se restaura defaultPresenterSprite al finalizar.")]
    public bool keepLastSprite = false;

    [Tooltip("Sprite que queda visible despues de la secuencia cuando keepLastSprite es false. " +
             "Si esta vacio se usa defaultPresenterSprite.")]
    public Sprite postSequenceSprite;

    public DialogueLine[] lines;
}

// Agrupa secuencias de retorno para un minijuego especifico
[System.Serializable]
public class MinigameReturnDialogues
{
    [Tooltip("ID del minijuego (1=DodgeDisk, 2=KOH, 3=DNA, 4=Space, 5=ChaseRun)")]
    public int minigameId;
    public string minigameName; // solo para identificar en el Inspector
    public DialogueSequence[] sequences;
}

public class RouletteShowDialogueSystem : MonoBehaviour
{
    [Header("Camara")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float zoomOutOrthoSize = 8f;
    [SerializeField] private float zoomToRouletteOrthoSize = 5f;
    [SerializeField] private float zoomDuration = 1f;

    [Header("Centro")]
    [SerializeField] private GameObject centerSprite;
    [SerializeField] private float centerSpritePopDuration = 0.5f;

    [Header("Luces")]
    [SerializeField] private GameObject[] lights;
    [SerializeField] private float lightsPendulumAmount = 3f;
    [SerializeField] private float lightsPendulumDuration = 1.5f;
    [SerializeField] private bool lightsAlternateDirection = true;

    [Header("Presentador")]
    [SerializeField] private SpriteRenderer presenterSpriteRenderer;
    [SerializeField] private float presenterJumpHeight = 0.5f;
    [SerializeField] private float presenterJumpDuration = 0.25f;
    [SerializeField] private Transform presenterRouletteDestination;

    [Header("Textos del dialogo")]
    [Tooltip("GOs vac�os en la escena donde puede aparecer cada bloque de texto")]
    [SerializeField] private Transform[] textSpawnPoints;
    [Tooltip("Prefab con componente TextMeshPro (3D, NO UI)")]
    [SerializeField] private TextMeshPro textPrefab;
    [SerializeField] private float textFadeInDuration = 0.2f;
    [SerializeField] private float textFadeOutDuration = 0.3f;

    // ── Secuencias de dialogo ─────────────────────────────────────────────────

    [Header("Dialogos de apertura (primera vez)")]
    [Tooltip("Se elige 1 al azar. Se usa cuando es la primera ronda (no hay minijuego previo).")]
    [SerializeField] private DialogueSequence[] openingSequences;

    [Header("Dialogos de retorno por minijuego")]
    [Tooltip("Secuencias que se muestran segun el ultimo minijuego jugado. " +
             "Si no hay entrada para ese minijuego, se usa openingSequences como fallback.")]
    [SerializeField] private MinigameReturnDialogues[] returnSequencesByMinigame;

    [Header("Dialogos de resultado")]
    [SerializeField] private DialogueSequence[] resultSequences;

    // ── Ruleta ────────────────────────────────────────────────────────────────

    [Header("Ruleta")]
    [SerializeField] private GameObject roulette;
    [SerializeField] private float rouletteDropDelay = 0f;
    [SerializeField] private float rouletteDropDuration = 1.2f;
    [SerializeField] private float rouletteBounceStrength = 0.5f;
    [SerializeField] private int rouletteBounceVibrato = 5;
    [SerializeField] private float rouletteBounceDuration = 0.8f;

    [Header("Tapa de la ruleta")]
    [SerializeField] private GameObject rouletteCover;
    [SerializeField] private bool removeCoverAutomatically = true;
    [SerializeField] private float coverMoveAmount = 10f;
    [SerializeField] private float coverMoveDuration = 0.5f;

    [Header("Objetos de resultado")]
    [SerializeField] private GameObject[] resultColorObjects;
    [SerializeField] private Color[] resultColors;
    [SerializeField] private float colorChangeDuration = 0.5f;

    [Header("Referencias")]
    [SerializeField] private MinigameSpinner minigameSpinner;
    [SerializeField] private int[] minigameIds; // fallback si no hay spinner

    // ── Estado interno ────────────────────────────────────────────────────────

    private Sequence mainSequence;
    private Vector3 rouletteDestination;
    private Vector3 coverOriginalPosition;
    private Vector3 presenterOriginalPosition;
    private int selectedMinigameId;
    private bool hasInitializedSpinner = false;
    private TextMeshPro activeText;

    // Sprite del presentador activo al terminar la secuencia de apertura
    // (se guarda para mantenerlo si keepLastSprite = true)
    private Sprite presenterSpriteAfterOpening = null;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (roulette != null)
        {
            rouletteDestination = roulette.transform.position;
            roulette.transform.position = new Vector3(
                rouletteDestination.x,
                rouletteDestination.y + 50f,
                rouletteDestination.z
            );
        }

        if (rouletteCover != null)
            coverOriginalPosition = rouletteCover.transform.position;

        if (presenterSpriteRenderer != null)
            presenterOriginalPosition = presenterSpriteRenderer.transform.localPosition;

        if (centerSprite != null)
            centerSprite.transform.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        if (minigameSpinner != null)
            minigameSpinner.OnSpinComplete += HandleSpinComplete;
    }

    void OnDisable()
    {
        if (minigameSpinner != null)
            minigameSpinner.OnSpinComplete -= HandleSpinComplete;
    }

    void Start()
    {
        PlayShowSequence();
    }

    // ── Secuencia principal ───────────────────────────────────────────────────

    private void PlayShowSequence()
    {
        mainSequence = DOTween.Sequence();

        if (mainCamera != null)
            mainSequence.Append(mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.OutQuad));

        if (centerSprite != null)
            mainSequence.Join(centerSprite.transform.DOScale(Vector3.one, centerSpritePopDuration).SetEase(Ease.OutElastic));

        StartLightsLoop();
        StartCoroutine(PlayOpeningThenDrop());
        mainSequence.Play();
    }

    private IEnumerator PlayOpeningThenDrop()
    {
        yield return null;

        // Elegir secuencia de apertura segun si hay minijuego previo
        DialogueSequence chosen = PickOpeningSequence();

        if (chosen != null)
            yield return StartCoroutine(PlayDialogueSequence(chosen, isOpening: true));

        if (rouletteDropDelay > 0f)
            yield return new WaitForSeconds(rouletteDropDelay);

        MovePresenterToRoulettePosition();

        yield return StartCoroutine(DropRoulette());

        if (removeCoverAutomatically)
            RemoveCover();

        if (mainCamera != null)
            mainCamera.DOOrthoSize(zoomToRouletteOrthoSize, zoomDuration).SetEase(Ease.InOutQuad);

        SpinRoulette();
    }

    // ── Elegir secuencia de apertura ──────────────────────────────────────────

    /// Devuelve la secuencia de apertura correcta.
    /// - Si hay un minijuego previo registrado y existe entrada en returnSequencesByMinigame, la usa.
    /// - Si no, usa openingSequences como fallback.
    private DialogueSequence PickOpeningSequence()
    {
        int lastMinigame = GetLastPlayedMinigame();

        if (lastMinigame > 0 && returnSequencesByMinigame != null)
        {
            Debug.Log($"[RouletteShow] LastPlayedMinigame = {lastMinigame}");
            foreach (MinigameReturnDialogues entry in returnSequencesByMinigame)
            {
                Debug.Log($"[RouletteShow] Entry id={entry.minigameId} | sequences={entry.sequences?.Length ?? 0}");
                if (entry.minigameId == lastMinigame && entry.sequences != null && entry.sequences.Length > 0)
                {
                    
                    Debug.Log($"[RouletteShow] Usando secuencias de retorno para minijuego {lastMinigame} ({entry.minigameName})");
                    return entry.sequences[Random.Range(0, entry.sequences.Length)];
                }
            }
            Debug.Log($"[RouletteShow] No hay secuencias de retorno para minijuego {lastMinigame}, usando apertura generica");
        }

        if (openingSequences != null && openingSequences.Length > 0)
            return openingSequences[Random.Range(0, openingSequences.Length)];

        return null;
    }

    /// Lee el ultimo minijuego jugado.
    /// Primero intenta leerlo de PlayerPrefs ("LastPlayedMinigame"),
    /// luego hace fallback a la lista de GameManager si existe.
    private int GetLastPlayedMinigame()
    {
        // PlayerPrefs tiene prioridad (seteado desde cada manager al terminar)
        int fromPrefs = PlayerPrefs.GetInt("LastPlayedMinigame", 0);
        if (fromPrefs > 0)
            return fromPrefs;

        // Fallback: leer el ultimo de la lista de GameManager
        if (GameManager.Instance != null)
        {
            List<int> played = GetPlayedMinigamesFromGameManager();
            if (played != null && played.Count > 0)
                return played[played.Count - 1];
        }

        return 0; // primera ronda, sin previo
    }

    /// Obtiene la lista de minijuegos jugados desde GameManager.
    /// Usa reflexion para no depender de que el campo sea publico.
    private List<int> GetPlayedMinigamesFromGameManager()
    {
        if (GameManager.Instance == null) return null;

        // GameManager.playedMinigames es private. Si lo necesitas publico,
        // agrega un getter en GameManager: public List<int> GetPlayedMinigames() => playedMinigames;
        // Por ahora intentamos con el getter si existe, sino devolvemos null.
        var method = GameManager.Instance.GetType().GetMethod("GetPlayedMinigames");
        if (method != null)
            return method.Invoke(GameManager.Instance, null) as List<int>;

        return null;
    }

    // ── Dialogos ──────────────────────────────────────────────────────────────

    /// isOpening = true para la secuencia de apertura (guarda el sprite final).
    /// isOpening = false para la de resultado.
    private IEnumerator PlayDialogueSequence(DialogueSequence sequence, bool isOpening)
    {
        if (sequence.lines == null || sequence.lines.Length == 0) yield break;

        Sprite lastLineSprite = null;

        for (int i = 0; i < sequence.lines.Length; i++)
        {
            DialogueLine line = sequence.lines[i];

            Sprite spriteToUse = line.presenterSprite != null ? line.presenterSprite : sequence.defaultPresenterSprite;
            if (presenterSpriteRenderer != null && spriteToUse != null)
            {
                presenterSpriteRenderer.sprite = spriteToUse;
                lastLineSprite = spriteToUse;
            }

            PresenterJump();
            yield return StartCoroutine(ShowTextBlock(line));

            if (line.isFinalDialogue && isOpening && !hasInitializedSpinner)
                hasInitializedSpinner = true;
        }

        // Manejar sprite post-secuencia
        if (isOpening)
        {
            if (sequence.keepLastSprite)
            {
                // Mantener el ultimo sprite (no hacer nada)
                presenterSpriteAfterOpening = lastLineSprite;
            }
            else
            {
                // Restaurar al sprite de cierre o al default
                Sprite restoreSprite = sequence.postSequenceSprite != null
                    ? sequence.postSequenceSprite
                    : sequence.defaultPresenterSprite;

                if (presenterSpriteRenderer != null && restoreSprite != null)
                    presenterSpriteRenderer.sprite = restoreSprite;

                presenterSpriteAfterOpening = restoreSprite;
            }
        }
    }

    // ── Texto ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowTextBlock(DialogueLine line)
    {
        ClearActiveText();

        if (textSpawnPoints == null || textSpawnPoints.Length == 0 || textPrefab == null)
        {
            yield return new WaitForSeconds(line.displayDuration);
            yield break;
        }

        Transform spawnPoint = textSpawnPoints[Random.Range(0, textSpawnPoints.Length)];
        TextMeshPro instance = Instantiate(textPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        instance.text = line.text;

        Color c = instance.color;
        instance.color = new Color(c.r, c.g, c.b, 0f);
        instance.transform.localScale = Vector3.one * 0.8f;
        activeText = instance;

        Sequence showSeq = DOTween.Sequence();
        showSeq.Join(instance.DOFade(1f, textFadeInDuration).SetEase(Ease.OutQuad));
        showSeq.Join(instance.transform.DOScale(Vector3.one, textFadeInDuration).SetEase(Ease.OutBack));
        showSeq.Play();

        yield return new WaitForSeconds(line.displayDuration);

        ClearActiveText();
        yield return new WaitForSeconds(textFadeOutDuration);
    }

    private void ClearActiveText()
    {
        if (activeText == null) return;
        TextMeshPro captured = activeText;
        activeText = null;
        captured.DOFade(0f, textFadeOutDuration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            if (captured != null) Destroy(captured.gameObject);
        });
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
        jumpSeq.Play();
    }

    private void MovePresenterToRoulettePosition()
    {
        if (presenterSpriteRenderer == null || presenterRouletteDestination == null) return;
        presenterSpriteRenderer.transform.DOMove(presenterRouletteDestination.position, 0.5f).SetEase(Ease.OutQuad);
    }

    // ── Luces ─────────────────────────────────────────────────────────────────

    private void StartLightsLoop()
    {
        if (lights == null || lights.Length == 0) return;

        for (int i = 0; i < lights.Length; i++)
        {
            GameObject light = lights[i];
            Vector3 originalPos = light.transform.localPosition;
            float direction = (lightsAlternateDirection && i % 2 != 0) ? -1f : 1f;

            light.transform.localPosition = new Vector3(
                originalPos.x + lightsPendulumAmount * direction,
                originalPos.y,
                originalPos.z
            );

            light.transform.DOLocalMoveX(originalPos.x - lightsPendulumAmount * direction, lightsPendulumDuration * 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    // ── Ruleta ────────────────────────────────────────────────────────────────

    private IEnumerator DropRoulette()
    {
        if (roulette == null) yield break;

        bool dropDone = false;

        Sequence dropSeq = DOTween.Sequence();
        dropSeq.Append(roulette.transform.DOMove(rouletteDestination, rouletteDropDuration).SetEase(Ease.OutBounce));
        dropSeq.Append(roulette.transform.DOPunchPosition(
            Vector3.down * rouletteBounceStrength,
            rouletteBounceDuration,
            rouletteBounceVibrato
        ).SetEase(Ease.OutElastic));
        dropSeq.OnComplete(() => dropDone = true);
        dropSeq.Play();

        yield return new WaitUntil(() => dropDone);
    }

    public void RemoveCover()
    {
        if (rouletteCover == null) return;
        rouletteCover.transform.DOMoveX(coverOriginalPosition.x + coverMoveAmount, coverMoveDuration).SetEase(Ease.OutQuad);
    }

    private void SpinRoulette()
    {
        if (minigameSpinner != null)
            minigameSpinner.InitializeSpinner();
        else
            FallbackSelectResult();
    }

    // ── Resultado ─────────────────────────────────────────────────────────────

    private void HandleSpinComplete(int winnerId)
    {
        selectedMinigameId = winnerId;

        int colorIndex = (winnerId - 1) % Mathf.Max(1, resultColors.Length);
        if (resultColors.Length > 0)
            ApplyResultColor(resultColors[colorIndex]);

        if (mainCamera != null)
            mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.InOutQuad).OnComplete(PlayResultDialogues);
        else
            PlayResultDialogues();
    }

    private void FallbackSelectResult()
    {
        if (minigameIds == null || minigameIds.Length == 0) { LoadMinigame(); return; }
        selectedMinigameId = minigameIds[Random.Range(0, minigameIds.Length)];
        LoadMinigame();
    }

    private void PlayResultDialogues()
    {
        if (resultSequences == null || resultSequences.Length == 0) { LoadMinigame(); return; }
        DialogueSequence chosen = resultSequences[Random.Range(0, resultSequences.Length)];
        StartCoroutine(PlayResultThenLoad(chosen));
    }

    private IEnumerator PlayResultThenLoad(DialogueSequence sequence)
    {
        yield return StartCoroutine(PlayDialogueSequence(sequence, isOpening: false));
        LoadMinigame();
    }

    private void ApplyResultColor(Color color)
    {
        if (resultColorObjects == null) return;
        foreach (var obj in resultColorObjects)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);

            UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
        }
    }

    // ── Carga ─────────────────────────────────────────────────────────────────

    private void LoadMinigame()
    {
        KillAllTweens();
        PlayerPrefs.SetInt("SelectedMinigame", selectedMinigameId);

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSelectModifier();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Select_Modifier");
    }

    private void KillAllTweens()
    {
        mainSequence?.Kill();
        DOTween.KillAll();
    }

    void OnDestroy()
    {
        KillAllTweens();
    }
}