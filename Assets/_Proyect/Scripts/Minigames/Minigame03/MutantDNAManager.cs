using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MutantDNAManager : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float gameDuration = 120f;
    [SerializeField] private float zoneChangeDuration = 5f; // Tiempo entre cambios de zona

    [Header("Jugadores")]
    [SerializeField] private GameObject player1; private PlayerControllerDNA p1Controller;
    [SerializeField] private GameObject player2; private PlayerControllerDNA p2Controller;

    [Header("PowerUps")]
    [SerializeField] private DNAPowerUpSpawner powerUpSpawner;

    [Header("DNA")]
    [SerializeField] private DNA dnaPickup;

    [Header("Depositos")]
    [SerializeField] private Deposit deposit1;
    [SerializeField] private Deposit deposit2;

    [Header("UI")]
    [SerializeField] private MutantDNAHUD hud;

    [Header("Zona / Transición")]
    [SerializeField] private Transform[] zoneCenters;
    [SerializeField] private Transform[] player1Spawns;
    [SerializeField] private Transform[] player2Spawns;
    [SerializeField] private ZoneHandSpawns[] handSpawnsByZone;
    [SerializeField] private ZoneCameraTeleport zoneCamera;
    [SerializeField] private HandController handLeft;
    [SerializeField] private HandController handRight;
    [SerializeField] private Image fadeImage;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float handMoveSpeed = 5f;
    [SerializeField] private float fadeDuration = 1f;
    private List<int> availableZones = new List<int>();
    private bool initialSetupDone = false;

    [Header("Debug")]
    [SerializeField] private float gameTimer;
    [SerializeField] private float zoneTimer;
    [SerializeField] private bool gameRunning;
    [SerializeField] private int currentZoneIndex = 0;

    private void Start()
    {
        p1Controller = player1.GetComponent<PlayerControllerDNA>();
        p2Controller = player2.GetComponent<PlayerControllerDNA>();

        // Elegir zona inicial (puede ser 0 o aleatoria)
        currentZoneIndex = Random.Range(0, zoneCenters.Length);
        ActivateDNAZone(currentZoneIndex, teleportImmediately: true);

        if (zoneCamera != null && zoneCenters.Length > currentZoneIndex)
        {
            zoneCamera.MoveToCenter(zoneCenters[currentZoneIndex].position);
        }

        StartMinigame();

        if (hud != null) hud.UpdateTimer(gameTimer);
    }

    private void StartMinigame()
    {
        gameTimer = gameDuration;
        zoneTimer = zoneChangeDuration;
        FreezePlayers(true);
        hud.StartCountdown(OnCountdownFinished);
    }

    private void OnCountdownFinished()
    {
        FreezePlayers(false);
        gameRunning = true;
        dnaPickup.SpawnDNA();

        if (powerUpSpawner != null)
            powerUpSpawner.SetActiveZone(currentZoneIndex);
    }

    private void SetupAvailableZones()
    {
        availableZones.Clear();
        for (int i = 0; i < zoneCenters.Length; i++)
            availableZones.Add(i);
    }

    private int GetNextZone(int currentZone)
    {
        if (!initialSetupDone)
        {
            SetupAvailableZones();
            initialSetupDone = true;
        }

        // Remover la zona actual si está en la lista (para no repetirla)
        availableZones.Remove(currentZone);

        // Si no quedan zonas, reiniciar la lista con todas excepto la actual
        if (availableZones.Count == 0)
        {
            SetupAvailableZones();
            availableZones.Remove(currentZone);
        }

        int index = Random.Range(0, availableZones.Count);
        return availableZones[index];
    }

    private void Update()
    {
        if (!gameRunning) return;

        gameTimer -= Time.deltaTime;
        zoneTimer -= Time.deltaTime;

        if (zoneTimer <= 0f)
        {
            StartCoroutine(ChangeZone());
            zoneTimer = zoneChangeDuration;
        }

        if (gameTimer <= 0f)
        {
            gameTimer = 0f;
            EndMinigame();
        }

        if (hud != null) hud.UpdateTimer(gameTimer);
    }

    // -------------------------------------------------------------
    //  ZONE TRANSITION (adaptado de KingOfHill)
    // -------------------------------------------------------------
    private IEnumerator ChangeZone()
    {
        Debug.Log("ChangeZone() iniciado");
        gameRunning = false;

        int oldZoneIndex = currentZoneIndex;
        int newZoneIndex = GetNextZone(currentZoneIndex);

        yield return StartCoroutine(PlayZoneTransition(oldZoneIndex, newZoneIndex));

        // Limpiar power-ups
        p1Controller.ClearPowerUpState();
        p2Controller.ClearPowerUpState();

        currentZoneIndex = newZoneIndex;

        // Actualizar spawner a la nueva zona
        if (powerUpSpawner != null)
            powerUpSpawner.SetActiveZone(currentZoneIndex);

        gameRunning = true;
        Debug.Log("ChangeZone() terminado");
    }

    private IEnumerator PlayZoneTransition(int oldZoneIndex, int newZoneIndex)
    {
        FreezePlayers(true);
        yield return StartCoroutine(SpawnAndMoveHandsToPlayers(oldZoneIndex));
        yield return StartCoroutine(GrabPlayers());
        yield return StartCoroutine(MoveHandsAwayWithPlayers(oldZoneIndex));
        yield return StartCoroutine(FadeToBlack());

        if (zoneCamera != null && zoneCenters.Length > newZoneIndex)
        {
            zoneCamera.MoveToCenter(zoneCenters[newZoneIndex].position);
        }
        // Mover cámara si tienes ZoneCameraController, si no, opcional
        // yield return StartCoroutine(MoveCameraToNewZone(newZoneIndex));
        ActivateDNAZone(newZoneIndex, teleportImmediately: false);
        yield return StartCoroutine(CountdownFadeAndReleasePlayers(newZoneIndex));
        FreezePlayers(false);
    }

    private IEnumerator SpawnAndMoveHandsToPlayers(int zoneIndex)
    {
        if (handSpawnsByZone == null || handSpawnsByZone.Length <= zoneIndex) yield break;

        // Activar manos y colocarlas en sus spawns
        ZoneHandSpawns spawns = handSpawnsByZone[zoneIndex];
        if (handLeft != null && spawns.handLeftSpawn != null)
        {
            handLeft.gameObject.SetActive(true);
            handLeft.transform.position = spawns.handLeftSpawn.position;
            handLeft.OpenHand();
        }
        if (handRight != null && spawns.handRightSpawn != null)
        {
            handRight.gameObject.SetActive(true);
            handRight.transform.position = spawns.handRightSpawn.position;
            handRight.OpenHand();
        }

        // Estirar manos hacia los jugadores
        bool leftDone = false, rightDone = false;
        if (handLeft != null) StartCoroutine(StretchHandCoroutine(handLeft, player1.transform.position, () => leftDone = true));
        if (handRight != null) StartCoroutine(StretchHandCoroutine(handRight, player2.transform.position, () => rightDone = true));
        while (!leftDone || !rightDone) yield return null;

        // Mover manos hasta la posición exacta de los jugadores
        yield return StartCoroutine(MoveHandsToPositions(player1.transform.position, player2.transform.position));
    }

    private IEnumerator StretchHandCoroutine(HandController hand, Vector3 target, System.Action onComplete)
    {
        if (hand != null) yield return StartCoroutine(hand.StretchTowards(target));
        onComplete?.Invoke();
    }

    private IEnumerator MoveHandsToPositions(Vector3 leftTarget, Vector3 rightTarget)
    {
        while (true)
        {
            bool leftDone = true, rightDone = true;
            if (handLeft != null)
            {
                handLeft.transform.position = Vector3.MoveTowards(handLeft.transform.position, leftTarget, handMoveSpeed * Time.deltaTime);
                leftDone = Vector3.Distance(handLeft.transform.position, leftTarget) < 0.1f;
            }
            if (handRight != null)
            {
                handRight.transform.position = Vector3.MoveTowards(handRight.transform.position, rightTarget, handMoveSpeed * Time.deltaTime);
                rightDone = Vector3.Distance(handRight.transform.position, rightTarget) < 0.1f;
            }
            if (leftDone && rightDone) break;
            yield return null;
        }
    }

    private IEnumerator GrabPlayers()
    {
        if (handLeft != null && !handLeft.HasPlayer) handLeft.CloseHand();
        if (handRight != null && !handRight.HasPlayer) handRight.CloseHand();

        if (handLeft != null && !handLeft.HasPlayer && player1 != null) handLeft.GrabPlayer(player1);
        if (handRight != null && !handRight.HasPlayer && player2 != null) handRight.GrabPlayer(player2);

        // Escala de manos (opcional)
        bool leftScaleDone = false, rightScaleDone = false;
        if (handLeft != null) StartCoroutine(ReturnHandScaleCoroutine(handLeft, () => leftScaleDone = true));
        if (handRight != null) StartCoroutine(ReturnHandScaleCoroutine(handRight, () => rightScaleDone = true));
        while (!leftScaleDone || !rightScaleDone) yield return null;
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator ReturnHandScaleCoroutine(HandController hand, System.Action onComplete)
    {
        if (hand != null) yield return StartCoroutine(hand.ReturnToOriginalScale());
        onComplete?.Invoke();
    }

    private IEnumerator MoveHandsAwayWithPlayers(int zoneIndex)
    {
        if (handSpawnsByZone == null || handSpawnsByZone.Length <= zoneIndex) yield break;
        ZoneHandSpawns spawns = handSpawnsByZone[zoneIndex];
        yield return StartCoroutine(MoveHandsToPositions(
            spawns.handLeftSpawn != null ? spawns.handLeftSpawn.position : handLeft.transform.position,
            spawns.handRightSpawn != null ? spawns.handRightSpawn.position : handRight.transform.position
        ));
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            color.a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
        color.a = 1f;
        fadeImage.color = color;
    }

    private IEnumerator CountdownFadeAndReleasePlayers(int newZoneIndex)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);
        bool fadeDone = false;
        bool playersReleased = false;

        StartCoroutine(FadeFromBlackCoroutine(() => fadeDone = true));
        StartCoroutine(ReleasePlayersCoroutine(newZoneIndex, () => playersReleased = true));

        // Mostrar cuenta regresiva (3,2,1,¡Ya!)
        for (int i = 3; i >= 0; i--)
        {
            if (countdownText != null)
            {
                if (i > 0) countdownText.text = i.ToString();
                else countdownText.text = "¡Ya!";
                yield return StartCoroutine(AnimateCountdownText(countdownText));
            }
            if (i > 0) yield return new WaitForSeconds(0.5f);
        }

        while (!fadeDone || !playersReleased) yield return null;

        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    private IEnumerator ReleasePlayersCoroutine(int newZoneIndex, System.Action onComplete)
    {
        if (handSpawnsByZone == null || handSpawnsByZone.Length <= newZoneIndex) { onComplete?.Invoke(); yield break; }
        if (player1Spawns == null || player2Spawns == null) { onComplete?.Invoke(); yield break; }

        ZoneHandSpawns spawns = handSpawnsByZone[newZoneIndex];
        if (handLeft != null && spawns.handLeftSpawn != null)
            handLeft.transform.position = spawns.handLeftSpawn.position;
        if (handRight != null && spawns.handRightSpawn != null)
            handRight.transform.position = spawns.handRightSpawn.position;

        // Estirar manos hacia los nuevos spawns
        bool leftStretch = false, rightStretch = false;
        if (handLeft != null) StartCoroutine(StretchHandCoroutine(handLeft, player1Spawns[newZoneIndex].position, () => leftStretch = true));
        if (handRight != null) StartCoroutine(StretchHandCoroutine(handRight, player2Spawns[newZoneIndex].position, () => rightStretch = true));
        while (!leftStretch || !rightStretch) yield return null;

        yield return StartCoroutine(MoveHandsToPositions(player1Spawns[newZoneIndex].position, player2Spawns[newZoneIndex].position));

        if (handLeft != null) handLeft.ReleasePlayer(player1Spawns[newZoneIndex].position);
        if (handRight != null) handRight.ReleasePlayer(player2Spawns[newZoneIndex].position);

        // Restaurar escala
        bool leftScale = false, rightScale = false;
        if (handLeft != null) StartCoroutine(ReturnHandScaleCoroutine(handLeft, () => leftScale = true));
        if (handRight != null) StartCoroutine(ReturnHandScaleCoroutine(handRight, () => rightScale = true));
        while (!leftScale || !rightScale) yield return null;

        yield return new WaitForSeconds(0.5f);
        // Desactivar manos
        if (handLeft != null) handLeft.gameObject.SetActive(false);
        if (handRight != null) handRight.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator FadeFromBlackCoroutine(System.Action onComplete)
    {
        if (fadeImage == null) { onComplete?.Invoke(); yield break; }
        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;
        for (float t = 0; t < fadeDuration * 3.5f; t += Time.deltaTime)
        {
            color.a = Mathf.Clamp01(1f - t / (fadeDuration * 3.5f));
            fadeImage.color = color;
            yield return null;
        }
        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
        onComplete?.Invoke();
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

    // -------------------------------------------------------------
    //  ACTIVE ZONE
    // -------------------------------------------------------------
    private void ActivateDNAZone(int index, bool teleportImmediately = false)
    {
        if (teleportImmediately)
        {
            player1.transform.position = player1Spawns[index].position;
            player2.transform.position = player2Spawns[index].position;
        }
        // Opcional: actualizar spawn points en los controladores
        p1Controller.SetSpawnPoint(player1Spawns[index].position);
        p2Controller.SetSpawnPoint(player2Spawns[index].position);
        // Respawnea el ADN (opcional, puedes elegir no respawnearlo aquí)
        dnaPickup.SpawnDNA();
        // Si tienes cajas estáticas, también puedes reposicionarlas según la zona
        // (lo dejo a tu criterio)
    }

    // -------------------------------------------------------------
    //  UTILS
    // -------------------------------------------------------------
    private void FreezePlayers(bool freeze)
    {
        if (p1Controller != null) p1Controller.SetFrozen(freeze);
        if (p2Controller != null) p2Controller.SetFrozen(freeze);
    }

    public void EndMinigame()
    {
        gameRunning = false;
        if (hud != null) hud.StopTimerEffects();
        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(3);
        PlayerPrefs.SetInt("LastPlayedMinigame", 3);
        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);
        SceneLoader.Instance.LoadResults();
    }
}