using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugMenuButton : MonoBehaviour
{
    [Header(" Marcar solo en la build para el profe de QA")]
    [SerializeField] private bool buttonVisible = false;

    [Header("Referencias")]
    [SerializeField] private CanvasGroup buttonCanvasGroup;   // CanvasGroup del boton
    [SerializeField] private TextMeshProUGUI buttonLabel;     // texto del boton
    [SerializeField] private GameObject debugIndicator;       // panel "DEBUG MODE ON" visible en el menu

    private void Start()
    {
        // si buttonVisible es false, el boton es transparente pero igual clickeable
        buttonCanvasGroup.alpha = buttonVisible ? 1f : 0f;
        buttonCanvasGroup.interactable = true;
        buttonCanvasGroup.blocksRaycasts = true;

        RefreshUI();
    }

    // llamado desde el Button.OnClick() en el Inspector
    public void OnClick()
    {
        DebugManager.Toggle();
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool on = DebugManager.IsDebugMode;

        if (buttonLabel != null)
            buttonLabel.text = on ? "DEBUG: ON" : "DEBUG: OFF";

        if (debugIndicator != null)
            debugIndicator.SetActive(on);
    }
}