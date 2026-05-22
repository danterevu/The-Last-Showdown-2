using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpaceMinigame : MonoBehaviour
{
    public static SpaceMinigame Instance;

    [Header("Jugadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Spawns por zona (indice = zona)")]
    [SerializeField] private Transform[] player1Spawns;
    [SerializeField] private Transform[] player2Spawns;

    [Header("Zonas")]
    [SerializeField] private SpaceZoneBoundary[] zones;

    [Header("Camara")]
    [SerializeField] private ZoneCameraController zoneCamera;

    [Header("Flash de transicion")]
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.8f;

    [Header("HUD")]
    [SerializeField] private HUDManager hudManager;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.5f;




    private const int KILLS_TO_WIN_ROUND = 3;
    private const int ROUNDS_TO_WIN_GAME = 3;
    private const int POINTS_WIN_ROUND = 20;
    private const int POINTS_WIN_GAME = 150;

    private int currentZoneIndex = 0;

    private int kills1 = 0;
    private int kills2 = 0;

    private int roundsWon1 = 0;
    private int roundsWon2 = 0;

    private bool roundOver = false;
    private bool gameOver = false;

    private bool p1Invulnerable = false;
    private bool p2Invulnerable = false;

    // Flags: qué jugador debe perder su loadout en el próximo respawn
    // Usando bools en lugar de lastVictim para soportar kills simultáneos correctamente
    private bool p1LosesLoadout = false;
    private bool p2LosesLoadout = false;

    private WeaponSpawner weaponSpawner;
    private SpacePowerUpSpawner powerUpSpawner;

    [SerializeField] private SpaceHUDRounds hudRounds;
    private void Awake()
    {
        Instance = this;

        weaponSpawner = FindFirstObjectByType<WeaponSpawner>();
        powerUpSpawner = FindFirstObjectByType<SpacePowerUpSpawner>();

        Debug.Log($"SpaceMinigame Awake | weaponSpawner: {weaponSpawner} | powerUpSpawner: {powerUpSpawner}");
    }

    private void Start()
    {
        Debug.Log($"SpaceMinigame Start | ModifierManager: {ModifierManager.Instance} | GameManager: {GameManager.Instance}");

        ModifierManager.Instance?.ResetGoldenKill();
        // GOLDEN KILL: habilitar el flag para la primera kill del primer round
        ModifierManager.Instance?.ResetGoldenKill();
        hudRounds?.UpdateKills(0, 0);
        hudRounds?.UpdateRounds(0, 0);
        hudRounds?.UpdateCurrentRound(1);
        StartCoroutine(InitDelayed());
    }
    private IEnumerator InitDelayed()
    {
        // esperar un frame para que todo este inicializado
        yield return null;

        ActivateZone(0);
        DoRespawn(player1, player1Spawns[0]);
        DoRespawn(player2, player2Spawns[0]);
    }
    public void RegisterKill(int killer, int victim)
    {
        Debug.Log($"RegisterKill llamado | killer:{killer} victim:{victim} | roundOver:{roundOver} | gameOver:{gameOver} | p1Inv:{p1Invulnerable} | p2Inv:{p2Invulnerable}");
        if (roundOver || gameOver) return;
        if (victim == 1 && p1Invulnerable) return;
        if (victim == 2 && p2Invulnerable) return;

        if (victim == 1) p1Invulnerable = true;
        else p2Invulnerable = true;

        // Marcar que la víctima debe perder su loadout al respawnear
        if (victim == 1) p1LosesLoadout = true;
        else p2LosesLoadout = true;

        Transform victimTransform = victim == 1 ? player1 : player2;
        victimTransform?.GetComponent<Explodable>()?.Explode();

        if (killer == 1) kills1++;
        else kills2++;

        hudRounds?.UpdateKills(kills1, kills2);

        if (kills1 >= KILLS_TO_WIN_ROUND || kills2 >= KILLS_TO_WIN_ROUND)
        {
            int roundWinner = kills1 >= KILLS_TO_WIN_ROUND ? 1 : 2;
            if (roundWinner == 1) roundsWon1++;
            else roundsWon2++;
            hudRounds?.UpdateCurrentRound(roundsWon1 + roundsWon2 + 1);
            hudRounds?.UpdateRounds(roundsWon1, roundsWon2);
            StartCoroutine(HandleRoundWon(roundWinner));
        }
        else
        {
            // Verificar que el objeto existe antes de iniciar el coroutine
            if (this != null && gameObject != null && gameObject.activeInHierarchy)
                StartCoroutine(RespawnBothDelayed());
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator HandleRoundWon(int winner)
    {
        roundOver = true;

        GameManager.Instance?.AddPoints(winner, POINTS_WIN_ROUND);

        bool gameWon = roundsWon1 >= ROUNDS_TO_WIN_GAME || roundsWon2 >= ROUNDS_TO_WIN_GAME;
        bool noMoreZones = (currentZoneIndex + 1) >= (zones != null ? zones.Length : 0);

        if (gameWon || noMoreZones)
        {
            int gameWinner;
            if (roundsWon1 > roundsWon2) gameWinner = 1;
            else if (roundsWon2 > roundsWon1) gameWinner = 2;
            else
                gameWinner = (GameManager.Instance != null &&
                              GameManager.Instance.player1RoundPoints >= GameManager.Instance.player2RoundPoints) ? 1 : 2;

            GameManager.Instance?.AddPoints(gameWinner, POINTS_WIN_GAME);

            gameOver = true;
            yield return new WaitForSeconds(respawnDelay);
            yield return StartCoroutine(FlashTransition());
            EndGame(gameWinner); // COMBO ROUNDS: pasamos el ganador
            yield break;
        }

        yield return new WaitForSeconds(respawnDelay);
        yield return StartCoroutine(FlashTransition());

        ActivateZone(currentZoneIndex + 1);
        ResetRoundKills();

        DoRespawn(player1, player1Spawns[currentZoneIndex]);
        DoRespawn(player2, player2Spawns[currentZoneIndex]);

        // Cambio de zona: ambos pierden arma y power up
        ClearPlayerLoadout(player1);
        ClearPlayerLoadout(player2);
        p1LosesLoadout = false;
        p2LosesLoadout = false;

        p1Invulnerable = false;
        p2Invulnerable = false;
        roundOver = false;
    }

    private IEnumerator RespawnBothDelayed()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (roundOver || gameOver) yield break;

        DoRespawn(player1, player1Spawns[currentZoneIndex]);
        DoRespawn(player2, player2Spawns[currentZoneIndex]);

        // La víctima pierde arma y power up; el killer conserva todo.
        // Los flags pueden ser ambos true si hubo kills simultáneos.
        if (p1LosesLoadout) ClearPlayerLoadout(player1);
        if (p2LosesLoadout) ClearPlayerLoadout(player2);
        p1LosesLoadout = false;
        p2LosesLoadout = false;

        p1Invulnerable = false;
        p2Invulnerable = false;
    }

    private void DoRespawn(Transform player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null) return;

        player.position = spawnPoint.position;
        player.rotation = spawnPoint.rotation;

        var ship = player.GetComponent<SpaceShipController>();
        if (ship != null) ship.ForceStop();

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void ActivateZone(int index)
    {
        currentZoneIndex = index;

        if (zones != null)
        {
            for (int i = 0; i < zones.Length; i++)
                zones[i].gameObject.SetActive(i == index);
        }

        if (zoneCamera != null && zones != null && index < zones.Length)
            zoneCamera.SetZoneCenter(zones[index].Center, index);

        weaponSpawner?.SetActiveZone(index);
        powerUpSpawner?.SetActiveZone(index);
    }

    private void ResetRoundKills()
    {
        kills1 = 0;
        kills2 = 0;

        // GOLDEN KILL: la primera kill del nuevo round vuelve a ser elegible
        ModifierManager.Instance?.ResetGoldenKill();
    }

    /// Quita arma y power up a un jugador. Se llama DESPUÉS de DoRespawn para
    /// evitar que la re-inicialización del respawn pise el clear.
    /// Usa GetComponentInChildren para funcionar sin importar la jerarquía del prefab.
    private void ClearPlayerLoadout(Transform player)
    {
        if (player == null) return;

        WeaponController wc = player.GetComponentInChildren<WeaponController>(true);
        wc?.ResetToDefault();

        PowerUpHolder holder = player.GetComponentInChildren<PowerUpHolder>(true);
        holder?.ClearPowerUp();
    }

    // gameWinner: 1 o 2 (qui�n gan� el minijuego completo)
    private void EndGame(int gameWinner)
    {
        // COMBO ROUNDS: registrar el ganador del minijuego para acumular racha.
        // Se llama ANTES de FinishMinigame para que el bonus quede
        // incluido en los puntos de ronda que se transfieren al global.
        ModifierManager.Instance?.RegisterSpaceRoundWinner(gameWinner);

        GameManager.Instance?.FinishMinigame();
        GameManager.Instance?.EndRound(5);

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadResults();
    }

    private IEnumerator FlashTransition()
    {
        if (flashImage == null) yield break;

        flashImage.enabled = true;
        Color c = flashImage.color;
        float half = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            c.a = Mathf.Lerp(0f, 1f, elapsed / half);
            flashImage.color = c;
            elapsed += Time.deltaTime;
            yield return null;
        }

        c.a = 1f;
        flashImage.color = c;
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < half)
        {
            c.a = Mathf.Lerp(1f, 0f, elapsed / half);
            flashImage.color = c;
            elapsed += Time.deltaTime;
            yield return null;
        }

        c.a = 0f;
        flashImage.color = c;
        flashImage.enabled = false;
    }
}