using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MutantDNAManager : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float gameDuration = 120f;

    [Header("Jugadores")]
    [SerializeField] private GameObject player1;   private PlayerControllerDNA p1Controller;
    [SerializeField] private GameObject player2;   private PlayerControllerDNA p2Controller;

    [Header("DNA")]
    [SerializeField] private DNA dnaPickup;

    [Header("Depositos")]
    [SerializeField] private Deposit deposit1; // el que solo acepta P1
    [SerializeField] private Deposit deposit2; // el que solo acepta P2

    [Header("UI")]
    [SerializeField] private MutantDNAHUD hud;  // arrastra el objeto con el script HUD

    [Header("Debug")]
    [SerializeField] private float gameTimer;
    [SerializeField] private bool gameRunning;

    private void Start()
    {
        p1Controller = player1.GetComponent<PlayerControllerDNA>();
        p2Controller = player2.GetComponent<PlayerControllerDNA>();

        StartMinigame();

        if (hud != null)
        {
            hud.UpdateTimer(gameTimer);
        }
    }

    private void StartMinigame()
    {
        gameTimer = gameDuration;
        FreezePlayers(true);  // Congelar antes del countdown
        hud.StartCountdown(OnCountdownFinished);
    }

    private void OnCountdownFinished()
    {
        FreezePlayers(false); // Descongelar al terminar
        gameRunning = true;
        dnaPickup.SpawnDNA();
    }

    private void FreezePlayers(bool freeze)
    {
        PlayerControllerDNA p1 = player1.GetComponent<PlayerControllerDNA>();
        PlayerControllerDNA p2 = player2.GetComponent<PlayerControllerDNA>();
        if (p1 != null) p1.SetFrozen(freeze);
        if (p2 != null) p2.SetFrozen(freeze);
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
        if (!gameRunning) return;
        gameTimer -= Time.deltaTime;
        if (hud != null) hud.UpdateTimer(gameTimer);
        

        //UpdateUI();
    }
    public void EndMinigame()
    {
        gameRunning = false;
        if (hud != null) hud.StopTimerEffects();
        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(3); // id del minijuego DNA
        PlayerPrefs.SetInt("LastPlayedMinigame", 3);
        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }
}