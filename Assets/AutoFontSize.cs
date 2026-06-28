using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class AutoFontSize : MonoBehaviour
{
    [SerializeField] private float minFontSize = 10f;
    [SerializeField] private float maxFontSize = 36f;

    private TextMeshProUGUI tmp;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minFontSize;
        tmp.fontSizeMax = maxFontSize;
    }
}