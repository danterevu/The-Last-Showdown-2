using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class ZoneHandSpawns
{
    public Transform handLeftSpawn;
    public Transform handRightSpawn;
}

public class KingOfHill : MonoBehaviour, IMinijuegoControlable
{
    [Header("Configuracion")]
    [SerializeField] private float gameDuration = 120f;
    [SerializeField] private float zoneChangeDuration = 30f;
    [SerializeField] private float pointsPerSecond = 2f;
    [SerializeField] private float flashDuration = 1f;

    [Header("Zonas")]
    [SerializeField] private Transform[] zones;
    [SerializeField] private HardPoint[] hardPoints;

    [Header("Spawns")]
    [SerializeField] private Transform[] player1Spawns;
    [SerializeField] private Transform[] player2Spawns;

    [Header("Jugadores")]
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;

    [Header("Camara")]
    [SerializeField] private ZoneCameraController zoneCamera;

    [Header("PowerUps")]
    [SerializeField] private PowerUpEffects powerUpEffects;
    [SerializeField] private PowerUpSpawner powerUpSpawner;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;
    [SerializeField] private Image flashImage;
    [SerializeField] private PowerUpHUD hudPlayer1;
    [SerializeField] private PowerUpHUD hudPlayer2;

    [Header("Transicion de Zona")]
    [SerializeField] private float handMoveSpeed = 5f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("UI de Transicion")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Manos")]
    [SerializeField] private HandController handLeft;
    [SerializeField] private HandController handRight;
    [SerializeField] private ZoneHandSpawns[] handSpawnsByZone;

    [Header("Debug")]
    [SerializeField] private int currentZoneIndex = 0;
    [SerializeField] private float gameTimer;
    [SerializeField] private float zoneTimer;
    [SerializeField] private bool gameRunning;

    [Header("Comentarista")]
    [SerializeField] private int comebackMinDiff = 10;   // pts de ventaja mínima
    [SerializeField] private float awareChangeZoneWarning = 8f;   // seg antes del cambio
    [SerializeField] private float alertEndGameSeconds = 10f;  // seg restantes al final


    private float pointAccumulator1 = 0f;
    private float pointAccumulator2 = 0f;
    private PlatformPlayerController p1Controller;
    private PlatformPlayerController p2Controller;

    private float pointBleedTimer1 = 0f;
    private float pointBleedTimer2 = 0f;
    private float[] hardpointTimeInZone = new float[2]; // [0]=p1, [1]=p2
    private int[] hardpointMultiplier = new int[] { 1, 1 }; // x1 x2 x3 x4

    // --- Comentarista ---
    private bool alertEndGameTriggered = false;
    private bool awareChangeZoneTriggered = false;
    private bool humilliateTriggered = false;  // reset al cambiar zona
    private bool comebackTriggered = false;  // reset al cambiar zona
    private int lastLeader = 0;

    private void Start()
    {
        AudioManager.Instance?.PlayMusic(SoundID.Minigame2Music);

        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager"); // crea un game object
            gm.AddComponent<GameManager>(); //al game object se le da el componente gamemanager
        }


        p1Controller = player1.GetComponent<PlatformPlayerController>();
        p2Controller = player2.GetComponent<PlatformPlayerController>();

        p1Controller.SetOtherPlayer(p2Controller);
        p2Controller.SetOtherPlayer(p1Controller);

        // elegir zona ANTES de llamar StartMinigame
        // para que los spawns esten seteados cuando los jugadores se inicializan
        currentZoneIndex = Random.Range(0, zones.Length);

        // setear spawns inmediatamente
        p1Controller.SetSpawnPoint(player1Spawns[currentZoneIndex].position);
        p2Controller.SetSpawnPoint(player2Spawns[currentZoneIndex].position);

        // teleport inmediato antes de que corra cualquier fisica
        player1.transform.position = player1Spawns[currentZoneIndex].position;
        player2.transform.position = player2Spawns[currentZoneIndex].position;

        p1Controller.SetManager(this);
        p2Controller.SetManager(this);

