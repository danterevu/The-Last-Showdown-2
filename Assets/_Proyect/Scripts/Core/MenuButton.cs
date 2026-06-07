using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Button Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private float jellyDuration = 0.5f;
    [SerializeField] private float jellyStrength = 0.1f;
    [SerializeField] private int jellyVibrato = 10;
    [SerializeField] private float jellyElasticity = 1f;

    [Header("Flash Settings")]
    [SerializeField] private CanvasGroup flashCanvasGroup;
    [SerializeField] private float flashDuration = 0.1f;

    private Vector3 originalScale;
    private RectTransform rectTransform;
    private Tweener hoverTweener;
    private Tweener jellyTweener;
    private Tweener flashTweener;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    // ─── Eventos de pointer (mouse) ───────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData) => DoHoverEnter();
    public void OnPointerExit(PointerEventData eventData) => DoHoverExit();
    public void OnPointerClick(PointerEventData eventData) => DoClick();

    // ─── API pública para que UINavigator los llame directamente ──────
    public void DoHoverEnter()
    {
        hoverTweener?.Kill();
        hoverTweener = rectTransform
            .DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // SetUpdate(true) = funciona aunque Time.timeScale = 0 (pausa)
    }

    public void DoHoverExit()
    {
        hoverTweener?.Kill();
        hoverTweener = rectTransform
            .DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void DoClick()
    {
        PlayJellyEffect();
        PlayFlash();
    }

    // ─── Efectos internos ─────────────────────────────────────────────
    private void PlayJellyEffect()
    {
        jellyTweener?.Kill();
        jellyTweener = rectTransform
            .DOPunchScale(Vector3.one * jellyStrength, jellyDuration, jellyVibrato, jellyElasticity)
            .SetEase(Ease.OutElastic)
            .SetUpdate(true);
    }

    private void PlayFlash()
    {
        if (flashCanvasGroup == null) return;
        flashTweener?.Kill();
        flashCanvasGroup.alpha = 1f;
        flashTweener = flashCanvasGroup
            .DOFade(0f, flashDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    void OnDestroy()
    {
        hoverTweener?.Kill();
        jellyTweener?.Kill();
        flashTweener?.Kill();
    }
}