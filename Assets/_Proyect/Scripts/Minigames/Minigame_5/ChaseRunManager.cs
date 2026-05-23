using UnityEngine;
using System.Collections;


public class ChaseRunManager : MonoBehaviour
{
    public static ChaseRunManager Instance { get; private set; }

    // Fases 
    public enum RunPhase { PhaseY, PhaseX }
    public RunPhase CurrentPhase { get; private set; } = RunPhase.PhaseY;

    // Referencias

    [Header("Jugadores")]
    [SerializeField] private ChaseRunPlayerController player1;
    [SerializeField] private ChaseRunPlayerController player2;

    [Header("Camara")]
    [SerializeField] private ChaseRunCamera chaseCamera;

    [Header("Power Ups")]
    [SerializeField] private ChaseRunPowerUpSpawner powerUpSpawner;

    [Header("UI / Flash")]
    [SerializeField] private UnityEngine.UI.Image flashImage;
    [SerializeField] private float flashDuration = 0.6f;

    // Scoring por supervivencia 
    [Header("Scoring")]
    [Tooltip("Puntos que gana un jugador por cada intervalo que sobrevive")]
    [SerializeField] private int pointsPerSurvivalInterval = 2;
    [SerializeField] private float survivalInterval = 3f;

    [Tooltip("Bonus de puntos al llegar a la meta")]
    [SerializeField] private int goalBonusPoints = 50;

    // Estado interno 
    private bool gameRunning = false;
    private bool player1Finished = false;
    private bool player2Finished = false;

    private float survivalTimer1 = 0f;
    private float survivalTimer2 = 0f;

  
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Asegurar GameManager en escena standalone (testing)
        if (GameManager.Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        StartMinigame();
    }

    private void StartMinigame()
    {
        CurrentPhase = RunPhase.PhaseY;
        gameRunning = true;

        player1.Initialize(this);
        player2.Initialize(this);

        chaseCamera.SetPhase(RunPhase.PhaseY);
        powerUpSpawner.SetPhase(RunPhase.PhaseY);
    }

    private void Update()
    {
        if (!gameRunning) return;

        // Puntos de supervivencia 
        if (!player1Finished)
        {
            survivalTimer1 += Time.deltaTime;
            if (survivalTimer1 >= survivalInterval)
            {
                survivalTimer1 = 0f;
                GameManager.Instance.AddPoints(1, pointsPerSurvivalInterval);
            }
        }

        if (!player2Finished)
        {
            survivalTimer2 += Time.deltaTime;
            if (survivalTimer2 >= survivalInterval)
            {
                survivalTimer2 = 0f;
                GameManager.Instance.AddPoints(2, pointsPerSurvivalInterval);
            }
        }
    }

    // Llamado por TriggerZone cuando el runner (camara) lo toca 
    public void TriggerPhaseChange()
    {
        if (CurrentPhase == RunPhase.PhaseX) return; // ya estamos en X
        StartCoroutine(DoPhaseChange());
    }

    private IEnumerator DoPhaseChange()
    {
        gameRunning = false;
        yield return StartCoroutine(Flash());

        CurrentPhase = RunPhase.PhaseX;
        chaseCamera.SetPhase(RunPhase.PhaseX);
        powerUpSpawner.SetPhase(RunPhase.PhaseX);

        // Notificar a los jugadores (para que sepan el eje de la kill zone)
        player1.OnPhaseChanged(RunPhase.PhaseX);
        player2.OnPhaseChanged(RunPhase.PhaseX);

        gameRunning = true;
    }

    // Llamado por GoalTrigger cuando un jugador toca la meta 
    public void PlayerReachedGoal(int playerNumber)
    {
        if (playerNumber == 1 && !player1Finished)
        {
            player1Finished = true;
            GameManager.Instance.AddPoints(1, goalBonusPoints);
        }
        else if (playerNumber == 2 && !player2Finished)
        {
            player2Finished = true;
            GameManager.Instance.AddPoints(2, goalBonusPoints);
        }

        // Si los dos terminaron, o uno termino y queremos terminar igual
        if (player1Finished && player2Finished)
            EndMinigame();
        else if (player1Finished || player2Finished)
            // dar 5 segundos al otro para llegar
            StartCoroutine(WaitThenEnd(5f));
    }

    private IEnumerator WaitThenEnd(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        EndMinigame();
    }

    private void EndMinigame()
    {
        if (!gameRunning && !player1Finished && !player2Finished) return;
        gameRunning = false;

        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(5);

        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }

    //  Flash de pantalla al cambiar fase 
    private IEnumerator Flash()
    {
        if (flashImage == null) yield break;

        flashImage.gameObject.SetActive(true);
        float half = flashDuration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / half));
            yield return null;
        }

        flashImage.color = Color.black;
        yield return new WaitForSeconds(0.1f);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - t / half));
            yield return null;
        }

        flashImage.gameObject.SetActive(false);
    }

    // Getters de estado
    public bool IsGameRunning() => gameRunning;
    public ChaseRunPlayerController GetPlayer(int number) => number == 1 ? player1 : player2;
}
