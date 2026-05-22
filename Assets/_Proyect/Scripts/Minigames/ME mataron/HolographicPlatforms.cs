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

    [Header("Spawns por tile")]
    [SerializeField] private int player1SpawnCol = 1;
    [SerializeField] private int player1SpawnRow = 1;
    [SerializeField] private int player2SpawnCol = 8;
    [SerializeField] private int player2SpawnRow = 8;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;

    [Header("Debug")]
    [SerializeField] private float gameTimer;
    [SerializeField] private bool gameRunning;

    private Rigidbody2D rb1;
    private Rigidbody2D rb2;
    private Collider2D col1;
    private Collider2D col2;

    private bool player1Fell = false;
    private bool player2Fell = false;
    private bool isHandlingFall = false;

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
        rb1 = player1.GetComponent<Rigidbody2D>();
        rb2 = player2.GetComponent<Rigidbody2D>();
        col1 = player1.GetComponent<Collider2D>();
        col2 = player2.GetComponent<Collider2D>();

        StartMinigame();
    }

    private void StartMinigame()
    {
        gameTimer = gameDuration;
        gameRunning = true;

        // Teletransporte inicial
        StartCoroutine(TeleportPlayerSafe(1));
        StartCoroutine(TeleportPlayerSafe(2));

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
            return;
        }

        UpdateUI();
    }

    public void OnPlayerFell(int player)
    {
        if (!gameRunning) return;

        Debug.Log("OnPlayerFell llamado: Player" + player);

        if (player == 1) player1Fell = true;
        else player2Fell = true;

        if (!isHandlingFall)
            StartCoroutine(EvaluateFall());
    }

    private IEnumerator EvaluateFall()
    {
        isHandlingFall = true;
        yield return null;

        bool simultaneous = player1Fell && player2Fell;

        if (simultaneous)
            Debug.Log("Caida simultanea");
        else if (player1Fell)
            GameManager.Instance.AddPoints(2, 1);
        else if (player2Fell)
            GameManager.Instance.AddPoints(1, 1);

        tileMapManager.RequestRebuild();
        yield return new WaitForSeconds(tileMapManager.GetRebuildDuration());

        if (player1Fell)
            yield return StartCoroutine(TeleportPlayerSafe(1));

        if (player2Fell)
            yield return StartCoroutine(TeleportPlayerSafe(2));

        player1Fell = false;
        player2Fell = false;
        isHandlingFall = false;

        UpdateUI();
    }

    // ==================== TELEPORTE SEGURO ====================
    private IEnumerator TeleportPlayerSafe(int player)
    {
        GameObject obj = player == 1 ? player1 : player2;
        PlayerController controller = player == 1 ? p1Controller : p2Controller;
        Rigidbody2D rb = player == 1 ? rb1 : rb2;

        int spawnCol = player == 1 ? player1SpawnCol : player2SpawnCol;
        int spawnRow = player == 1 ? player1SpawnRow : player2SpawnRow;
        Vector2 spawnPos = tileMapManager.GetTilePosition(spawnCol, spawnRow);

        Debug.Log($"Teletransportando Player{player} a: {spawnPos}");

        // 1. Guardar estado activo y desactivar el GameObject
        bool wasActive = obj.activeSelf;
        obj.SetActive(false);

        // 2. Mover la posición (no hay scripts ni físicas activas)
        obj.transform.position = new Vector3(spawnPos.x, spawnPos.y + 0.1f, obj.transform.position.z);

        // 3. Esperar un frame para asegurar que Unity procese
        yield return null;

        // 4. Reactivar
        obj.SetActive(wasActive);

        // 5. Resetear velocidades (por si acaso)
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();

        Debug.Log($"Player{player} teletransportado exitosamente a {obj.transform.position}");
    }
    // ========================================================

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

    public void EndMinigame()
    {
        gameRunning = false;

        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(3);

        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }
}