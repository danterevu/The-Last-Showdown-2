using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Tema visual")]
    [SerializeField] private HUDTheme theme;
    [SerializeField] private Image hudColorImage;

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timerWarningTime = 20f;
    [SerializeField] private float timerDangerTime = 10f;
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = Color.yellow;
    [SerializeField] private Color timerDangerColor = Color.red;

    [Header("Personajes HUD")]
    [SerializeField] private Animator character1Animator;
    [SerializeField] private Animator character2Animator;

    [Header("Animacion del score")]
    [SerializeField] private float countDuration = 0.4f;
    [SerializeField] private float punchScale = 1.4f;
    [SerializeField] private float punchDuration;
    [SerializeField] private Color addColor = Color.green;
    [SerializeField] private Color removeColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private int displayedScore1;
    private int displayedScore2;

    private Vector3 score1OriginalScale;
    private Vector3 score2OriginalScale;

    private Coroutine countCoroutine1;
    private Coroutine countCoroutine2;
    private Coroutine punchCoroutine1;
    private Coroutine punchCoroutine2;
    private Coroutine timerCoroutine;

    private int timerState = 0;
    // Ciclo de vida
    private void OnEnable()
    {
        GameManager.OnPointsChanged += HandlePointsChanged;
    }
    private void OnDisable()
    {
        GameManager.OnPointsChanged -= HandlePointsChanged;
    }
   
    void Start()
    {
     if(GameManager.Instance!= null) 
        {
            displayedScore1 = GameManager.Instance.player1RoundPoints;
            displayedScore2 = GameManager.Instance.player2RoundPoints;
        }
        ApplyTheme();
        UpdateTexts();
        if (player1ScoreText != null) score1OriginalScale = player1ScoreText.transform.localScale;
        if (player2ScoreText != null) score2OriginalScale = player2ScoreText.transform.localScale;
        timerState = -1; 
    }
    private void HandlePointsChanged(int player, int amount, bool isAdd)
    {
        if (player == 1)
        {
            int target = GameManager.Instance.player1RoundPoints;
            if (countCoroutine1 != null) StopCoroutine(countCoroutine1);
            if (punchCoroutine1 != null) StopCoroutine(punchCoroutine1);
            countCoroutine1 = StartCoroutine(CountScore(1, displayedScore1, target));
            punchCoroutine1 = StartCoroutine(PunchText(player1ScoreText, isAdd)); 
        }
        else
        {
            int target = GameManager.Instance.player2RoundPoints;
            if (countCoroutine2 != null) StopCoroutine(countCoroutine2);
            if (punchCoroutine2 != null) StopCoroutine(punchCoroutine2);
            countCoroutine2 = StartCoroutine(CountScore(2, displayedScore2, target));
            punchCoroutine2 = StartCoroutine(PunchText(player2ScoreText, isAdd));
        }
        if (isAdd) NotifyCharacterCelebrate(player);
        else NotifyCharacterTaunt(player == 1 ? 2 : 1);

    }
    private void ApplyTheme()
    {
        if (theme == null) return;
        if (hudColorImage != null) hudColorImage.color = theme.hudColor;
        if(player1ScoreText != null) player1ScoreText.color = theme.player1TextColor; 
        if(player2ScoreText!= null) player2ScoreText.color = theme.player2TextColor; 
        if (timerText != null) timerNormalColor = theme.timerTextColor;
    }

    private IEnumerator CountScore(int player, int from, int to)
    {
        float elapsed = 0f;
        while (elapsed < countDuration)
        {
            float t = elapsed / countDuration;
                int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            if (player == 1) displayedScore1 = current;
            else displayedScore2 = current;
            UpdateTexts();
            elapsed += Time.deltaTime;
            yield return null;
        }
        if(player == 1) { displayedScore1 = to; countCoroutine1 = null; }
        else
        {
            displayedScore2 = to; countCoroutine2 = null;
        }
        UpdateTexts();
    }
    private IEnumerator PunchText(TextMeshProUGUI text,bool isAdd) 
    {

        if (text == null) yield break;


        Vector3 originalScale = text == player1ScoreText ? score1OriginalScale : score2OriginalScale;
        Color targetColor = isAdd ? addColor : removeColor;
        text.color = targetColor;

        if(!isAdd)
        {
            float shakeElapsed = 0f;
            float shakeDur = 0.3f;
            Vector3 originalPos = text.transform.localPosition;
            while(shakeElapsed<shakeDur)
            {
                text.transform.localPosition = originalPos + new Vector3(Random.Range(-4f, 4f),0f, 0f);
                shakeElapsed += Time.deltaTime;
                yield return null;
            }
            text.transform.localPosition = originalPos;
        }
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            float t = elapsed / punchDuration;
            text.transform.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < punchDuration)
        {
            float t = elapsed / punchDuration;
            text.transform.localScale = Vector3.Lerp(originalScale * punchScale, originalScale, t);
            text.color = Color.Lerp(targetColor, normalColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.transform.localScale = originalScale;
        text.color = normalColor;
    }
    //parte del timer
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;


        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        //Debug.Log("time: " + timeRemaining + " warning: " + timerWarningTime + " danger: " + timerDangerTime + " state: " + timerState);

        if (timeRemaining <= timerDangerTime)
            if (timeRemaining <= timerDangerTime)
        {
            if (timerState != 2)
            {
                timerState = 2;
                if (timerCoroutine != null) StopCoroutine(timerCoroutine);
                timerCoroutine = StartCoroutine(TimerPulse(timerDangerColor, 0.3f));
            }
        }
        else if (timeRemaining <= timerWarningTime)
        {
            if (timerState != 1)
            {
                timerState = 1;
                if (timerCoroutine != null) StopCoroutine(timerCoroutine);
                timerCoroutine = StartCoroutine(TimerPulse(timerWarningColor, 0.6f));
            }
        }
        else
        {
            if (timerState != 0)
            {
                timerState = 0;
                if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
                timerText.color = timerNormalColor;
            }
        }
    }
    private IEnumerator TimerPulse(Color pulseColor, float speed)
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * (1f / speed) * Mathf.PI) + 1f) / 2f;
            if (timerText != null)
                timerText.color = Color.Lerp(timerNormalColor, pulseColor, t);
            yield return null;
        }
    }
    public void StopTimerPulse()
    {
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        if (timerText != null) timerText.color = timerNormalColor;

    }
    //Interaciones de los personajes
    public void NotifyCharacterCelebrate(int player)
    {
        Animator anim = player == 1 ? character1Animator : character2Animator;
        if (anim != null) anim.SetTrigger("Celebrate");
    }
    public void NotifyCharacterTaunt(int player)
    {
        Animator anim = player == 1 ? character1Animator : character2Animator;
        if (anim != null) anim.SetTrigger("Taunt");
    }
    public void NotifyCharacterWin(int player)
    {
        Animator anim = player == 1 ? character1Animator : character2Animator;
        if (anim != null) anim.SetTrigger("Win");
    }
        public void NotifyCharacterLose(int player)
    {
        Animator anim = player == 1 ? character1Animator : character2Animator;
        if (anim != null) anim.SetTrigger("Lose");
    }
    // extra
    private void UpdateTexts()
    {
        if (player1ScoreText != null) player1ScoreText.text = " " + displayedScore1;
        if (player2ScoreText != null) player2ScoreText.text = " " + displayedScore2;
    }
}


