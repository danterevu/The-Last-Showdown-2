using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MutantDNAManager : MonoBehaviour
{
    [Header("Configuraci�n")]
    [SerializeField] private float gameDuration = 120f;

    [Header("Jugadores")]
    [SerializeField] private GameObject player1;   private PlayerControllerDNA p1Controller;
    [SerializeField] private GameObject player2;   private PlayerControllerDNA p2Controller;

    [Header("DNA")]
    [SerializeField] private DNA dnaPickup;

    [Header("Dep�sitos")]
    [SerializeField] private Deposit deposit1; // el que solo acepta P1
    [SerializeField] private Deposit deposit2; // el que solo acepta P2

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;
    [SerializeField] private TextMeshProUGUI countdownText;

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
        StartCoroutine(InitialCountdown());
    }

    private IEnumerator InitialCountdown()
    {
        FreezePlayers(true);
        gameRunning = false;

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
        gameRunning = true;
        dnaPickup.SpawnDNA();
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
        if (p1Controller != null)
        {
            p1Controller.SetFrozen(freeze);
        }
        if (p2Controller != null)
        {
            p2Controller.SetFrozen(freeze);
        }
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

        //UpdateUI();
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
        PlayerPrefs.SetInt("LastPlayedMinigame", 3);
        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }
}