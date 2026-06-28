using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialoguePanel : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image presenterImage;
    [SerializeField] private TextMeshProUGUI commentText;
    [SerializeField] private RectTransform panelRect;

    [Header("Typewriter")]
    [SerializeField] private float charDelay = 0.03f;

    [Header("Animación slide")]
    [SerializeField] private float slideInDuration = 0.3f;
    [SerializeField] private float slideOutDuration = 0.25f;

    private float panelWidth;
    private Coroutine typewriterCoroutine;
    private Coroutine hideCoroutine;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        panelWidth = panelRect.rect.width;
    }

    public void HideImmediate()
    {
        StopAllCoroutines();
        IsActive = false;
        gameObject.SetActive(false);
    }

    public void Show(string text, Sprite sprite, float duration)
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        presenterImage.sprite = sprite;
        commentText.text = "";
        gameObject.SetActive(true);
        IsActive = true;

        typewriterCoroutine = StartCoroutine(TypewriterRoutine(text));
        hideCoroutine = StartCoroutine(ShowHideRoutine(duration));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        commentText.text = "";
        foreach (char c in text)
        {
            commentText.text += c;
            yield return new WaitForSeconds(charDelay);
        }
    }

    private IEnumerator ShowHideRoutine(float displayDuration)
    {
        // Slide in desde la izquierda
        yield return StartCoroutine(Slide(offscreen: true, onscreen: false, slideInDuration));

        // Esperar mientras se muestra
        yield return new WaitForSeconds(displayDuration);

        // Slide out hacia la izquierda
        yield return StartCoroutine(Slide(offscreen: false, onscreen: true, slideOutDuration));

        IsActive = false;
        gameObject.SetActive(false);
    }

    // Si saliendo=true, va de posicion visible hacia afuera; si false, al reves
    private IEnumerator Slide(bool offscreen, bool onscreen, float duration)
    {
        Vector2 hidden = new Vector2(-panelWidth, panelRect.anchoredPosition.y);
        Vector2 visible = new Vector2(0f, panelRect.anchoredPosition.y);

        Vector2 from = onscreen ? visible : hidden;
        Vector2 to = onscreen ? hidden : visible;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            panelRect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        panelRect.anchoredPosition = to;
    }
}