        InicializarMinijuego();
        CongelarJugadores();
    }

    public void InicializarMinijuego()
    {
        gameTimer = gameDuration;
        zoneTimer = zoneChangeDuration;

        // zona ya elegida en Start, solo activar
        ActivateZone(currentZoneIndex, teleport: false);

        hudPlayer1?.TrackPlayer(p1Controller);
        hudPlayer2?.TrackPlayer(p2Controller);
        UpdateUI();

        ResetCommentaryFlags();
        alertEndGameTriggered = false;
    }

    public void IniciarMinijuego()
    {
        FreezePlayers(false);
        gameRunning = true;
    }

    public void CongelarJugadores()
    {
        FreezePlayers(true);
    }

    public void DescongelarJugadores()
    {
        FreezePlayers(false);
    }

    private void Update()
    {
        if (!gameRunning) return;

        gameTimer -= Time.deltaTime;
        zoneTimer -= Time.deltaTime;

        HandleHardPointPoints();
        UpdateCommentaryChecks();

        if (zoneTimer <= 0f)
        {
            Debug.Log("¡Cambio de zona iniciado!");
            StartCoroutine(ChangeZone());
            zoneTimer = zoneChangeDuration;
        }

        if (gameTimer <= 0f)
        {
            gameTimer = 0f;
            EndMinigame();
        }

        UpdateUI();
    }

    private IEnumerator ChangeZone()
    {
        Debug.Log("ChangeZone() iniciado");
        gameRunning = false;
        ResetCommentaryFlags();

        int oldZoneIndex = currentZoneIndex;
        int newZoneIndex;
        do { newZoneIndex = Random.Range(0, zones.Length); }
        while (newZoneIndex == oldZoneIndex && zones.Length > 1);

        Debug.Log("Zona vieja: " + oldZoneIndex + ", Zona nueva: " + newZoneIndex);
        yield return StartCoroutine(PlayZoneTransition(oldZoneIndex, newZoneIndex));

        powerUpEffects.CancelAll(p1Controller, p2Controller);

        currentZoneIndex = newZoneIndex;

        Debug.Log("ChangeZone() terminado, gameRunning = true");
        gameRunning = true;
    }

    private IEnumerator PlayZoneTransition(int oldZoneIndex, int newZoneIndex)
    {
        Debug.Log("PlayZoneTransition() iniciado");
        FreezePlayers(true);

        Debug.Log("SpawnAndMoveHandsToPlayers()");
        yield return StartCoroutine(SpawnAndMoveHandsToPlayers(oldZoneIndex));

        Debug.Log("GrabPlayers()");
        yield return StartCoroutine(GrabPlayers());

        Debug.Log("MoveHandsAwayWithPlayers()");
        yield return StartCoroutine(MoveHandsAwayWithPlayers(oldZoneIndex));

        Debug.Log("FadeToBlack()");
        yield return StartCoroutine(FadeToBlack());

        Debug.Log("MoveCameraToNewZone()");
        yield return StartCoroutine(MoveCameraToNewZone(newZoneIndex));

        Debug.Log("ActivateZone()");
        ActivateZone(newZoneIndex, teleport: false);

        Debug.Log("Iniciando contador + soltar jugadores");
        yield return StartCoroutine(CountdownFadeAndReleasePlayers(newZoneIndex));

        Debug.Log("PlayZoneTransition() terminado");
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
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        bool fadeDone = false;

        bool playersReleased = false;

        StartCoroutine(FadeFromBlackCoroutine(() => fadeDone = true));

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

            if (i == 3)
            {
                StartCoroutine(ReleasePlayersCoroutine(newZoneIndex, () => playersReleased = true));
            }

            if (i > 0)
                yield return new WaitForSeconds(0.5f);
        }



        while (!fadeDone || !playersReleased)
        {
            yield return null;
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        FreezePlayers(false);
    }

    private IEnumerator ReleasePlayersCoroutine(int newZoneIndex, System.Action onComplete)
    {
        if (handSpawnsByZone == null || handSpawnsByZone.Length <= newZoneIndex)
        {
            onComplete?.Invoke();
            yield break;
        }
        if (player1Spawns == null || player1Spawns.Length <= newZoneIndex)
        {
            onComplete?.Invoke();
            yield break;
        }
        if (player2Spawns == null || player2Spawns.Length <= newZoneIndex)
        {
            onComplete?.Invoke();
            yield break;
        }

        ZoneHandSpawns spawns = handSpawnsByZone[newZoneIndex];

        if (handLeft != null && spawns.handLeftSpawn != null)
            handLeft.transform.position = spawns.handLeftSpawn.position;
        if (handRight != null && spawns.handRightSpawn != null)
            handRight.transform.position = spawns.handRightSpawn.position;

        bool leftStretchDone = false;
        bool rightStretchDone = false;

        if (handLeft != null)
            StartCoroutine(StretchHandCoroutine(handLeft, player1Spawns[newZoneIndex].position, () => leftStretchDone = true));
        if (handRight != null)
            StartCoroutine(StretchHandCoroutine(handRight, player2Spawns[newZoneIndex].position, () => rightStretchDone = true));

        while (!leftStretchDone || !rightStretchDone)
        {
            yield return null;
        }

        yield return StartCoroutine(MoveHandsToPositions(
            player1Spawns[newZoneIndex].position,
            player2Spawns[newZoneIndex].position
        ));

        if (handLeft != null)
            handLeft.ReleasePlayer(player1Spawns[newZoneIndex].position);
        if (handRight != null)
            handRight.ReleasePlayer(player2Spawns[newZoneIndex].position);

        bool leftScaleDone = false;
        bool rightScaleDone = false;

        if (handLeft != null)
            StartCoroutine(ReturnHandScaleCoroutine(handLeft, () => leftScaleDone = true));
        if (handRight != null)
            StartCoroutine(ReturnHandScaleCoroutine(handRight, () => rightScaleDone = true));

        while (!leftScaleDone || !rightScaleDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        Animator p1Animator = player1 != null ? player1.GetComponent<Animator>() : null;
        Animator p2Animator = player2 != null ? player2.GetComponent<Animator>() : null;

        if (p1Animator != null)
            p1Animator.SetBool("Surprised", false);
        if (p2Animator != null)
            p2Animator.SetBool("Surprised", false);

        yield return StartCoroutine(MoveHandsToPositions(
            spawns.handLeftSpawn != null ? spawns.handLeftSpawn.position : handLeft.transform.position,
            spawns.handRightSpawn != null ? spawns.handRightSpawn.position : handRight.transform.position
        ));

        if (handLeft != null) handLeft.gameObject.SetActive(false);
        if (handRight != null) handRight.gameObject.SetActive(false);

        onComplete?.Invoke();
    }

    private IEnumerator FadeFromBlackCoroutine(System.Action onComplete)
    {
        if (fadeImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

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

    private IEnumerator SpawnAndMoveHandsToPlayers(int zoneIndex)
    {
        Debug.Log("SpawnAndMoveHandsToPlayers - zoneIndex: " + zoneIndex);
        if (handSpawnsByZone == null || handSpawnsByZone.Length <= zoneIndex)
        {
            Debug.LogWarning("handSpawnsByZone es nulo o no tiene suficientes elementos!");
            yield break;
        }

        Animator p1Animator = player1 != null ? player1.GetComponent<Animator>() : null;
        Animator p2Animator = player2 != null ? player2.GetComponent<Animator>() : null;

        if (p1Animator != null)
            p1Animator.SetBool("Surprised", true);
        if (p2Animator != null)
            p2Animator.SetBool("Surprised", true);

        ZoneHandSpawns spawns = handSpawnsByZone[zoneIndex];
        if (handLeft != null && spawns.handLeftSpawn != null)
        {
            Debug.Log("Activando mano izquierda en: " + spawns.handLeftSpawn.position);
            handLeft.gameObject.SetActive(true);
            handLeft.transform.position = spawns.handLeftSpawn.position;
            handLeft.OpenHand();
        }
        if (handRight != null && spawns.handRightSpawn != null)
        {
            Debug.Log("Activando mano derecha en: " + spawns.handRightSpawn.position);
            handRight.gameObject.SetActive(true);
            handRight.transform.position = spawns.handRightSpawn.position;
            handRight.OpenHand();
        }

        bool leftStretchDone = false;
        bool rightStretchDone = false;

        if (handLeft != null)
            StartCoroutine(StretchHandCoroutine(handLeft, player1.transform.position, () => leftStretchDone = true));
        if (handRight != null)
            StartCoroutine(StretchHandCoroutine(handRight, player2.transform.position, () => rightStretchDone = true));

        while (!leftStretchDone || !rightStretchDone)
        {
            yield return null;
        }

        Debug.Log("Moviendo manos hacia jugadores...");
        yield return StartCoroutine(MoveHandsToPositions(player1.transform.position, player2.transform.position));
        Debug.Log("Manos llegaron a los jugadores");
    }

    private IEnumerator StretchHandCoroutine(HandController hand, Vector3 target, System.Action onComplete)
    {
        if (hand != null)
            yield return StartCoroutine(hand.StretchTowards(target));
        onComplete?.Invoke();
    }

    private IEnumerator MoveHandsToPositions(Vector3 leftTarget, Vector3 rightTarget)
    {
        while (true)
        {
            bool leftDone = true, rightDone = true;

            if (handLeft != null)
            {
                handLeft.transform.position = Vector3.MoveTowards(
                    handLeft.transform.position,
                    leftTarget,
                    handMoveSpeed * Time.deltaTime
                );
                leftDone = Vector3.Distance(handLeft.transform.position, leftTarget) < 0.1f;
            }

            if (handRight != null)
            {
                handRight.transform.position = Vector3.MoveTowards(
                    handRight.transform.position,
                    rightTarget,
                    handMoveSpeed * Time.deltaTime
                );
                rightDone = Vector3.Distance(handRight.transform.position, rightTarget) < 0.1f;
            }

            if (leftDone && rightDone) break;
            yield return null;
        }
    }

    private IEnumerator GrabPlayers()
    {
        if (handLeft != null && !handLeft.HasPlayer)
            handLeft.CloseHand();
        if (handRight != null && !handRight.HasPlayer)
            handRight.CloseHand();

        if (handLeft != null && !handLeft.HasPlayer && player1 != null)
            handLeft.GrabPlayer(player1);
        if (handRight != null && !handRight.HasPlayer && player2 != null)
            handRight.GrabPlayer(player2);

        bool leftScaleDone = false;
        bool rightScaleDone = false;

        if (handLeft != null)
            StartCoroutine(ReturnHandScaleCoroutine(handLeft, () => leftScaleDone = true));
        if (handRight != null)
            StartCoroutine(ReturnHandScaleCoroutine(handRight, () => rightScaleDone = true));

        while (!leftScaleDone || !rightScaleDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator ReturnHandScaleCoroutine(HandController hand, System.Action onComplete)
    {
        if (hand != null)
            yield return StartCoroutine(hand.ReturnToOriginalScale());
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

    private void FreezePlayers(bool freeze)
    {
        if (p1Controller != null)
        {
            var rb1 = p1Controller.GetComponent<Rigidbody2D>();
            if (rb1 != null)
            {
                rb1.linearVelocity = Vector2.zero;
                rb1.constraints = freeze
                    ? RigidbodyConstraints2D.FreezeAll
                    : RigidbodyConstraints2D.FreezeRotation;
            }
            // NO tocar enabled del controller
        }
        if (p2Controller != null)
        {
            var rb2 = p2Controller.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.linearVelocity = Vector2.zero;
                rb2.constraints = freeze
                    ? RigidbodyConstraints2D.FreezeAll
                    : RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    private IEnumerator MoveCameraToNewZone(int zoneIndex)
    {
        if (zoneCamera != null && zones.Length > zoneIndex)
        {
            zoneCamera.SetZoneCenter(zones[zoneIndex].position, zoneIndex);
        }
        yield return new WaitForSeconds(0.5f);
    }

    private void ActivateZone(int index, bool teleport)
    {
        if (teleport)
        {
            player1.transform.position = player1Spawns[index].position;
            player2.transform.position = player2Spawns[index].position;
        }

        p1Controller.SetSpawnPoint(player1Spawns[index].position);
        p2Controller.SetSpawnPoint(player2Spawns[index].position);

        zoneCamera.SetZoneCenter(zones[index].position, index);

        powerUpSpawner.SetActiveZone(index);
        powerUpEffects.SetCurrentZone(index);
    }

    private IEnumerator Flash()
    {
        flashImage.gameObject.SetActive(true);
        float half = flashDuration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / half));
            yield return null;
        }

        flashImage.color = Color.black;
        yield return new WaitForSeconds(0.1f);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(0, 0, 0, Mathf.Clamp01(1f - t / half));
            yield return null;
        }

        flashImage.gameObject.SetActive(false);
    }

    public void ActivatePowerUp(PowerUpPickup.PowerUpType type, PlatformPlayerController user, PlatformPlayerController target)
    {
        switch (type)
        {
            case PowerUpPickup.PowerUpType.Hook:
                StartCoroutine(powerUpEffects.ActivateHook(user, target));
                break;
            case PowerUpPickup.PowerUpType.HeavyGravity:
                StartCoroutine(powerUpEffects.ActivateHeavyGravity(target));
                break;
            case PowerUpPickup.PowerUpType.MirrorControl:
                StartCoroutine(powerUpEffects.ActivateMirrorControl(user, target));
                break;
            case PowerUpPickup.PowerUpType.Crusher:
                StartCoroutine(powerUpEffects.ActivateCrusher(user, target));
                break;

            case PowerUpPickup.PowerUpType.Jetpack:
                StartCoroutine(powerUpEffects.ActivateJetpack(user));
                break;

        }
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

    private void HandleHardPointPoints()
    {
        HardPoint activePoint = hardPoints[currentZoneIndex];
        bool p1Inside = activePoint.IsPlayer1Inside;
        bool p2Inside = activePoint.IsPlayer2Inside;

        //  PUNTOS BASE POR ZONA 
        if (p1Inside && !p2Inside)
        {
            // calcular multiplicador progresivo si aplica
            int mult = GetHardpointMultiplier(1);
            pointAccumulator1 += pointsPerSecond * mult * Time.deltaTime;
            if (pointAccumulator1 >= 1f)
            {
                int pts = Mathf.FloorToInt(pointAccumulator1);
                GameManager.Instance.AddPoints(1, pts);
                pointAccumulator1 -= pts;
            }
            hardpointTimeInZone[0] = 0f; // p2 no esta
        }
        else if (p2Inside && !p1Inside)
        {
            int mult = GetHardpointMultiplier(2);
            pointAccumulator2 += pointsPerSecond * mult * Time.deltaTime;
            if (pointAccumulator2 >= 1f)
            {
                int pts = Mathf.FloorToInt(pointAccumulator2);
                GameManager.Instance.AddPoints(2, pts);
                pointAccumulator2 -= pts;
            }
            hardpointTimeInZone[1] = 0f;
        }

        // acumular tiempo en zona para multiplicador progresivo
        if (p1Inside) hardpointTimeInZone[0] += Time.deltaTime;
        else hardpointTimeInZone[0] = 0f;
        if (p2Inside) hardpointTimeInZone[1] += Time.deltaTime;
        else hardpointTimeInZone[1] = 0f;

        //  POINT BLEED: -1/seg fuera de la zona 
        if (ModifierManager.Instance != null &&
            ModifierManager.Instance.activeKOHModifier == ModifierManager.KOHModifier.PointBleed)
        {
            // p1: solo sangra si esta FUERA y hay alguien adentro
            if (!p1Inside && p2Inside)
            {
                pointBleedTimer1 += Time.deltaTime;
                if (pointBleedTimer1 >= 1f)
                {
                    GameManager.Instance.RemovePoints(1, ModifierManager.Instance.pointBleedAmount);
                    pointBleedTimer1 = 0f;
                }
            }
            else pointBleedTimer1 = 0f;

            // p2: mismo
            if (!p2Inside && p1Inside)
            {
                pointBleedTimer2 += Time.deltaTime;
                if (pointBleedTimer2 >= 1f)
                {
                    GameManager.Instance.RemovePoints(2, ModifierManager.Instance.pointBleedAmount);
                    pointBleedTimer2 = 0f;
                }
            }
            else pointBleedTimer2 = 0f;
        }

        // recalcular comeback si aplica
        if (ModifierManager.Instance != null &&
            ModifierManager.Instance.activeKOHModifier == ModifierManager.KOHModifier.ComebackMultiplier)
            ModifierManager.Instance.RecalculateComebackMultiplier();
    }

    // devuelve el multiplicador de tiempo en zona (1,2,3,4)
    private int GetHardpointMultiplier(int player)
    {
        if (ModifierManager.Instance == null ||
            ModifierManager.Instance.activeKOHModifier != ModifierManager.KOHModifier.ProgressiveHardpoint)
            return 1;

        float time = hardpointTimeInZone[player - 1];
        if (time >= 15f) return 4;
        if (time >= 10f) return 3;
        if (time >= 5f) return 2;
        return 1;
    }

    private void TryComment(CommentTrigger trigger, float chance = 1f)
    {
        if (CommentarySystem.Instance == null) return;
        if (Random.value <= chance)
            CommentarySystem.Instance.TriggerComment(trigger);
    }

    public void OnPlayerDied(bool diedByPunch = false)
    {
        if (!gameRunning) return;

        if (diedByPunch)
            TryComment(CommentTrigger.PunchDeathRival, 0.80f);
        else
            TryComment(CommentTrigger.PlayerDeath, 0.55f);
    }

    public void OnPlayerEnteredZone(int playerIndex)
    {
        if (!gameRunning) return;

        // Solo comenta en ~45 % de los casos para no saturar
        TryComment(CommentTrigger.EnterZone, 0.45f);
    }

    private void UpdateCommentaryChecks()
    {
        if (!gameRunning || GameManager.Instance == null) return;

        int p1 = GameManager.Instance.player1RoundPoints;
        int p2 = GameManager.Instance.player2RoundPoints;

        // AwareChangeZone 
        if (!awareChangeZoneTriggered && zoneTimer <= awareChangeZoneWarning)
        {
            awareChangeZoneTriggered = true;
            TryComment(CommentTrigger.AwareChangeZone, 0.70f);
        }

        // AlertEndGame 
        if (!alertEndGameTriggered && gameTimer <= alertEndGameSeconds)
        {
            alertEndGameTriggered = true;
            TryComment(CommentTrigger.AlertEndGame, 0.85f);
        }

        //  HumilliateRival 
        // Dispara cuando alguien tiene el doble o más de puntos que el otro
        if (!humilliateTriggered)
        {
            bool p1Dominates = p2 > 0 && p1 >= p2 * 2;
            bool p2Dominates = p1 > 0 && p2 >= p1 * 2;

            if (p1Dominates || p2Dominates)
            {
                humilliateTriggered = true;
                TryComment(CommentTrigger.HumilliateRival, 0.75f);
            }
        }

        // Comeback
        // Condición: uno tenía ventaja de comebackMinDiff o más puntos,
        // y el otro lo alcanza (empate) o lo supera.
        //
        // Seguimos quién lideraba con ventaja suficiente en el frame anterior.
        // Si el líder cambia (o hay empate cuando antes había diferencia >= min),
        // es un comeback.
        if (!comebackTriggered)
        {
            int diff = p1 - p2;  // positivo = P1 gana, negativo = P2 gana

            int currentLeader = diff > 0 ? 1 : (diff < 0 ? 2 : 0);

            // Solo actualizamos el lastLeader si la diferencia es suficientemente
            // grande para que "cuente" como ventaja real
            if (Mathf.Abs(diff) >= comebackMinDiff)
            {
                if (lastLeader == 0)
                    lastLeader = currentLeader; // primera vez que alguien saca ventaja
            }

            // Comeback: había un líder claro y ahora el otro lo alcanzó o superó
            if (lastLeader != 0 && currentLeader != lastLeader)
            {
                comebackTriggered = true;
                TryComment(CommentTrigger.Comeback, 0.80f);
            }
        }
    }

    private void ResetCommentaryFlags()
    {
        awareChangeZoneTriggered = false;
        humilliateTriggered = false;
        comebackTriggered = false;
        lastLeader = 0;
        // alertEndGameTriggered NO se resetea (una sola vez por partida)
    }

    // EndMinigame actualizado
    public void EndMinigame()
    {
        gameRunning = false;

        // Null check para GameManager
        if (GameManager.Instance != null)
        {
            var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
            GameManager.Instance.EndRound(2); // id del minijuego
            // guardar para mostrar en Results
            PlayerPrefs.SetInt("LastRoundP1", p1Round);
            PlayerPrefs.SetInt("LastRoundP2", p2Round);
            PlayerPrefs.SetInt("LastPlayedMinigame", 2);
        }

        // Buscar SceneLoader si Instance es nulo
        if (SceneLoader.Instance == null)
        {
            SceneLoader.Instance = FindAnyObjectByType<SceneLoader>();
        }

        // Null check para SceneLoader antes de llamar LoadResults
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadResults();
        }
        else
        {
            Debug.LogError("SceneLoader.Instance es nulo y no se encontró en la escena!");
        }
    }
}