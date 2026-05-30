using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)] public string text;
    public Sprite presenterSprite;
    public float displayDuration = 2.5f;
    public float typeSpeed = 0.05f;
    public bool isFinalDialogue = false;
}

[System.Serializable]
public class DialogueSequence
{
    public List<DialogueLine> lines = new List<DialogueLine>();
    public Sprite defaultPresenterSprite;
}

public class RouletteShowDialogueSystem : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float initialOrthoSize = 3f;
    [SerializeField] private float zoomOutOrthoSize = 8f;
    [SerializeField] private float zoomToRouletteOrthoSize = 5f;
    [SerializeField] private float zoomDuration = 1f;

    [Header("Center Sprite")]
    [SerializeField] private GameObject centerSprite;
    [SerializeField] private float centerSpritePopDuration = 0.5f;

    [Header("Lights")]
    [SerializeField] private GameObject[] lights;
    [SerializeField] private float lightsMoveAmount = 5f;
    [SerializeField] private float lightsMoveDuration = 2f;

    [Header("Presenter & Dialogues")]
    [SerializeField] private SpriteRenderer presenterSpriteRenderer;
    [SerializeField] private RectTransform dialogueContainer;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float dialogueRiseAmount = 30f;
    [SerializeField] private float dialogueRiseDuration = 0.5f;
    [SerializeField] private float dialoguePopAmount = 1.2f;

    [Header("Roulette")]
    [SerializeField] private GameObject roulette;
    [SerializeField] private float rouletteDropHeight = 10f;
    [SerializeField] private float rouletteDropDuration = 1f;
    [SerializeField] private float rouletteBounceStrength = 1f;
    [SerializeField] private float rouletteBounceDuration = 1f;

    [Header("Roulette Cover")]
    [SerializeField] private GameObject rouletteCover;
    [SerializeField] private float coverMoveAmount = 10f;
    [SerializeField] private float coverMoveDuration = 0.5f;

    [Header("Result Objects")]
    [SerializeField] private GameObject[] resultColorObjects;
    [SerializeField] private Color[] resultColors;
    [SerializeField] private float colorChangeDuration = 0.5f;

    [Header("Dialogue Sequences")]
    [SerializeField] private List<DialogueSequence> dialogueSequenceVariations = new List<DialogueSequence>();
    [SerializeField] private List<DialogueSequence> resultDialogueSequenceVariations = new List<DialogueSequence>();

    [Header("References")]
    [SerializeField] private MinigameSpinner minigameSpinner;

    private Sequence mainSequence;
    private Vector3 rouletteOriginalPosition;
    private Vector3 coverOriginalPosition;
    private Vector3 dialogueOriginalPosition;
    private int selectedMinigameId;
    private DialogueSequence currentOpeningSequence;
    private DialogueSequence currentResultSequence;
    private Coroutine typingCoroutine;
    private bool isWaitingForSpinResult = false;
    private bool hasInitializedSpinner = false;

    void Awake()
    {
        if (roulette != null)
        {
            rouletteOriginalPosition = roulette.transform.localPosition;
            roulette.transform.localPosition = new Vector3(rouletteOriginalPosition.x, rouletteOriginalPosition.y + rouletteDropHeight, rouletteOriginalPosition.z);
        }

        if (rouletteCover != null)
        {
            coverOriginalPosition = rouletteCover.transform.localPosition;
        }

        if (dialogueText != null)
        {
            dialogueOriginalPosition = dialogueText.transform.localPosition;
            dialogueText.alpha = 0f;
            dialogueText.text = "";
        }

        if (centerSprite != null)
        {
            centerSprite.transform.localScale = Vector3.zero;
        }
    }

    void OnEnable()
    {
        if (minigameSpinner != null)
        {
            minigameSpinner.OnSpinComplete += HandleSpinComplete;
        }
    }

    void OnDisable()
    {
        if (minigameSpinner != null)
        {
            minigameSpinner.OnSpinComplete -= HandleSpinComplete;
        }
    }

    void Start()
    {
        PlayShowSequence();
    }

    private void PlayShowSequence()
    {
        mainSequence = DOTween.Sequence();

        // 1. Zoom out camera
        if (mainCamera != null)
        {
            mainSequence.Append(mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.OutQuad));
        }

        // 2. Pop center sprite
        if (centerSprite != null)
        {
            mainSequence.Join(centerSprite.transform.DOScale(Vector3.one, centerSpritePopDuration).SetEase(Ease.OutElastic));
        }

        // 3. Start lights loop
        StartLightsLoop();

        // 4. Pick random opening dialogue sequence
        PickRandomOpeningSequence();

        // 5. Drop roulette with bounce
        mainSequence.AppendCallback(DropRoulette);
        mainSequence.AppendInterval(rouletteDropDuration + rouletteBounceDuration);

        // 6. Remove cover
        mainSequence.AppendCallback(RemoveCover);
        mainSequence.AppendInterval(coverMoveDuration);

        // 7. Zoom to roulette
        if (mainCamera != null)
        {
            mainSequence.Append(mainCamera.DOOrthoSize(zoomToRouletteOrthoSize, zoomDuration).SetEase(Ease.InOutQuad));
        }

        mainSequence.Play();
    }

    private void PickRandomOpeningSequence()
    {
        if (dialogueSequenceVariations.Count == 0) return;

        currentOpeningSequence = dialogueSequenceVariations[Random.Range(0, dialogueSequenceVariations.Count)];
        StartCoroutine(PlayDialogueSequence(currentOpeningSequence, true));
    }

    private void PickRandomResultSequence()
    {
        if (resultDialogueSequenceVariations.Count == 0)
        {
            LoadMinigame();
            return;
        }

        currentResultSequence = resultDialogueSequenceVariations[Random.Range(0, resultDialogueSequenceVariations.Count)];
        StartCoroutine(PlayDialogueSequence(currentResultSequence, false));
    }

    private System.Collections.IEnumerator PlayDialogueSequence(DialogueSequence sequence, bool activatesRoulette)
    {
        for (int i = 0; i < sequence.lines.Count; i++)
        {
            DialogueLine line = sequence.lines[i];

            // Set presenter sprite
            if (presenterSpriteRenderer != null)
            {
                presenterSpriteRenderer.sprite = line.presenterSprite != null ? line.presenterSprite : sequence.defaultPresenterSprite;
            }

            // Show dialogue with effect
            yield return ShowDialogueWithEffect(line);

            if (line.isFinalDialogue && activatesRoulette && !hasInitializedSpinner)
            {
                hasInitializedSpinner = true;
                // Start the roulette
                if (minigameSpinner != null)
                {
                    minigameSpinner.InitializeSpinner();
                }
                else
                {
                    Debug.LogWarning("MinigameSpinner not assigned! Using fallback random selection.");
                    // Fallback: pick random minigame and proceed
                    selectedMinigameId = Random.Range(1, 5);
                    HandleSpinComplete(selectedMinigameId);
                }
            }
        }

        // If we're playing result dialogues and done, load the minigame
        if (!activatesRoulette)
        {
            LoadMinigame();
        }
    }

    private System.Collections.IEnumerator ShowDialogueWithEffect(DialogueLine line)
    {
        if (dialogueText == null) yield break;

        // Reset dialogue position and state
        dialogueText.transform.localPosition = dialogueOriginalPosition;
        dialogueText.alpha = 0f;
        dialogueText.text = "";
        dialogueText.transform.localScale = Vector3.one * 0.8f;

        Sequence showSequence = DOTween.Sequence();
        showSequence.Join(dialogueText.DOFade(1f, dialogueRiseDuration * 0.5f).SetEase(Ease.OutQuad));
        showSequence.Join(dialogueText.transform.DOLocalMoveY(dialogueOriginalPosition.y + dialogueRiseAmount, dialogueRiseDuration).SetEase(Ease.OutQuad));
        showSequence.Join(dialogueText.transform.DOScale(Vector3.one, dialogueRiseDuration * 0.5f).SetEase(Ease.OutBack));
        showSequence.Play();

        // Type text letter by letter
        float timer = 0f;
        int charIndex = 0;

        while (charIndex < line.text.Length)
        {
            timer += Time.deltaTime;
            if (timer >= line.typeSpeed)
            {
                timer = 0f;
                charIndex++;
                dialogueText.text = line.text.Substring(0, charIndex);

                // Pop effect for each character
                dialogueText.transform.DOScale(Vector3.one * dialoguePopAmount, 0.05f)
                    .OnComplete(() => dialogueText.transform.DOScale(Vector3.one, 0.05f));
            }
            yield return null;
        }

        yield return new WaitForSeconds(line.displayDuration);

        // Hide dialogue
        Sequence hideSequence = DOTween.Sequence();
        hideSequence.Join(dialogueText.DOFade(0f, dialogueRiseDuration * 0.5f).SetEase(Ease.InQuad));
        hideSequence.Join(dialogueText.transform.DOLocalMoveY(dialogueOriginalPosition.y + dialogueRiseAmount * 1.5f, dialogueRiseDuration).SetEase(Ease.InQuad));
        hideSequence.Play();

        yield return new WaitForSeconds(dialogueRiseDuration);
    }

    private void HandleSpinComplete(int winnerId)
    {
        selectedMinigameId = winnerId;

        // Apply color (use winnerId - 1 as index, since winnerId starts at 1)
        int colorIndex = (winnerId - 1) % resultColors.Length;
        if (resultColors.Length > 0)
        {
            ApplyResultColor(resultColors[colorIndex]);
        }

        // Zoom out camera
        if (mainCamera != null)
        {
            mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.InOutQuad).OnComplete(PickRandomResultSequence);
        }
        else
        {
            PickRandomResultSequence();
        }
    }

    private void StartLightsLoop()
    {
        if (lights == null || lights.Length == 0) return;

        for (int i = 0; i < lights.Length; i++)
        {
            GameObject light = lights[i];
            Vector3 originalPos = light.transform.localPosition;
            float direction = i % 2 == 0 ? 1f : -1f;

            Sequence lightSequence = DOTween.Sequence();
            lightSequence.Append(light.transform.DOLocalMove(originalPos + Vector3.right * lightsMoveAmount * direction, lightsMoveDuration / 2f)
                .SetEase(Ease.InOutSine).SetDelay(i * 0.2f));
            lightSequence.Append(light.transform.DOLocalMove(originalPos + Vector3.left * lightsMoveAmount * direction, lightsMoveDuration)
                .SetEase(Ease.InOutSine));
            lightSequence.Append(light.transform.DOLocalMove(originalPos, lightsMoveDuration / 2f)
                .SetEase(Ease.InOutSine));
            lightSequence.SetLoops(-1, LoopType.Restart);
            lightSequence.Play();
        }
    }

    private void DropRoulette()
    {
        if (roulette == null) return;

        Sequence dropSequence = DOTween.Sequence();
        dropSequence.Append(roulette.transform.DOLocalMove(rouletteOriginalPosition, rouletteDropDuration).SetEase(Ease.InBounce));
        dropSequence.Append(roulette.transform.DOPunchPosition(Vector3.down * rouletteBounceStrength, rouletteBounceDuration, 5).SetEase(Ease.OutElastic));
        dropSequence.Play();
    }

    private void RemoveCover()
    {
        if (rouletteCover == null) return;
        rouletteCover.transform.DOLocalMoveX(coverOriginalPosition.x + coverMoveAmount, coverMoveDuration).SetEase(Ease.OutQuad);
    }

    private void ApplyResultColor(Color color)
    {
        if (resultColorObjects == null) return;

        foreach (var obj in resultColorObjects)
        {
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
            }

            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
            }
        }
    }

    private void LoadMinigame()
    {
        KillAllTweens();
        
        // Save the selected minigame (in case we need it)
        PlayerPrefs.SetInt("SelectedMinigame", selectedMinigameId);
        
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSelectModifier();
        }
        else
        {
            Debug.LogWarning("SceneLoader.Instance is null! Loading directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Select_Modifier");
        }
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
