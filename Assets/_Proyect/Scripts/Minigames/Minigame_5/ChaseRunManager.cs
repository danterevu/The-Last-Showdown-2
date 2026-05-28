using UnityEngine;
using System.Collections;

public class ChaseRunManager : MonoBehaviour
{
    public static ChaseRunManager Instance { get; private set; }

    public enum RunPhase { PhaseY, PhaseX }
    public RunPhase CurrentPhase { get; private set; } = RunPhase.PhaseY;

    // ── Referencias ──────────────────────────────────────────────────────────

    [Header("Jugadores")]
    [SerializeField] private ChaseRunPlayerController player1;
    [SerializeField] private ChaseRunPlayerController player2;

    [Header("Cámara")]
    [SerializeField] private ChaseRunCamera chaseCamera;

    [Header("Power Ups")]
    [SerializeField] private ChaseRunPowerUpSpawner powerUpSpawner;

    [Header("UI / Flash")]
    [SerializeField] private UnityEngine.UI.Image flashImage;
    [SerializeField] private float flashDuration = 0.6f;

    [Header("Scoring")]
    [Tooltip("Puntos por cada intervalo de supervivencia")]
    [SerializeField] private int pointsPerSurvivalInterval = 2;
    [SerializeField] private float survivalInterval = 3f;
    [Tooltip("Bonus al llegar a la meta")]
    [SerializeField] private int goalBonusPoints = 50;

    // ── Estado interno ────────────────────────────────────────────────────────

    private bool gameRunning = false;
    private bool player1Finished = false;
    private bool player2Finished = false;
    private bool endingGame = false;

    private float survivalTimer1 = 0f;
    private float survivalTimer2 = 0f;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Asegurar GameManager en escena standalone (testing)
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        StartMinigame();
    }

    private void StartMinigame()
    {
        CurrentPhase = RunPhase.PhaseY;
        player1Finished = false;
        player2Finished = false;
        endingGame = false;
        gameRunning = true;

        player1.Initialize(this);
        player2.Initialize(this);

        chaseCamera.SetPhase(RunPhase.PhaseY);
        powerUpSpawner?.SetPhase(RunPhase.PhaseY);
    }

    private void Update()
    {
        if (!gameRunning) return;

        // Puntos de supervivencia — solo a los que siguen vivos en carrera
        if (!player1Finished)
        {
            survivalTimer1 += Time.deltaTime;
            if (survivalTimer1 >= survivalInterval)
            {
                survivalTimer1 = 0f;
                GameManager.Instance?.AddPoints(1, pointsPerSurvivalInterval);
            }
        }

        if (!player2Finished)
        {
            survivalTimer2 += Time.deltaTime;
            if (survivalTimer2 >= survivalInterval)
            {
                survivalTimer2 = 0f;
                GameManager.Instance?.AddPoints(2, pointsPerSurvivalInterval);
            }
        }
    }

    // ── Cambio de fase ────────────────────────────────────────────────────────

    /// <summary>Llamado por TriggerZone cuando el runner pasa por el trigger.</summary>
    public void TriggerPhaseChange()
    {
        if (CurrentPhase == RunPhase.PhaseX) return;
        StartCoroutine(DoPhaseChange());
    }

    private IEnumerator DoPhaseChange()
    {
        gameRunning = false;

        yield return StartCoroutine(DoFlash());

        CurrentPhase = RunPhase.PhaseX;
        chaseCamera.SetPhase(RunPhase.PhaseX);
        powerUpSpawner?.SetPhase(RunPhase.PhaseX);

        player1.OnPhaseChanged(RunPhase.PhaseX);
        player2.OnPhaseChanged(RunPhase.PhaseX);

        gameRunning = true;
    }

    // ── Meta ──────────────────────────────────────────────────────────────────

    /// <summary>Llamado por GoalTrigger cuando un jugador toca la meta.</summary>
    public void PlayerReachedGoal(int playerNumber)
    {
        if (playerNumber == 1 && !player1Finished)
        {
            player1Finished = true;
            GameManager.Instance?.AddPoints(1, goalBonusPoints);
        }
        else if (playerNumber == 2 && !player2Finished)
        {
            player2Finished = true;
            GameManager.Instance?.AddPoints(2, goalBonusPoints);
        }

        if (endingGame) return;

        if (player1Finished && player2Finished)
        {
            EndMinigame();
        }
        else
        {
            // Dar 5 segundos al otro jugador para llegar
            StartCoroutine(WaitThenEnd(5f));
        }
    }

    private IEnumerator WaitThenEnd(float seconds)
    {
        endingGame = true;
        yield return new WaitForSeconds(seconds);
        EndMinigame();
    }

    private void EndMinigame()
    {
        gameRunning = false;

        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(5);

        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }

    // ── Flash de pantalla ─────────────────────────────────────────────────────

    private IEnumerator DoFlash()
    {
        if (flashImage == null) yield break;

        flashImage.gameObject.SetActive(true);
        float half = flashDuration / 2f;

        // Fade in a negro
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            flashImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / half));
            yield return null;
        }

        flashImage.color = Color.black;
        yield return new WaitForSeconds(0.1f);

        // Fade out desde negro
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            flashImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - t / half));
            yield return null;
        }

        flashImage.color = new Color(0f, 0f, 0f, 0f);
        flashImage.gameObject.SetActive(false);
    }

    // ── Getters públicos ──────────────────────────────────────────────────────

    public bool IsGameRunning() => gameRunning;
    public ChaseRunPlayerController GetPlayer(int number) => number == 1 ? player1 : player2;
}
