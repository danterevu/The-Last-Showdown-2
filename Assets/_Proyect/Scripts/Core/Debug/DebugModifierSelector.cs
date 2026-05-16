using UnityEngine;
using TMPro;

public class DebugModifierSelector : MonoBehaviour
{
    [Header("Objetos de la escena")]
    [SerializeField] private GameObject spinnerRoot;
    [SerializeField] private GameObject debugPanel;

    [Header("Etiquetas de los 3 botones del panel")]
    [SerializeField] private TextMeshProUGUI buttonLabel0;
    [SerializeField] private TextMeshProUGUI buttonLabel1;
    [SerializeField] private TextMeshProUGUI buttonLabel2;

    // nombres y descripciones por minijuego [minijuego][modificador]
    private static readonly string[,] Labels =
    {
        // MG1 - DodgeDisk
        { "Muerte da puntos", "Kill tras power up", "Bonus al ganador" },
        // MG2 - King of Hill
        { "Point Bleed", "Comeback Multiplier", "Progressive Hardpoint" },
        // MG3 - (adaptar a sus modificadores)
        { "Modificador 3A", "Modificador 3B", "Modificador 3C" },
        // MG4 - (adaptar a sus modificadores)
        { "Modificador 4A", "Modificador 4B", "Modificador 4C" },
        // MG5 - Space
        { "Modificador 5A", "Modificador 5B", "Modificador 5C" },
    };

    private void Start()
    {
        bool debug = DebugManager.IsDebugMode;
        spinnerRoot.SetActive(!debug);
        debugPanel.SetActive(debug);

        if (debug) SetupLabels();
    }

    private void SetupLabels()
    {
        int mg = DebugManager.SelectedMinigame - 1; // índice 0-based
        if (mg < 0 || mg >= 5) return;

        if (buttonLabel0 != null) buttonLabel0.text = Labels[mg, 0];
        if (buttonLabel1 != null) buttonLabel1.text = Labels[mg, 1];
        if (buttonLabel2 != null) buttonLabel2.text = Labels[mg, 2];
    }

    // cada botón llama esto con índice 0, 1 o 2
    public void SelectModifier(int index)
    {
        int mg = DebugManager.SelectedMinigame;
        ApplyModifier(mg, index);
        SceneLoader.Instance.LoadMinigame(mg);
    }

    private void ApplyModifier(int minigame, int index)
    {
        if (ModifierManager.Instance == null) return;

        switch (minigame)
        {
            case 1: // DodgeDisk
                ModifierManager.Instance.activeDDModifier = index switch
                {
                    0 => ModifierManager.DodgeDiskModifier.DeathGivesPoints,
                    1 => ModifierManager.DodgeDiskModifier.PowerUpKillBonus,
                    _ => ModifierManager.DodgeDiskModifier.WinnerBonus,
                };
                break;

            case 2: // King of Hill
                ModifierManager.Instance.activeKOHModifier = index switch
                {
                    0 => ModifierManager.KOHModifier.PointBleed,
                    1 => ModifierManager.KOHModifier.ComebackMultiplier,
                    _ => ModifierManager.KOHModifier.ProgressiveHardpoint,
                };
                break;

                // case 3, 4, 5: agregar cuando tengas los enums de esos minijuegos
        }
    }
}