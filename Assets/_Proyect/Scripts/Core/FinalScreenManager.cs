using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalScreenManager : MonoBehaviour
{
    [Header("Resultado")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI loserScoreText;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Personajes arriba")]
    [SerializeField] private GameObject gloppyWinner;
    [SerializeField] private GameObject choppyWinner;
    [SerializeField] private GameObject gloppyLoser;
    [SerializeField] private GameObject choppyLoser;

    [Header("Personajes abajo")]
    [SerializeField] private GameObject gloppyLoserBottom;
    [SerializeField] private GameObject choppyLoserBottom;
    [SerializeField] private GameObject cinematicPresenter;
    [SerializeField] private GameObject cinematicPresenterHand;
    [SerializeField] private GameObject cinematicPresenterHandTie; 

    [Header("Animators ganador")]
    [SerializeField] private Animator gloppyWinnerAnimator;
    [SerializeField] private Animator choppyWinnerAnimator;

    [Header("Animators perdedor")]
    [SerializeField] private Animator gloppyLoserAnimator;
    [SerializeField] private Animator choppyLoserAnimator;
    private Animator activeLoserAnimator;

    [Header("Panel intro")]
    [SerializeField] private GameObject blackPanel;

    [Header("Camara")]
    [SerializeField] private Transform cameraDestination;
    [SerializeField] private float cameraDuration = 2f;
    [SerializeField] private string cinematicTitleText = "Y el perdedor es...";
    [SerializeField] private string cinematicTitleTextTie = "Para estos empates...";

    [Header("UI secuencia")]
    [SerializeField] private GameObject winnerScoreObject;
    [SerializeField] private GameObject loserScoreObject;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private float delayBetweenElements = 0.4f;

    [Header("Fade cinemática")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private int debugP1Score = 3;
    [SerializeField] private int debugP2Score = 1;

    private bool isPlayer1Winner;
    private bool isTie;

    private void Start()
    {
        int p1, p2;

        if (debugMode || GameManager.Instance == null)
        {
            p1 = debugP1Score;
            p2 = debugP2Score;
        }
        else
        {
            p1 = GameManager.Instance.player1Score;
            p2 = GameManager.Instance.player2Score;
        }

        isTie = p1 == p2;
        isPlayer1Winner = p1 > p2;

        // Desactivar todo al inicio
        winnerScoreObject.SetActive(false);
        loserScoreObject.SetActive(false);
        continueButton.SetActive(false);

        gloppyWinner.SetActive(false);
        choppyWinner.SetActive(false);
        gloppyLoser.SetActive(false);
        choppyLoser.SetActive(false);

        SetAlpha(gloppyLoserBottom, 0f);
        SetAlpha(choppyLoserBottom, 0f);
        SetAlpha(cinematicPresenter, 0f);
        SetAlpha(cinematicPresenterHand, 0f);

        gloppyLoserBottom.SetActive(false);
        choppyLoserBottom.SetActive(false);
        cinematicPresenter.SetActive(false);
        cinematicPresenterHand.SetActive(false);

        if (isTie)
        {
            winnerText.text = $"¡Empate!\n{p1}";
            loserScoreText.text = "";
        }
        else if (isPlayer1Winner)
        {
            winnerText.text = $"¡ Gloppy!\n{p1}";
            loserScoreText.text = $"Choppy: {p2}";
        }
        else
        {
            winnerText.text = $"¡ Choppy!\n{p2}";
            loserScoreText.text = $"Gloppy: {p1}";
        }
    }

    public void OnIntroFinished()
    {
        blackPanel.SetActive(false);

        if (isTie)
        {
            gloppyWinner.SetActive(true);
            choppyWinner.SetActive(true);
        }
        else if (isPlayer1Winner)
        {
            gloppyWinner.SetActive(true);
            choppyLoser.SetActive(true);
            activeLoserAnimator = choppyLoserAnimator;
        }
        else
        {
            choppyWinner.SetActive(true);
            gloppyLoser.SetActive(true);
            activeLoserAnimator = gloppyLoserAnimator;
        }

        StartCoroutine(ShowResultsSequence());
    }

    private IEnumerator ShowResultsSequence()
    {
        yield return StartCoroutine(PunchAppear(winnerScoreObject));
        yield return new WaitForSeconds(delayBetweenElements);

        yield return StartCoroutine(PunchAppear(loserScoreObject));
        yield return new WaitForSeconds(delayBetweenElements);

        yield return StartCoroutine(PunchAppear(continueButton));
    }

    private IEnumerator PunchAppear(GameObject target)
    {
        target.SetActive(true);
        Transform t = target.transform;
        t.localScale = Vector3.zero;

        float elapsed = 0f;
        float halfDuration = punchDuration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, punchScale, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(punchScale, 1f, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    private IEnumerator PunchDisappear(GameObject target)
    {
        Transform t = target.transform;
        float elapsed = 0f;
        float halfDuration = punchDuration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, punchScale, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(punchScale, 0f, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.zero;
        target.SetActive(false);
    }

    public void OnContinueButton()
    {
        continueButton.GetComponent<Button>().interactable = false;
        StartCoroutine(MoveCameraAndStartCinematic());
    }

    private IEnumerator MoveCameraAndStartCinematic()
    {
        if (titleText != null)
            titleText.text = isTie ? cinematicTitleTextTie : cinematicTitleText;

        StartCoroutine(PunchDisappear(winnerScoreObject));
        StartCoroutine(PunchDisappear(loserScoreObject));

        Camera cam = Camera.main;
        Vector3 startPos = cam.transform.position;
        Vector3 endPos = new Vector3(cameraDestination.position.x, cameraDestination.position.y, cam.transform.position.z);

        float elapsed = 0f;
        while (elapsed < cameraDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraDuration);
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        cam.transform.position = endPos;
        StartCoroutine(FadeInBottomObjects());
    }

    private IEnumerator FadeInBottomObjects()
    {
        cinematicPresenter.SetActive(true);
        cinematicPresenterHand.SetActive(true);

        if (isTie)
        {
            cinematicPresenterHand.SetActive(false);
            // En empate se activan los dos losers y los dos winners abajo
            gloppyLoserBottom.SetActive(true);
            choppyLoserBottom.SetActive(true);

            // Worried a los dos ganadores
            if (gloppyWinnerAnimator != null)
                gloppyWinnerAnimator.SetTrigger("Worried");
            if (choppyWinnerAnimator != null)
                choppyWinnerAnimator.SetTrigger("Worried");

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(gloppyLoserBottom, alpha);
                SetAlpha(choppyLoserBottom, alpha);
                SetAlpha(cinematicPresenter, alpha);
                SetAlpha(cinematicPresenterHand, alpha);
                yield return null;
            }

            SetAlpha(gloppyLoserBottom, 1f);
            SetAlpha(choppyLoserBottom, 1f);
        }
        else
        {
            cinematicPresenterHand.SetActive(true);
            GameObject activeLoserBottom = isPlayer1Winner ? choppyLoserBottom : gloppyLoserBottom;
            activeLoserBottom.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(activeLoserBottom, alpha);
                SetAlpha(cinematicPresenter, alpha);
                SetAlpha(cinematicPresenterHand, alpha);
                yield return null;
            }

            SetAlpha(activeLoserBottom, 1f);
        }

        SetAlpha(cinematicPresenter, 1f);
        SetAlpha(cinematicPresenterHand, 1f);

        Animator presenterAnim = cinematicPresenter.GetComponent<Animator>();
        if (presenterAnim != null)
            presenterAnim.enabled = true;
    }

    private void SetAlpha(GameObject target, float alpha)
    {
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    public void OnLoserWorried()
    {
        if (activeLoserAnimator != null)
            activeLoserAnimator.SetTrigger("Worried");
    }

    public void OnLoserGrabbed()
    {
        if (activeLoserAnimator != null)
            activeLoserAnimator.SetTrigger("Grabbed");
    }

    public void OnReturnToMenu()
    {
        GameManager.Instance.ResetGame();
        SceneLoader.Instance.LoadMenu();
    }
}