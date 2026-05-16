using UnityEngine;

public class DebugMinigameSelector : MonoBehaviour
{
    [Header("Objetos de la escena")]
    [SerializeField] private GameObject spinnerRoot;   // la ruleta normal, se oculta en debug
    [SerializeField] private GameObject debugPanel;    // panel con los 5 botones de minijuegos

    private void Start()
    {
        bool debug = DebugManager.IsDebugMode;
        spinnerRoot.SetActive(!debug);
        debugPanel.SetActive(debug);
    }

    // cada botón del panel llama esto con su id (1,2,3,4,5)
    public void SelectMinigame(int id)
    {
        DebugManager.SetMinigame(id);
        SceneLoader.Instance.LoadSelectModifier();
    }
}