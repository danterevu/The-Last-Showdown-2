using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class RouletteShowSequence : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float initialOrthoSize = 3f;
    [SerializeField] private float zoomOutOrthoSize = 8f;
    [SerializeField] private float zoomToRouletteOrthoSize = 5f;
    [SerializeField] private float zoomDuration = 1f;

    [Header("Center Sprite")]
    [SerializeField] private RectTransform centerSprite;
    [SerializeField] private float centerSpritePopDuration = 0.5f;

    [Header("Lights")]
    [SerializeField] private RectTransform[] lights;
    [SerializeField] private float lightsMoveAmount = 100f;
    [SerializeField] private float lightsMoveDuration = 2f;

    [Header("Presenter Dialogues")]
    [SerializeField] private TextMeshProUGUI presenterDialogueText;
    [SerializeField] private float dialogueFadeDuration = 0.5f;
    [SerializeField] private float dialogueDisplayDuration = 2f;
    [SerializeField] private string[] openingDialogues;
    [SerializeField] private string[] resultDialogues;

    [Header("Roulette")]
    [SerializeField] private RectTransform rouletteTransform;
    [SerializeField] private float rouletteDropHeight = 1000f;
    [SerializeField] private float rouletteDropDuration = 1f;
    [SerializeField] private float rouletteBounceStrength = 50f;
    [SerializeField] private int rouletteBounceVibrato = 5;
    [SerializeField] private float rouletteBounceDuration = 1f;

    [Header("Roulette Cover")]
    [SerializeField] private RectTransform rouletteCover;
    [SerializeField] private float coverMoveAmount = 500f;
    [SerializeField] private float coverMoveDuration = 0.5f;

    [Header("Result Objects")]
    [SerializeField] private GameObject[] resultColorObjects;
    [SerializeField] private Color[] resultColors;
    [SerializeField] private float colorChangeDuration = 0.5f;

    [Header("Minigame Selection")]
    [SerializeField] private int[] minigameIds;
    [SerializeField] private float resultDelay = 1f;
    [SerializeField] private float finalDialogueDelay = 2f;

    private Sequence mainSequence;
    private Vector3 rouletteOriginalPosition;
    private Vector3 coverOriginalPosition;
    private int selectedMinigameId;

    void Awake()
    {
        if (rouletteTransform != null)
        {
            rouletteOriginalPosition = rouletteTransform.localPosition;
            rouletteTransform.localPosition = new Vector3(rouletteOriginalPosition.x, rouletteOriginalPosition.y + rouletteDropHeight, rouletteOriginalPosition.z);
        }

        if (rouletteCover != null)
        {
            coverOriginalPosition = rouletteCover.localPosition;
        }

        if (presenterDialogueText != null)
        {
            presenterDialogueText.alpha = 0f;
        }

        if (centerSprite != null)
        {
            centerSprite.localScale = Vector3.zero;
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
            mainSequence.Join(centerSprite.DOScale(Vector3.one, centerSpritePopDuration).SetEase(Ease.OutElastic));
        }

        // 3. Start lights loop
        StartLightsLoop();

        // 4. Opening dialogues
        float dialogueTime = mainSequence.Duration();
        for (int i = 0; i < openingDialogues.Length; i++)
        {
            int index = i;
            mainSequence.InsertCallback(dialogueTime, () => ShowDialogue(openingDialogues[index]));
            dialogueTime += dialogueDisplayDuration + dialogueFadeDuration * 2;
        }

        // 5. Drop roulette with bounce
        mainSequence.InsertCallback(dialogueTime, DropRoulette);
        dialogueTime += rouletteDropDuration + rouletteBounceDuration;

        // 6. Remove cover
        mainSequence.InsertCallback(dialogueTime, RemoveCover);
        dialogueTime += coverMoveDuration;

        // 7. Zoom to roulette
        if (mainCamera != null)
        {
            mainSequence.Insert(dialogueTime, mainCamera.DOOrthoSize(zoomToRouletteOrthoSize, zoomDuration).SetEase(Ease.InOutQuad));
        }
        dialogueTime += zoomDuration;

        // 8. Spin roulette and get result
        mainSequence.InsertCallback(dialogueTime, SpinAndGetResult);

        mainSequence.Play();
    }

    private void StartLightsLoop()
    {
        if (lights == null || lights.Length == 0) return;

        foreach (var light in lights)
        {
            Vector3 originalPos = light.localPosition;
            Sequence lightSequence = DOTween.Sequence();
            lightSequence.Append(light.DOLocalMoveX(originalPos.x + lightsMoveAmount, lightsMoveDuration).SetEase(Ease.InOutSine));
            lightSequence.Append(light.DOLocalMoveX(originalPos.x - lightsMoveAmount, lightsMoveDuration).SetEase(Ease.InOutSine));
            lightSequence.SetLoops(-1, LoopType.Yoyo);
            lightSequence.Play();
        }
    }

    private void ShowDialogue(string dialogue)
    {
        if (presenterDialogueText == null) return;

        Sequence dialogueSequence = DOTween.Sequence();
        presenterDialogueText.text = dialogue;
        dialogueSequence.Append(presenterDialogueText.DOFade(1f, dialogueFadeDuration).SetEase(Ease.InQuad));
        dialogueSequence.AppendInterval(dialogueDisplayDuration);
        dialogueSequence.Append(presenterDialogueText.DOFade(0f, dialogueFadeDuration).SetEase(Ease.OutQuad));
        dialogueSequence.Play();
    }

    private void DropRoulette()
    {
        if (rouletteTransform == null) return;

        Sequence dropSequence = DOTween.Sequence();
        dropSequence.Append(rouletteTransform.DOLocalMove(rouletteOriginalPosition, rouletteDropDuration).SetEase(Ease.InBounce));
        dropSequence.Append(rouletteTransform.DOPunchPosition(Vector3.down * rouletteBounceStrength, rouletteBounceDuration, rouletteBounceVibrato).SetEase(Ease.OutElastic));
        dropSequence.Play();
    }

    private void RemoveCover()
    {
        if (rouletteCover == null) return;
        rouletteCover.DOLocalMoveX(coverOriginalPosition.x + coverMoveAmount, coverMoveDuration).SetEase(Ease.OutQuad);
    }

    private void SpinAndGetResult()
    {
        // Randomly select a result
        int resultIndex = Random.Range(0, resultColors.Length);
        selectedMinigameId = minigameIds[resultIndex % minigameIds.Length];

        // Apply color to result objects
        ApplyResultColor(resultColors[resultIndex]);

        // Zoom out camera
        if (mainCamera != null)
        {
            mainCamera.DOOrthoSize(zoomOutOrthoSize, zoomDuration).SetEase(Ease.InOutQuad).OnComplete(ShowResultDialogues);
        }
        else
        {
            ShowResultDialogues();
        }
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

            Image image = obj.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.DOColor(color, colorChangeDuration).SetEase(Ease.InOutQuad);
            }
        }
    }

    private void ShowResultDialogues()
    {
        float dialogueTime = 0f;
        for (int i = 0; i < resultDialogues.Length; i++)
        {
            int index = i;
            DOVirtual.DelayedCall(dialogueTime, () => ShowDialogue(resultDialogues[index]));
            dialogueTime += dialogueDisplayDuration + dialogueFadeDuration * 2;
        }

        DOVirtual.DelayedCall(dialogueTime + finalDialogueDelay, LoadMinigame);
    }

    private void LoadMinigame()
    {
        KillAllTweens();
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMinigame(selectedMinigameId);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Minigame_" + selectedMinigameId);
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
