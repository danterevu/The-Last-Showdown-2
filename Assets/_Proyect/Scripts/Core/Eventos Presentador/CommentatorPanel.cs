using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CommentatorPanel : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI commentText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Ícono fijo de este comentarista")]
    [SerializeField] private Sprite defaultIcon;

    [Header("Animación")]
    [SerializeField] private float slideDistance = 100f;
    [SerializeField] private float animDuration = 0.35f;
    [SerializeField] private float displayDuration = 4f;

    public bool IsActive { get; private set; }

    private Vector2 shownPosition;
    private Coroutine showCoroutine;

    private void Awake()
    {
        // guardamos la posición tal cual la pusiste en la escena
        shownPosition = panelRect.anchoredPosition;
    }

    public void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        panelRect.anchoredPosition = shownPosition + Vector2.down * slideDistance;
        IsActive = false;
    }

    public void Show(string text, float duration = -1f)
    {
        if (showCoroutine != null) StopCoroutine(showCoroutine);
        float dur = duration > 0f ? duration : displayDuration;
        showCoroutine = StartCoroutine(ShowRoutine(text, dur));
    }

    private IEnumerator ShowRoutine(string text, float duration)
    {
        IsActive = true;

        commentText.text = text;
        if (iconImage != null && defaultIcon != null)
            iconImage.sprite = defaultIcon;

        Vector2 hiddenPos = shownPosition + Vector2.down * slideDistance;
        panelRect.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0f;

        // slide in + fade in
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            panelRect.anchoredPosition = Vector2.Lerp(hiddenPos, shownPosition, t);
            canvasGroup.alpha = t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        panelRect.anchoredPosition = shownPosition;
        canvasGroup.alpha = 1f;

        // esperar
        yield return new WaitForSeconds(duration);

        // slide out + fade out
        elapsed = 0f;
        while (elapsed < animDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            panelRect.anchoredPosition = Vector2.Lerp(shownPosition, hiddenPos, t);
            canvasGroup.alpha = 1f - t;
            elapsed += Time.deltaTime;
            yield return null;
        }

        HideImmediate();
        showCoroutine = null;
    }
}