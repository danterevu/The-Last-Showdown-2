using UnityEngine;

public class ModifierManager : MonoBehaviour
{
    public static ModifierManager Instance;

    [Header("Modificador activo")]
    public KOHModifier activeKOHModifier = KOHModifier.None;
    public DodgeDiskModifier activeDDModifier = DodgeDiskModifier.None;
    public SpaceModifier activeSpaceModifier = SpaceModifier.None;
    public MutantDNAModifier activeDNAModifier = MutantDNAModifier.None;

    [Header("Valores configurables")]
    public float comebackMultiplier = 3f;
    public int pointBleedAmount = 1;
    public int powerUpKillBonusPoints = 15;
    public int deathGivesPoints = 10;
    public int winnerBonusPoints = 50;

    [Header("Space - Golden Kill")]
    public float goldenKillMultiplier = 3f;     // multiplicador para la primera kill

    [Header("Space - Combo Rounds")]
    public int comboRoundsBonusPerStreak = 15;  // puntos extra por ronda consecutiva ganada

    // --- estado interno Golden Kill ---
    // true = la primera kill del round todavía no ocurrió
    private bool goldenKillAvailable = false;

    // --- estado interno Combo Rounds ---
    // cuántas rondas consecutivas lleva ganando cada jugador
    private int player1WinStreak = 0;
    private int player2WinStreak = 0;

    // modificadores disponibles por minijuego
    public enum MinigameType { DodgeDisk, KingOfHill, Space }

    // modificadores King of the Hill
    public enum KOHModifier
    {
        None,
        ComebackMultiplier,   // x3 al que va perdiendo
        ProgressiveHardpoint, // multiplica los puntos por tiempo en zona
        PointBleed            // pierde puntos por segundo fuera de la zona
    }

    // modificadores Dodge Disk
    public enum DodgeDiskModifier
    {
        None,
        PowerUpKillBonus,   // matar con power up da puntos extra
        DeathGivesPoints,   // morir le da puntos al rival
        WinnerBonus         // el ganador recibe un bonus al final
    }

    public enum MutantDNAModifier
    {
        None,
        CrateStun,      // ser aturdido con caja resta 5 puntos
        PowerUpHit,     // afectar al rival con power up da 5 puntos
        ThrowBonus      // depositar lanzando da 50 pts en vez de 30
    }

    // valores configurables
    public int crateStunPenalty = 5;
    public int powerUpHitBonus = 5;


    // modificadores Space (Minigame_4)
    public enum SpaceModifier
    {
        None,
        GoldenKill,     // la primera kill de la ronda vale x3
        ComboRounds     // cada ronda consecutiva ganada suma +15 pts extra
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------
    //  SETTERS de modificadores
    // -------------------------------------------------------

    public void SetKOHModifier(KOHModifier mod)
    {
        activeKOHModifier = mod;
        ApplyKOHModifier();
    }

    public void SetDodgeDiskModifier(DodgeDiskModifier mod)
    {
        activeDDModifier = mod;
    }

    public void SetDNAModifier(MutantDNAModifier mod)
    {
        activeDNAModifier = mod;
    }

    // Llamado desde la ruleta antes de cargar el Minigame_5
    public void SetSpaceModifier(SpaceModifier mod)
    {
        activeSpaceModifier = mod;

        // Golden Kill: habilitar el flag para la primera kill del round
        goldenKillAvailable = (mod == SpaceModifier.GoldenKill);
    }

    // -------------------------------------------------------
    //  GOLDEN KILL  (consultado desde Projectile.cs)
    // -------------------------------------------------------

    // Devuelve true si la primera kill todavía no ocurrió Y el mod está activo
    public bool IsGoldenKillAvailable()
    {
        return activeSpaceModifier == SpaceModifier.GoldenKill && goldenKillAvailable;
    }

    // Llamado por Projectile.cs la primera vez que hay una kill con el mod activo
    public void ConsumeGoldenKill()
    {
        goldenKillAvailable = false;
    }

    // Permite resetear el flag si el minijuego hace un respawn mid-game
    // (opcional: llamarlo desde SpaceMinigame.StartMinigame si querés una
    //  "primera kill por vida" en lugar de "primera kill del round")
    public void ResetGoldenKill()
    {
        if (activeSpaceModifier == SpaceModifier.GoldenKill)
            goldenKillAvailable = true;
    }

    // -------------------------------------------------------
    //  COMBO ROUNDS  (llamado desde SpaceMinigame.EndMinigame)
    // -------------------------------------------------------

    // Registra quién ganó la ronda y aplica el bonus si hay racha.
    // Llamar ANTES de GameManager.FinishMinigame() para que el bonus
    // quede incluido en los puntos de ronda que se transfieren al global.
    //
    // winner: 1 o 2. Pasa 0 si hubo empate (no suma racha a nadie).
    public void RegisterSpaceRoundWinner(int winner)
    {
        if (activeSpaceModifier != SpaceModifier.ComboRounds) return;
        if (winner == 0) return; // empate: se resetean ambas rachas

        if (winner == 1)
        {
            player1WinStreak++;
            player2WinStreak = 0;

            // La racha empieza a contar desde la segunda victoria consecutiva
            int bonusSteps = player1WinStreak - 1;
            if (bonusSteps > 0)
            {
                int bonus = bonusSteps * comboRoundsBonusPerStreak;
                GameManager.Instance.AddPoints(1, bonus);
                Debug.Log($"[ComboRounds] Jugador 1 lleva {player1WinStreak} rondas seguidas  +{bonus} pts");
            }
        }
        else
        {
            player2WinStreak++;
            player1WinStreak = 0;

            int bonusSteps = player2WinStreak - 1;
            if (bonusSteps > 0)
            {
                int bonus = bonusSteps * comboRoundsBonusPerStreak;
                GameManager.Instance.AddPoints(2, bonus);
                Debug.Log($"[ComboRounds] Jugador 2 lleva {player2WinStreak} rondas seguidas  +{bonus} pts");
            }
        }
    }

    // -------------------------------------------------------
    //  KOH - lógica existente (sin cambios)
    // -------------------------------------------------------

    public void ApplyKOHModifier()
    {
        GameManager.Instance.player1Multiplier = 1f;
        GameManager.Instance.player2Multiplier = 1f;

        if (activeKOHModifier == KOHModifier.ComebackMultiplier)
            RecalculateComebackMultiplier();
    }

    public void RecalculateComebackMultiplier()
    {
        if (activeKOHModifier != KOHModifier.ComebackMultiplier) return;

        int p1 = GameManager.Instance.player1RoundPoints;
        int p2 = GameManager.Instance.player2RoundPoints;

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

    // -------------------------------------------------------
    //  RESET GLOBAL
    // -------------------------------------------------------

    public void ResetModifiers()
    {
        activeKOHModifier = KOHModifier.None;
        activeDDModifier = DodgeDiskModifier.None;
        activeDNAModifier = MutantDNAModifier.None;
        activeSpaceModifier = SpaceModifier.None;
        goldenKillAvailable = false;
        player1WinStreak = 0;
        player2WinStreak = 0;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.player1Multiplier = 1f;
            GameManager.Instance.player2Multiplier = 1f;
        }
    }
}
