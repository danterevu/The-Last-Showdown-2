using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;   // <-- NUEVO

public class MutantDNAHUD : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Power-Up Icons")] 
    [SerializeField] private Image player1PowerUpIcon;   // <-- NUEVO
    [SerializeField] private Image player2PowerUpIcon;   // <-- NUEVO
    [SerializeField] private Sprite berserkSpriteP1;
    [SerializeField] private Sprite berserkSpriteP2;
    [SerializeField] private Sprite shrinkSpriteP1;
    [SerializeField] private Sprite shrinkSpriteP2;
    [SerializeField] private Sprite mineSpriteP1;
    [SerializeField] private Sprite mineSpriteP2;
    [SerializeField] private Sprite remoteSpriteP1;
    [SerializeField] private Sprite remoteSpriteP2;
    [SerializeField] private Sprite slimeSpriteP1;
    [SerializeField] private Sprite slimeSpriteP2;

    [Header("Tema visual (colores)")]
    [SerializeField] private Color player1Color = Color.blue;
    [SerializeField] private Color player2Color = Color.red;
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = Color.yellow;
    [SerializeField] private Color timerDangerColor = Color.red;
    [SerializeField] private float warningTime = 20f;
    [SerializeField] private float dangerTime = 10f;

    [Header("Animación de puntos")]
    [SerializeField] private float countDuration = 0.4f;
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.15f;
    [SerializeField] private Color pointGainColor = Color.green;
    [SerializeField] private Color pointLossColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Animadores de los personajes (opcional)")]
    [SerializeField] private Animator character1Anim;
    [SerializeField] private Animator character2Anim;

    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownDuration = 0.5f;
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private float scaleMultiplier = 1.3f;

    // Variables internas
    private int displayedScore1, displayedScore2;
    private Coroutine scoreAnimCoroutine1, scoreAnimCoroutine2;
    private Coroutine timerPulseCoroutine;
    private int lastTimerState = -1;

    private PlayerControllerDNA player1;   // <-- NUEVO
    private PlayerControllerDNA player2;   // <-- NUEVO

    private void OnEnable()
    {
        // Suscripción a eventos estáticos
        PlayerControllerDNA.OnPowerUpGained += HandlePowerUpGained;
        PlayerControllerDNA.OnPowerUpUsed += HandlePowerUpUsed;
    }
    private void OnDisable()
    {
        PlayerControllerDNA.OnPowerUpGained -= HandlePowerUpGained;
        PlayerControllerDNA.OnPowerUpUsed -= HandlePowerUpUsed;
    }
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            displayedScore1 = GameManager.Instance.player1RoundPoints;
            displayedScore2 = GameManager.Instance.player2RoundPoints;
            UpdateScoreTexts();
        }

        GameManager.OnPointsChanged += HandlePointsChanged;

        // Ocultar íconos al inicio
        if (player1PowerUpIcon != null) player1PowerUpIcon.gameObject.SetActive(false);
        if (player2PowerUpIcon != null) player2PowerUpIcon.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.OnPointsChanged -= HandlePointsChanged;
    }

    private void UpdateScoreTexts()
    {
        if (player1ScoreText != null) player1ScoreText.text = displayedScore1.ToString();
        if (player2ScoreText != null) player2ScoreText.text = displayedScore2.ToString();
    }

    public void StartCountdown(System.Action onComplete)
    {
        StartCoroutine(InitialCountdownCoroutine(onComplete));
    }

    private IEnumerator InitialCountdownCoroutine(System.Action onComplete)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i >= 0; i--)
            {
                if (i > 0)
                    countdownText.text = i.ToString();
                else
                    countdownText.text = "¡Ya!";

                yield return StartCoroutine(AnimateCountdownTextCoroutine(countdownText));

                if (i > 0)
                    yield return new WaitForSeconds(countdownDuration);
            }
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(countdownDuration * 3);
        }

        onComplete?.Invoke();
    }

    private IEnumerator AnimateCountdownTextCoroutine(TextMeshProUGUI text)
    {
        Vector3 originalScale = text.transform.localScale;
        Vector3 originalPos = text.transform.localPosition;
        float duration = 0.5f;

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

    private void HandlePointsChanged(int player, int amount, bool isAdd)
    {
        if (player == 1)
        {
            if (scoreAnimCoroutine1 != null) StopCoroutine(scoreAnimCoroutine1);
            int newScore = GameManager.Instance.player1RoundPoints;
            scoreAnimCoroutine1 = StartCoroutine(AnimateScoreChange(player1ScoreText, displayedScore1, newScore, isAdd, player));
            displayedScore1 = newScore;
            if (character1Anim != null) character1Anim.SetTrigger(isAdd ? "Win" : "Lose");
        }
        else if (player == 2)
        {
            if (scoreAnimCoroutine2 != null) StopCoroutine(scoreAnimCoroutine2);
            int newScore = GameManager.Instance.player2RoundPoints;
            scoreAnimCoroutine2 = StartCoroutine(AnimateScoreChange(player2ScoreText, displayedScore2, newScore, isAdd, player));
            displayedScore2 = newScore;
            if (character2Anim != null) character2Anim.SetTrigger(isAdd ? "Win" : "Lose");
        }
    }

    private IEnumerator AnimateScoreChange(TextMeshProUGUI text, int from, int to, bool isAdd, int player)
    {
        if (text != null)
        {
            StartCoroutine(PunchTextEffect(text, isAdd));
        }

        float elapsed = 0f;
        int current = from;
        while (elapsed < countDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / countDuration;
            current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            if (text != null) text.text = current.ToString();
            yield return null;
        }
        if (text != null) text.text = to.ToString();
    }

    private IEnumerator PunchTextEffect(TextMeshProUGUI text, bool isAdd)
    {
        if (text == null) yield break;
        Vector3 originalScale = text.transform.localScale;
        Color targetColor = isAdd ? pointGainColor : pointLossColor;
        Color originalColor = text.color;

        text.color = targetColor;

        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            float t = elapsed / punchDuration;
            text.transform.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.transform.localScale = originalScale * punchScale;

        elapsed = 0f;
        while (elapsed < punchDuration)
        {
            float t = elapsed / punchDuration;
            text.transform.localScale = Vector3.Lerp(originalScale * punchScale, originalScale, t);
            text.color = Color.Lerp(targetColor, originalColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.transform.localScale = originalScale;
        text.color = originalColor;
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        int newState = 0;
        if (timeRemaining <= dangerTime) newState = 2;
        else if (timeRemaining <= warningTime) newState = 1;

        if (newState != lastTimerState)
        {
            lastTimerState = newState;
            if (timerPulseCoroutine != null) StopCoroutine(timerPulseCoroutine);
            if (newState == 0)
            {
                timerText.color = timerNormalColor;
            }
            else
            {
                Color pulseColor = (newState == 1) ? timerWarningColor : timerDangerColor;
                timerPulseCoroutine = StartCoroutine(TimerPulseEffect(pulseColor));
            }
        }
    }

    private IEnumerator TimerPulseEffect(Color targetColor)
    {
        Color originalColor = timerNormalColor;
        while (true)
        {
            float t = (Mathf.Sin(Time.time * 3f) + 1f) / 2f;
            timerText.color = Color.Lerp(originalColor, targetColor, t);
            yield return null;
        }
    }

    public void StopTimerEffects()
    {
        if (timerPulseCoroutine != null)
        {
            StopCoroutine(timerPulseCoroutine);
            timerPulseCoroutine = null;
        }
        if (timerText != null) timerText.color = timerNormalColor;
    }

    private void HandlePowerUpGained(PlayerControllerDNA player, DNAPowerUpPickup.DNAPowerUpType type)
    {
        bool isPlayer1 = player.CompareTag("Player1");
        Image targetIcon = isPlayer1 ? player1PowerUpIcon : player2PowerUpIcon;
        if (targetIcon == null) return;

        Sprite selectedSprite = null;

        switch (type)
        {
            case DNAPowerUpPickup.DNAPowerUpType.Berserk:
                selectedSprite = isPlayer1 ? berserkSpriteP1 : berserkSpriteP2;
                break;
            case DNAPowerUpPickup.DNAPowerUpType.Shrink:
                selectedSprite = isPlayer1 ? shrinkSpriteP1 : shrinkSpriteP2;
                break;
            case DNAPowerUpPickup.DNAPowerUpType.Mine:
                selectedSprite = isPlayer1 ? mineSpriteP1 : mineSpriteP2;
                break;
            case DNAPowerUpPickup.DNAPowerUpType.RemoteControl:
                selectedSprite = isPlayer1 ? remoteSpriteP1 : remoteSpriteP2;
                break;
            case DNAPowerUpPickup.DNAPowerUpType.SlimeShot:
                selectedSprite = isPlayer1 ? slimeSpriteP1 : slimeSpriteP2;
                break;
            default:
                return;
        }

        if (selectedSprite != null)
        {
            targetIcon.sprite = selectedSprite;
            targetIcon.gameObject.SetActive(true);
        }
    }

    private void HandlePowerUpUsed(PlayerControllerDNA player)
    {
        bool isPlayer1 = player.CompareTag("Player1");
        Image targetIcon = isPlayer1 ? player1PowerUpIcon : player2PowerUpIcon;
        if (targetIcon != null) targetIcon.gameObject.SetActive(false);
    }
}