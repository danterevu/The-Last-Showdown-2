using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MutantDNAManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float gameDuration = 120f;

    [Header("Jugadores")]
    [SerializeField] private GameObject player1;   private PlayerControllerDNA p1Controller;
    [SerializeField] private GameObject player2;   private PlayerControllerDNA p2Controller;

    [Header("DNA")]
    [SerializeField] private DNA dnaPickup;

    [Header("Depósitos")]
    [SerializeField] private Deposit deposit1; // el que solo acepta P1
    [SerializeField] private Deposit deposit2; // el que solo acepta P2

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;

    [Header("Debug")]
    [SerializeField] private float gameTimer;
    [SerializeField] private bool gameRunning;

    private void Start()
    {
        p1Controller = player1.GetComponent<PlayerControllerDNA>();
        p2Controller = player2.GetComponent<PlayerControllerDNA>();

        StartMinigame();
    }

    private void StartMinigame()
    {
        gameTimer = gameDuration;
        gameRunning = true;

        dnaPickup.SpawnDNA();
        UpdateUI();

    }

    private void Update()
    {
        if (!gameRunning) return;

        gameTimer -= Time.deltaTime;

        if (gameTimer <= 0f)
        {
            gameTimer = 0f;
            EndMinigame();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(gameTimer / 60f);
        int seconds = Mathf.FloorToInt(gameTimer % 60f);
        if (timerText) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (player1ScoreText)
            player1ScoreText.text = "" + GameManager.Instance.player1RoundPoints;
        if (player2ScoreText)
            player2ScoreText.text = "" + GameManager.Instance.player2RoundPoints;
    }

    public void EndMinigame()
    {
        gameRunning = false;

        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(3); // id del minijuego DNA

        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }
}