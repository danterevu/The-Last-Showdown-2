using TMPro;
using UnityEngine;

public class MinigameUiManager : MonoBehaviour
{
    [Header("Textos del Minijuego")]
    [SerializeField] private TextMeshProUGUI minigameTitleText;
    [SerializeField] private TextMeshProUGUI minigameDescText;

    [Header("Textos del Modificador")]
    [SerializeField] private TextMeshProUGUI modifierTitleText;
    [SerializeField] private TextMeshProUGUI modifierDescText;

    // ── Datos de minijuegos ──────────────────────────────────
    private static readonly string[] minigameTitles = new string[]
    {
        "",           // índice 0 vacío
        "DODGE DISK", // 1
        "KING OF THE HILL", // 2
        "",           // 3 (si no tenés)
        "",           // 4
        "SPACE BATTLE" // 5
    };

    private static readonly string[] minigameDescs = new string[]
    {
        "",
        "¡Esquivá el disco y sobreviví!",
        "¡Dominá la zona y acumulá puntos!",
        "",
        "",
        "¡Eliminá a tu rival en el espacio!"
    };

    // ── Datos de modificadores ───────────────────────────────
    // [minigameId][modIndex]
    private static readonly string[][] modifierTitles = new string[][]
    {
        new string[] {},                                              // índice 0
        new string[] { "BONUS KILL", "BONUS DEATH", "BONUS WINNER" }, // 1: DodgeDisk
        new string[] { "COMEBACK x3", "BONUS HARDPOINT", "POINT BLEED" }, // 2: KOH
        new string[] {},                                              // 3
        new string[] {},                                              // 4
        new string[] { "GOLDEN KILL", "COMBO ROUNDS", "SIN MODIFICADOR" } // 5: Space
    };

    private static readonly string[][] modifierDescs = new string[][]
    {
        new string[] {},
        new string[] {
            "Matar con un power up da puntos extra",
            "Morir suma puntos al rival",
            "El ganador recibe un bonus al final"
        },
        new string[] {
            "El que va perdiendo tiene multiplicador x3",
            "Más tiempo en zona = más puntos",
            "Fuera de la zona perdés puntos por segundo"
        },
        new string[] {},
        new string[] {},
        new string[] {
            "La primera kill de la ronda vale el triple",
            "Ganar rondas seguidas da multiplicador",
            "Esta ronda no tiene modificador"
        }
    };

    private void Start()
    {
        int minigameId = PlayerPrefs.GetInt("SelectedMinigame", 1);
        int modIndex = PlayerPrefs.GetInt("SelectedModifier", 0);

        SetMinigameTexts(minigameId);
        SetModifierTexts(minigameId, modIndex);
    }

    private void SetMinigameTexts(int id)
    {
        if (minigameTitleText != null)
            minigameTitleText.text = (id < minigameTitles.Length)
                ? minigameTitles[id] : "MINIJUEGO";

        if (minigameDescText != null)
            minigameDescText.text = (id < minigameDescs.Length)
                ? minigameDescs[id] : "";
    }

    private void SetModifierTexts(int minigameId, int modIndex)
    {
        string title = "SIN MODIFICADOR";
        string desc = "";

        if (minigameId < modifierTitles.Length &&
            modIndex < modifierTitles[minigameId].Length)
        {
            title = modifierTitles[minigameId][modIndex];
            desc = modifierDescs[minigameId][modIndex];
        }

        if (modifierTitleText != null) modifierTitleText.text = title;
        if (modifierDescText != null) modifierDescText.text = desc;
    }
}
