using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class AutoPanelSize : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;

    [Header("Limites del panel")]
    [SerializeField] private float minHeight = 80f;
    [SerializeField] private float maxHeight = 300f;

    [Header("Padding interno")]
    [SerializeField] private float paddingTop = 16f;
    [SerializeField] private float paddingBottom = 16f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (targetText == null) return;

        float textHeight = targetText.preferredHeight;
        float targetHeight = Mathf.Clamp(textHeight + paddingTop + paddingBottom, minHeight, maxHeight);

        if (!Mathf.Approximately(rectTransform.sizeDelta.y, targetHeight))
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, targetHeight);
    }
}