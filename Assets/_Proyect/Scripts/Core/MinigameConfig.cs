using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// Enum centralizado: reemplaza los IDs numéricos mágicos en todo el proyecto.
// ─────────────────────────────────────────────────────────────────────────────
public enum MinigameID
{
    None = 0,
    DodgeDisk = 1,
    KingOfHill = 2,
    DNA = 3,
    Space = 4,
    ChaseRun = 5,
}

// ─────────────────────────────────────────────────────────────────────────────
// Datos de un modificador: nombre, valor de enum y diálogos del presentador
// cuando ese modificador es elegido.
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class ModifierConfig
{
    [Tooltip("Nombre visible en la ruleta de modificadores.")]
    public string displayName = "Modificador";

    [Tooltip("Valor entero que se pasa a ModifierManager (mapea al enum del minijuego).")]
    public int enumValue = 1;

    [Tooltip("Diálogos que el presentador dice cuando ESTE modificador es elegido. " +
             "Se elige uno al azar si hay más de uno.")]
    public DialogueSequence[] resultDialogues;
}

// ─────────────────────────────────────────────────────────────────────────────
// ScriptableObject: un asset por minijuego.
// Crear en: clic derecho → Create → Minigame → MinigameConfig
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "MinigameConfig", menuName = "Minigame/MinigameConfig")]
public class MinigameConfig : ScriptableObject
{
    [Header("Identificación")]
    public MinigameID id;
    public string displayName = "Minijuego";

    [Header("Visual")]
    [Tooltip("Color de resultado asociado a este minijuego en la ruleta.")]
    public Color resultColor = Color.white;

    [Header("Modificadores (mismo orden que los sectores de la ruleta)")]
    [Tooltip("Exactamente 3 modificadores.")]
    public ModifierConfig[] modifiers = new ModifierConfig[3];
}