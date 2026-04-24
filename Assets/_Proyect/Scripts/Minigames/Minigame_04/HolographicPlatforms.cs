using UnityEngine;
using TMPro;
using System.Collections;

public class HolographicPlatforms : MonoBehaviour
{
    [Header("Minigame Settings")]
    [SerializeField] private float gameDuration = 120f;

    [Header("References")]
    [SerializeField] private TileMapManager tileMapManager;
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private Transform player1Spawn;
    [SerializeField] private Transform player2Spawn;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.5f; // espera antes de respawnear

    [Header("Debug")]
    [SerializeField] private float gameTimer;
    [SerializeField] private bool gameRunning;

    // para detectar caida simultanea
    private bool player1Fell = false;
    private bool player2Fell = false;
    private bool isHandlingFall = false; // evita que se llame dos veces a la vez

    private PlayerController p1Controller;
    private PlayerController p2Controller;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager no encontrado.");
            return;
        }

        p1Controller = player1.GetComponent<PlayerController>();
        p2Controller = player2.GetComponent<PlayerController>();

        StartMinigame();
    }

   
    //  INICIO


    private void StartMinigame()
    {
        gameTimer = gameDuration;
        gameRunning = true;

        RespawnPlayer(1, instant: true);
        RespawnPlayer(2, instant: true);

        UpdateUI();
    }


    //  UPDATE
   

    private void Update()
    {
        if (!gameRunning) return;

        gameTimer -= Time.deltaTime;

        if (gameTimer <= 0f)
        {
            gameTimer = 0f;
            EndMinigame();
            return;
        }

        UpdateUI();
    }

    
    //  CAIDA - llamado desde DeathZone
   

    public void OnPlayerFell(int player)
    {
        if (!gameRunning) return;
        if (isHandlingFall) return; // ya estamos procesando una caida

        // registrar quien cayo
        if (player == 1) player1Fell = true;
        else player2Fell = true;

        // esperar un frame para ver si el otro tambien cayo
        StartCoroutine(EvaluateFall());
    }

    private IEnumerator EvaluateFall()
    {
        if (isHandlingFall) yield break;
        isHandlingFall = true;

        // esperar un frame para que OnPlayerFell del otro jugador
        // pueda registrarse si cayo al mismo tiempo
        yield return null;

        bool simultaneous = player1Fell && player2Fell;

        if (simultaneous)
        {
            // nadie suma puntos
            Debug.Log("Caida simultanea - no se suman puntos");
        }
        else if (player1Fell)
        {
            // player1 cayo, player2 gana el punto
            GameManager.Instance.AddPoints(2, 1);
        }
        else if (player2Fell)
        {
            // player2 cayo, player1 gana el punto
            GameManager.Instance.AddPoints(1, 1);
        }

        // congelar jugadores caidos mientras se reconstruye
        FreezePlayer(1, player1Fell);
        FreezePlayer(2, player2Fell);

        // reconstruir el mapa
        tileMapManager.RequestRebuild();

        // esperar el delay de respawn antes de reaparecer
        yield return new WaitForSeconds(respawnDelay + tileMapManager.GetRebuildDuration());

        // respawnear solo a los que cayeron
        if (player1Fell) RespawnPlayer(1, instant: false);
        if (player2Fell) RespawnPlayer(2, instant: false);

        // resetear flags
        player1Fell = false;
        player2Fell = false;
        isHandlingFall = false;

        UpdateUI();
    }

   
    //  RESPAWN


    private void RespawnPlayer(int player, bool instant)
    {
        if (player == 1)
        {
            player1.transform.position = player1Spawn.position;
            FreezePlayer(1, false);
        }
        else
        {
            player2.transform.position = player2Spawn.position;
            FreezePlayer(2, false);
        }
    }

    private void FreezePlayer(int player, bool frozen)
    {
        if (player == 1)
            p1Controller.SetFrozen(frozen);
        else
            p2Controller.SetFrozen(frozen);
    }

   
    //  UI
   

    private void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTimer / 60f);
            int seconds = Mathf.FloorToInt(gameTimer % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (player1ScoreText != null)
            player1ScoreText.text = "P1: " + GameManager.Instance.player1RoundPoints;
        if (player2ScoreText != null)
            player2ScoreText.text = "P2: " + GameManager.Instance.player2RoundPoints;
    }

    
    //  FIN
    

    public void EndMinigame()
    {
        gameRunning = false;

        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(3); // id de este minijuego

        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }
}