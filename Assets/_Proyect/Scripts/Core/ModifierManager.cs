using UnityEngine;

public class ModifierManager : MonoBehaviour
{
    public static ModifierManager Instance;

    [Header("Modificador activo")]
    public KOHModifier activeKOHModifier = KOHModifier.None;
    public DodgeDiskModifier activeDDModifier = DodgeDiskModifier.None;

    [Header("Valores configurables")]
    public float comebackMultiplier = 3f;       // x3 para el que va perdiendo
    public int pointBleedAmount = 1;            // puntos que se pierden por segundo
    public int powerUpKillBonusPoints = 15;     // bonus por matar con power up
    public int deathGivesPoints = 10;           // puntos que le das al rival al morir
    public int winnerBonusPoints = 50;          // bonus al ganador al final

    // modificadores disponibles por minijuego
    public enum MinigameType { DodgeDisk, KingOfHill }

    // modificadores King of the Hill
    public enum KOHModifier
    {
        None,
        ComebackMultiplier,   // x3 al que va perdiendo
        ProgressiveHardpoint, // x1?x2?x3?x4 por tiempo en zona
        PointBleed            // -1/seg fuera de la zona
    }

    // modificadores Dodge Disk
    public enum DodgeDiskModifier
    {
        None,
        PowerUpKillBonus,   // matas con power up = +X puntos
        DeathGivesPoints,   // morir le da puntos al rival
        WinnerBonus         // el ganador se lleva bonus extra
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // llamado desde la ruleta de modificadores antes de cada minijuego
    public void SetKOHModifier(KOHModifier mod)
    {
        activeKOHModifier = mod;
        ApplyKOHModifier();
    }

    public void SetDodgeDiskModifier(DodgeDiskModifier mod)
    {
        activeDDModifier = mod;
    }

    // el ComebackMultiplier se recalcula cada vez que se suman puntos
    // lo chequea KingOfHill en HandleHardPointPoints
    public void ApplyKOHModifier()
    {
        // resetear multiplicadores primero
        GameManager.Instance.player1Multiplier = 1f;
        GameManager.Instance.player2Multiplier = 1f;

        if (activeKOHModifier == KOHModifier.ComebackMultiplier)
            RecalculateComebackMultiplier();
    }

    // se llama cada vez que cambian los puntos en KOH
    public void RecalculateComebackMultiplier()
    {
        if (activeKOHModifier != KOHModifier.ComebackMultiplier) return;

        int p1 = GameManager.Instance.player1RoundPoints;
        int p2 = GameManager.Instance.player2RoundPoints;

        // el que va perdiendo por 20+ puntos recibe el x3
        if (p1 < p2 - 20)
        {
            GameManager.Instance.player1Multiplier = comebackMultiplier;
            GameManager.Instance.player2Multiplier = 1f;
        }
        else if (p2 < p1 - 20)
        {
            GameManager.Instance.player2Multiplier = comebackMultiplier;
            GameManager.Instance.player1Multiplier = 1f;
        }
        else
        {
            GameManager.Instance.player1Multiplier = 1f;
            GameManager.Instance.player2Multiplier = 1f;
        }
    }

    public void ResetModifiers()
    {
        activeKOHModifier = KOHModifier.None;
        activeDDModifier = DodgeDiskModifier.None;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.player1Multiplier = 1f;
            GameManager.Instance.player2Multiplier = 1f;
        }
    }
}