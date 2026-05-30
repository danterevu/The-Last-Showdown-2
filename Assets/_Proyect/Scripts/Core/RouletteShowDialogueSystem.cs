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
    public string sequenceName; // solo para identificar en el Inspector
    public Sprite defaultPresenterSprite;
    public DialogueLine[] lines;
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
    [SerializeField] private Transform presenterRouletteDestination; // GO destino al caer la ruleta

    [Header("Textos del diálogo")]
    [Tooltip("GOs vacíos en la escena donde puede aparecer cada bloque de texto")]
    [SerializeField] private Transform[] textSpawnPoints;
    [Tooltip("Prefab con componente TextMeshPro (3D, NO UI)")]
    [SerializeField] private TextMeshPro textPrefab;
    [SerializeField] private float textFadeInDuration = 0.2f;
    [SerializeField] private float textFadeOutDuration = 0.3f;

    [Header("Secuencias de diálogo (se elige 1 al azar)")]
    [SerializeField] private DialogueSequence[] openingSequences;
    [SerializeField] private DialogueSequence[] resultSequences;

    [Header("Ruleta")]
    [SerializeField] private GameObject roulette;
    [SerializeField] private float rouletteDropDelay = 0f; // delay antes de caer (en segundos)
    [SerializeField] private float rouletteDropDuration = 1.2f;
    [SerializeField] private float rouletteBounceStrength = 0.5f;
    [SerializeField] private int rouletteBounceVibrato = 5;
    [SerializeField] private float rouletteBounceDuration = 0.8f;

    [Header("Tapa de la ruleta")]
    [SerializeField] private GameObject rouletteCover;
    [SerializeField] private bool removeCoverAutomatically = true; // si es false, sacarla a mano
    [SerializeField] private float coverMoveAmount = 10f;
    [SerializeField] private float coverMoveDuration = 0.5f;

    [Header("Objetos de resultado")]
    [SerializeField] private GameObject[] resultColorObjects;
    [SerializeField] private Color[] resultColors;
    [SerializeField] private float colorChangeDuration = 0.5f;

    [Header("Referencias")]
    [SerializeField] private MinigameSpinner minigameSpinner;

    // ── Estado interno ────────────────────────────────────────────────────────

    private Sequence mainSequence;
    private Vector3 rouletteDestination;
    private Vector3 coverOriginalPosition;
    private Vector3 presenterOriginalPosition;
    private int selectedMinigameId;
    private bool hasInitializedSpinner = false;

    private TextMeshPro activeText;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    void Awake()
    {
        // Guardar destino de la ruleta y moverla fuera de pantalla hacia arriba
        if (roulette != null)
        {
            rouletteDestination = roulette.transform.position;
            roulette.transform.position = new Vector3(
                rouletteDestination.x,
                rouletteDestination.y + 50f, // fuera de camara siempre
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

        // Arrancar los diálogos de apertura como coroutine independiente
        // para que no bloqueen ni dependan del timing del mainSequence
        StartCoroutine(PlayOpeningThenDrop());

        mainSequence.Play();
    }

    private IEnumerator PlayOpeningThenDrop()
    {
        yield return null;

        // Diálogos de apertura
        if (openingSequences != null && openingSequences.Length > 0)
        {
            DialogueSequence chosen = openingSequences[Random.Range(0, openingSequences.Length)];
            yield return StartCoroutine(PlayDialogueSequence(chosen, true));
        }

        // Delay configurable antes de caer
        if (rouletteDropDelay > 0f)
            yield return new WaitForSeconds(rouletteDropDelay);

        // El presentador se mueve ANTES de la caída
        MovePresenterToRoulettePosition();

        // Caer la ruleta
        yield return StartCoroutine(DropRoulette());

        // Sacar la tapa automáticamente si está configurado
        if (removeCoverAutomatically)
            RemoveCover();

        // Zoom hacia la ruleta
        if (mainCamera != null)
            mainCamera.DOOrthoSize(zoomToRouletteOrthoSize, zoomDuration)
                .SetEase(Ease.InOutQuad);

        // Recién acá empieza a girar
        SpinRoulette();
    }

    // ── Diálogos ──────────────────────────────────────────────────────────────

    private IEnumerator PlayDialogueSequence(DialogueSequence sequence, bool activatesRoulette)
    {
        if (sequence.lines == null || sequence.lines.Length == 0) yield break;

        for (int i = 0; i < sequence.lines.Length; i++)
        {
            DialogueLine line = sequence.lines[i];

            // Cambiar sprite + salto del presentador
            Sprite spriteToUse = line.presenterSprite != null ? line.presenterSprite : sequence.defaultPresenterSprite;
            if (presenterSpriteRenderer != null && spriteToUse != null)
                presenterSpriteRenderer.sprite = spriteToUse;

            PresenterJump();

            // Mostrar el texto como bloque en un spawn point aleatorio
            yield return StartCoroutine(ShowTextBlock(line));

            if (line.isFinalDialogue && activatesRoulette && !hasInitializedSpinner)
            {
                hasInitializedSpinner = true;
            }
        }
    }

    private IEnumerator ShowTextBlock(DialogueLine line)
    {
        // Limpiar texto anterior
        ClearActiveText();

        if (textSpawnPoints == null || textSpawnPoints.Length == 0 || textPrefab == null)
        {
            yield return new WaitForSeconds(line.displayDuration);
            yield break;
        }

        // Elegir spawn point aleatorio
        Transform spawnPoint = textSpawnPoints[Random.Range(0, textSpawnPoints.Length)];

        // Instanciar texto 3D
        TextMeshPro instance = Instantiate(textPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        instance.text = line.text;

        Color c = instance.color;
        instance.color = new Color(c.r, c.g, c.b, 0f);
        instance.transform.localScale = Vector3.one * 0.8f;
        activeText = instance;

        // Fade in + pop
        Sequence showSeq = DOTween.Sequence();
        showSeq.Join(instance.DOFade(1f, textFadeInDuration).SetEase(Ease.OutQuad));
        showSeq.Join(instance.transform.DOScale(Vector3.one, textFadeInDuration).SetEase(Ease.OutBack));
        showSeq.Play();

        yield return new WaitForSeconds(line.displayDuration);

        ClearActiveText();

        // Pequeña pausa entre líneas
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

    // ── Salto del presentador ─────────────────────────────────────────────────

    private void PresenterJump()
    {
        if (presenterSpriteRenderer == null) return;

        Transform t = presenterSpriteRenderer.transform;
        t.DOKill();
        t.localPosition = presenterOriginalPosition;

        Sequence jumpSeq = DOTween.Sequence();
        jumpSeq.Append(t.DOLocalMoveY(presenterOriginalPosition.y + presenterJumpHeight, presenterJumpDuration)
            .SetEase(Ease.OutQuad));
        jumpSeq.Append(t.DOLocalMoveY(presenterOriginalPosition.y, presenterJumpDuration)
            .SetEase(Ease.InQuad));
        jumpSeq.Play();
    }

    // Mover el presentador hacia el GO destino al caer la ruleta
    private void MovePresenterToRoulettePosition()
    {
        if (presenterSpriteRenderer == null || presenterRouletteDestination == null) return;

        presenterSpriteRenderer.transform.DOMove(
            presenterRouletteDestination.position,
            0.5f
        ).SetEase(Ease.OutQuad);
    }

    // ── Luces péndulo ─────────────────────────────────────────────────────────

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
        dropSeq.OnComplete(() =>
        {
            dropDone = true;
            MovePresenterToRoulettePosition();
        });
        dropSeq.Play();

        yield return new WaitUntil(() => dropDone);
    }

    // Método público para sacar la cortina manualmente desde el Inspector o un botón
    public void RemoveCover()
    {
        if (rouletteCover == null) return;
        rouletteCover.transform.DOMoveX(coverOriginalPosition.x + coverMoveAmount, coverMoveDuration).SetEase(Ease.OutQuad);
    }

    private void SpinRoulette()
    {
        if (minigameSpinner != null)
        {
            minigameSpinner.InitializeSpinner();
        }
        else
        {
            FallbackSelectResult();
        }
    }

    // ── Resultado ─────────────────────────────────────────────────────────────

    private void HandleSpinComplete(int winnerId)
    {
        selectedMinigameId = winnerId;

        int colorIndex = (winnerId - 1) % Mathf.Max(1, resultColors.Length);

        if (resultColors.Length > 0)
            ApplyResultColor(resultColors[colorIndex]);

        if (mainCamera != null)
        {
            mainCamera.DOOrthoSize(
                zoomOutOrthoSize,
                zoomDuration
            )
            .SetEase(Ease.InOutQuad)
            .OnComplete(PlayResultDialogues);
        }
        else
        {
            PlayResultDialogues();
        }
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
        yield return StartCoroutine(PlayDialogueSequence(sequence, false));
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

    [SerializeField] private int[] minigameIds; // fallback si no hay spinner

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