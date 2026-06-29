using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RouletteShowSequence : MonoBehaviour
{
    [Header("Camara")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float initialOrthoSize = 3f;
    [SerializeField] private float zoomOutOrthoSize = 8f;
    [SerializeField] private float zoomToRouletteOrthoSize = 5f;
    [SerializeField] private float zoomDuration = 1f;

    [Header("Centro")]
    [SerializeField] private GameObject centerSprite;
    [SerializeField] private float centerSpritePopDuration = 0.5f;

    [Header("Luces")]
    [SerializeField] private GameObject[] lights;
    [SerializeField] private float lightsPendulumAmount = 3f;   // unidades que se mueve a cada lado
    [SerializeField] private float lightsPendulumDuration = 1.5f; // tiempo de un lado al otro
    [SerializeField] private bool lightsAlternateDirection = true;

    [Header("Presentador")]
    [SerializeField] private SpriteRenderer presenterSpriteRenderer;
    [SerializeField] private float presenterJumpHeight = 0.5f;
    [SerializeField] private float presenterJumpDuration = 0.25f;

    [Header("Textos del diálogo")]
    [Tooltip("GOs vacíos donde pueden aparecer los fragmentos de texto")]
    [SerializeField] private Transform[] textSpawnPoints;
    [Tooltip("Prefab con TextMeshPro para cada fragmento de texto")]
    [SerializeField] private TextMeshProUGUI textPrefab;
    [SerializeField] private float wordFadeInDuration = 0.15f;
    [SerializeField] private float wordStayDuration = 2f;
    [SerializeField] private float wordFadeOutDuration = 0.3f;
    [SerializeField] private float timeBetweenWords = 0.2f;
    [SerializeField] private string[] openingDialogues;
    [SerializeField] private string[] resultDialogues;

    [Header("Ruleta")]
    [SerializeField] private GameObject roulette;
    [SerializeField] private MinigameSpinner minigameSpinner;
    [SerializeField] private float rouletteDropHeight = 10f;
    [SerializeField] private float rouletteDropDuration = 1.2f;
    [SerializeField] private float rouletteBounceStrength = 0.5f;
    [SerializeField] private int rouletteBounceVibrato = 5;
    [SerializeField] private float rouletteBounceDuration = 0.8f;

    [Header("Tapa de la ruleta")]
    [SerializeField] private GameObject rouletteCover;
    [SerializeField] private float coverMoveAmount = 10f;
    [SerializeField] private float coverMoveDuration = 0.5f;

    [Header("Objetos de resultado")]
    [SerializeField] private GameObject[] resultColorObjects;
    [SerializeField] private Color[] resultColors;
    [SerializeField] private float colorChangeDuration = 0.5f;

    [Header("Selección de minijuego")]
    [SerializeField] private int[] minigameIds;
    [SerializeField] private float finalDialogueDelay = 2f;

    private Sequence mainSequence;
    private Vector3 rouletteDestination;
    private Vector3 coverOriginalPosition;
    private Vector3 presenterOriginalPosition;
    private int selectedMinigameId;

    // Pool de textos activos para limpiarlos entre diálogos
    private List<TextMeshProUGUI> activeWordTexts = new List<TextMeshProUGUI>();

    void Awake()
    {
        if (roulette != null)
        {
            rouletteDestination = roulette.transform.position;
            roulette.transform.position = new Vector3(
                rouletteDestination.x,
                rouletteDestination.y + rouletteDropHeight,
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

    void Start()
    {
        AudioManager.Instance?.ResumeMusic(SoundID.SelectionMusic);
        PlayShowSequence();
    }

    // ─── Secuencia principal ───────────────────────────────────────────────────

    private void PlayShowSequence()
    {
        mainSequence = DOTween.Sequence();

        if (mainCamera != null)
            mainSequence.Append(mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.OutQuad));

        if (centerSprite != null)
            mainSequence.Join(centerSprite.transform.DOScale(Vector3.one, centerSpritePopDuration).SetEase(Ease.OutElastic));

        StartLightsLoop();

        // Calcular duración total de cada diálogo para encadenarlos
        float dialogueTotalDuration = EstimateDialogueDuration();

        float dialogueTime = mainSequence.Duration();
        for (int i = 0; i < openingDialogues.Length; i++)
        {
            int index = i;
            mainSequence.InsertCallback(dialogueTime, () => StartCoroutine(PlayDialogue(openingDialogues[index])));
            dialogueTime += dialogueTotalDuration;
        }

        mainSequence.InsertCallback(dialogueTime, DropAndSpinRoulette);
        dialogueTime += rouletteDropDuration + rouletteBounceDuration;

        mainSequence.InsertCallback(dialogueTime, RemoveCover);
        dialogueTime += coverMoveDuration;

        if (mainCamera != null)
            mainSequence.Insert(dialogueTime, mainCamera.DOOrthoSize(zoomToRouletteOrthoSize, zoomDuration).SetEase(Ease.InOutQuad));

        mainSequence.Play();
    }

    // Estimación del tiempo que tarda un diálogo completo (para encadenar correctamente)
    private float EstimateDialogueDuration()
    {
        // Promedio de palabras por diálogo * tiempo por palabra + fade out final
        return wordStayDuration + wordFadeOutDuration + 0.5f;
    }

    // ─── Diálogo: palabras en posiciones aleatorias ────────────────────────────

    private IEnumerator PlayDialogue(string dialogue)
    {
        if (textSpawnPoints == null || textSpawnPoints.Length == 0 || textPrefab == null)
            yield break;

        // Limpiar textos anteriores
        ClearAllWordTexts();

        // Salto del presentador al empezar el diálogo
        PresenterJump();

        // Separar el texto por espacios
        string[] words = dialogue.Split(' ');

        // Llevar registro de qué puntos están ocupados
        List<int> availablePoints = new List<int>();
        for (int i = 0; i < textSpawnPoints.Length; i++)
            availablePoints.Add(i);

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i].Trim();
            if (string.IsNullOrEmpty(word)) continue;

            // Si no quedan puntos libres, reciclar todos
            if (availablePoints.Count == 0)
            {
                for (int j = 0; j < textSpawnPoints.Length; j++)
                    availablePoints.Add(j);
            }

            // Elegir punto aleatorio
            int randomIndex = Random.Range(0, availablePoints.Count);
            int pointIndex = availablePoints[randomIndex];
            availablePoints.RemoveAt(randomIndex);

            Transform spawnPoint = textSpawnPoints[pointIndex];

            SpawnWordText(word, spawnPoint);

            yield return new WaitForSeconds(timeBetweenWords);
        }

        // Esperar que los textos se lean y luego fadeout de todos
        yield return new WaitForSeconds(wordStayDuration);
        ClearAllWordTexts();
    }

    private void SpawnWordText(string word, Transform spawnPoint)
    {
        TextMeshProUGUI instance = Instantiate(textPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
        instance.text = word;

        Color c = instance.color;
        instance.color = new Color(c.r, c.g, c.b, 0f);
        instance.transform.localScale = Vector3.one * 0.7f;

        activeWordTexts.Add(instance);

        // Fade in + pop
        Sequence wordSeq = DOTween.Sequence();
        wordSeq.Join(instance.DOFade(1f, wordFadeInDuration).SetEase(Ease.OutQuad));
        wordSeq.Join(instance.transform.DOScale(Vector3.one, wordFadeInDuration).SetEase(Ease.OutBack));
        wordSeq.Play();
    }

    private void ClearAllWordTexts()
    {
        foreach (TextMeshProUGUI t in activeWordTexts)
        {
            if (t == null) continue;
            t.DOFade(0f, wordFadeOutDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                if (t != null) Destroy(t.gameObject);
            });
        }
        activeWordTexts.Clear();
    }

    // ─── Salto del presentador ─────────────────────────────────────────────────

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

    // ─── Luces péndulo ─────────────────────────────────────────────────────────

    private void StartLightsLoop()
    {
        if (lights == null || lights.Length == 0) return;

        for (int i = 0; i < lights.Length; i++)
        {
            GameObject light = lights[i];
            Vector3 originalPos = light.transform.localPosition;
            float direction = (lightsAlternateDirection && i % 2 != 0) ? -1f : 1f;

            // Arrancar desde el lado opuesto para que se vea el movimiento desde el inicio
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

    // ─── Ruleta ────────────────────────────────────────────────────────────────

    private void DropAndSpinRoulette()
    {
        if (roulette == null) return;

        Sequence dropSequence = DOTween.Sequence();
        dropSequence.Append(
            roulette.transform.DOMove(rouletteDestination, rouletteDropDuration)
                .SetEase(Ease.OutBounce)
        );
        dropSequence.Append(
            roulette.transform.DOPunchPosition(
                Vector3.down * rouletteBounceStrength,
                rouletteBounceDuration,
                rouletteBounceVibrato
            ).SetEase(Ease.OutElastic)
        );
        dropSequence.OnComplete(SpinAndGetResult);
        dropSequence.Play();
    }

    private void RemoveCover()
    {
        if (rouletteCover == null) return;
        rouletteCover.transform.DOMoveX(coverOriginalPosition.x + coverMoveAmount, coverMoveDuration).SetEase(Ease.OutQuad);
    }

    private void SpinAndGetResult()
    {
        if (minigameSpinner != null)
        {
            minigameSpinner.OnSpinComplete += OnSpinnerResult;
            minigameSpinner.InitializeSpinner();
            return;
        }

        FallbackSelectResult();
    }

    private void OnSpinnerResult(int minigameId)
    {
        if (minigameSpinner != null)
            minigameSpinner.OnSpinComplete -= OnSpinnerResult;

        selectedMinigameId = minigameId;

        int colorIndex = (minigameId - 1) % Mathf.Max(1, resultColors.Length);
        if (resultColors.Length > 0)
            ApplyResultColor(resultColors[colorIndex]);

        if (mainCamera != null)
            mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.InOutQuad).OnComplete(ShowResultDialogues);
        else
            ShowResultDialogues();
    }

    private void FallbackSelectResult()
    {
        if (resultColors.Length == 0 || minigameIds.Length == 0)
        {
            LoadMinigame();
            return;
        }

        int resultIndex = Random.Range(0, resultColors.Length);
        selectedMinigameId = minigameIds[resultIndex % minigameIds.Length];
        ApplyResultColor(resultColors[resultIndex]);

        if (mainCamera != null)
            mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.InOutQuad).OnComplete(ShowResultDialogues);
        else
            ShowResultDialogues();
    }

    // ─── Resultado ─────────────────────────────────────────────────────────────

    private void ApplyResultColor(Color color)
    {
        if (resultColorObjects == null) return;

        foreach (var obj in resultColorObjects)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);

            Image img = obj.GetComponent<Image>();
            if (img != null) img.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
        }
    }

    private void ShowResultDialogues()
    {
        float delay = 0f;
        for (int i = 0; i < resultDialogues.Length; i++)
        {
            int index = i;
            DOVirtual.DelayedCall(delay, () => StartCoroutine(PlayDialogue(resultDialogues[index])));
            delay += EstimateDialogueDuration();
        }

        DOVirtual.DelayedCall(delay + finalDialogueDelay, LoadMinigame);
    }

    private void LoadMinigame()
    {
        KillAllTweens();
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadMinigame(selectedMinigameId);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Minigame_" + selectedMinigameId);
    }

    // ─── Limpieza ──────────────────────────────────────────────────────────────

    private void KillAllTweens()
    {
        mainSequence?.Kill();
        DOTween.KillAll();
    }

    void OnDestroy()
    {
        if (minigameSpinner != null)
            minigameSpinner.OnSpinComplete -= OnSpinnerResult;

        KillAllTweens();
    }
}