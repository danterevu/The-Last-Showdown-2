using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Cuenta regresiva")]
    [SerializeField] private TextMeshProUGUI countdownText;

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
        yield return StartCoroutine(InitialCountdown());
    }

    private IEnumerator InitialCountdown()
    {
        FreezePlayers(true);
        roundOver = true;

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = 3; i >= 0; i--)
        {
            if (countdownText != null)
            {
                if (i > 0)
                {
                    countdownText.text = i.ToString();
                    yield return StartCoroutine(AnimateCountdownText(countdownText));
                }
                else
                {
                    countdownText.text = "¡Ya!";
                }
            }
            else if (i == 0)
            {
                yield return null;
            }

            if (i > 0)
                yield return new WaitForSeconds(0.5f);
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        FreezePlayers(false);
        roundOver = false;
    }

    private IEnumerator AnimateCountdownText(TextMeshProUGUI text)
    {
        Vector3 originalScale = text.transform.localScale;
        Vector3 originalPos = text.transform.localPosition;
        float duration = 0.5f;
        float shakeAmount = 10f;
        float scaleMultiplier = 1.3f;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;

            float scale = Mathf.Lerp(1f, scaleMultiplier, Mathf.PingPong(progress * 2, 1f));
            text.transform.localScale = originalScale * scale;

            float shakeX = Random.Range(-shakeAmount, shakeAmount) * (1f - progress);
            float shakeY = Random.Range(-shakeAmount, shakeAmount) * (1f - progress);
            text.transform.localPosition = originalPos + new Vector3(shakeX, shakeY, 0f);

            yield return null;
        }

        text.transform.localScale = originalScale;
        text.transform.localPosition = originalPos;
    }

    private void FreezePlayers(bool freeze)
    {
        if (player1 != null)
        {
            SpaceShipController ship1 = player1.GetComponent<SpaceShipController>();
            if (ship1 != null) ship1.enabled = !freeze;
            Rigidbody2D rb1 = player1.GetComponent<Rigidbody2D>();
            if (rb1 != null) rb1.simulated = !freeze;
        }
        if (player2 != null)
        {
            SpaceShipController ship2 = player2.GetComponent<SpaceShipController>();
            if (ship2 != null) ship2.enabled = !freeze;
            Rigidbody2D rb2 = player2.GetComponent<Rigidbody2D>();
            if (rb2 != null) rb2.simulated = !freeze;
        }
    }
    public void RegisterKill(int killer, int victim)
    {
        Debug.LogWarning($"[SPACEMINIGAME] RegisterKill llamado | Killer: {killer} | Victim: {victim} | RoundOver: {roundOver} | GameOver: {gameOver} | P1Invulnerable: {p1Invulnerable} | P2Invulnerable: {p2Invulnerable}");
        if (roundOver || gameOver) return;
        if (victim == 1 && p1Invulnerable) return;
        if (victim == 2 && p2Invulnerable) return;

        if (victim == 1) p1Invulnerable = true;
        else p2Invulnerable = true;

        // Marcar que la víctima debe perder su loadout al respawnear
        if (victim == 1) p1LosesLoadout = true;
        else p2LosesLoadout = true;

        Transform victimTransform = victim == 1 ? player1 : player2;
        SpaceShipController victimShip = victimTransform?.GetComponent<SpaceShipController>();
        
        Debug.Log($"[SPACEMINIGAME] Resetando velocidad de jugador {victim}");
        // Resetear velocidad del jugador al morir
        SlowField.RemoveShipFromAllSlowFields(victimShip);
        victimShip?.ResetSpeedToOriginal();
        
        victimTransform?.GetComponent<Explodable>()?.Explode();
        victimShip?.HideAllParticles();

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
            yield return StartCoroutine(FadeTransition());
            EndGame(gameWinner); // COMBO ROUNDS: pasamos el ganador
            yield break;
        }

        yield return new WaitForSeconds(respawnDelay);
        yield return StartCoroutine(ChangeZone(currentZoneIndex + 1));
    }

    private IEnumerator ChangeZone(int newZoneIndex)
    {
        FreezePlayers(true);
        roundOver = true;

        // Fade to black
        yield return StartCoroutine(FadeToBlack());

        // Activar nueva zona y respawnear jugadores
        ActivateZone(newZoneIndex);
        ResetRoundKills();

        DoRespawn(player1, player1Spawns[newZoneIndex]);
        DoRespawn(player2, player2Spawns[newZoneIndex]);

        // Cambio de zona: ambos pierden arma y power up
        ClearPlayerLoadout(player1);
        ClearPlayerLoadout(player2);
        p1LosesLoadout = false;
        p2LosesLoadout = false;
        p1Invulnerable = false;
        p2Invulnerable = false;

        // Cuenta regresiva + fade out
        yield return StartCoroutine(CountdownFadeAndRelease());
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    private IEnumerator CountdownFadeAndRelease()
    {
        bool fadeDone = false;
        bool countdownDone = false;

        StartCoroutine(FadeFromBlackCoroutine(() => fadeDone = true));

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = 3; i >= 0; i--)
        {
            if (countdownText != null)
            {
                if (i > 0)
                {
                    countdownText.text = i.ToString();
                    yield return StartCoroutine(AnimateCountdownText(countdownText));
                }
                else
                {
                    countdownText.text = "¡Ya!";
                }
            }
            else if (i == 0)
            {
                yield return null;
            }

            if (i > 0)
                yield return new WaitForSeconds(0.5f);
        }

        countdownDone = true;

        while (!fadeDone)
        {
            yield return null;
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        FreezePlayers(false);
        roundOver = false;
    }

    private IEnumerator FadeFromBlackCoroutine(System.Action onComplete)
    {
        if (fadeImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        for (float t = 0; t < fadeDuration * 3.5f; t += Time.deltaTime)
        {
            c.a = Mathf.Clamp01(1f - t / (fadeDuration * 3.5f));
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false);

        onComplete?.Invoke();
    }

    private IEnumerator FadeTransition()
    {
        yield return StartCoroutine(FadeToBlack());
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeFromBlackCoroutine(null));
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
        if (ship != null) {
            ship.ForceStop();
            ship.DeactivateRocketSabotage(); // Desactivar sabotaje al respawnear
        }

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
    public void EndGame(int gameWinner)
    {
        // COMBO ROUNDS: registrar el ganador del minijuego para acumular racha.
        // Se llama ANTES de FinishMinigame para que el bonus quede
        // incluido en los puntos de ronda que se transfieren al global.
        ModifierManager.Instance?.RegisterSpaceRoundWinner(gameWinner);

        GameManager.Instance?.FinishMinigame();
        GameManager.Instance?.EndRound(4);
        PlayerPrefs.SetInt("LastPlayedMinigame", 4); // el id de ese minijuego

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadResults();
    }


}