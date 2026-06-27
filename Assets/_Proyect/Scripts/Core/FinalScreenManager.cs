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

    [Header("Animators perdedor")]
    [SerializeField] private Animator gloppyLoserAnimator;
    [SerializeField] private Animator choppyLoserAnimator;
    private Animator activeLoserAnimator;

    [Header("Panel intro")]
    [SerializeField] private GameObject blackPanel;

    [Header("Camara")]
    [SerializeField] private Transform cameraDestination;
    [SerializeField] private float cameraDuration = 2f;

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

        isPlayer1Winner = p1 >= p2;

        // Desactivar todo al inicio
        winnerScoreObject.SetActive(false);
        loserScoreObject.SetActive(false);
        continueButton.SetActive(false);

        // Desactivar todos los personajes arriba
        gloppyWinner.SetActive(false);
        choppyWinner.SetActive(false);
        gloppyLoser.SetActive(false);
        choppyLoser.SetActive(false);

        // Desactivar todos los objetos de abajo
        SetAlpha(gloppyLoserBottom, 0f);
        SetAlpha(choppyLoserBottom, 0f);
        SetAlpha(cinematicPresenter, 0f);
        SetAlpha(cinematicPresenterHand, 0f);

        gloppyLoserBottom.SetActive(false);
        choppyLoserBottom.SetActive(false);
        cinematicPresenter.SetActive(false);
        cinematicPresenterHand.SetActive(false);

        if (p1 > p2)
        {
            winnerText.text = $"¡Ganó Gloppy!\n{p1}";
            loserScoreText.text = $"Choppy: {p2}";
        }
        else if (p2 > p1)
        {
            winnerText.text = $"¡Ganó Choppy!\n{p2}";
            loserScoreText.text = $"Gloppy: {p1}";
        }
        else
        {
            winnerText.text = $"¡Empate!\n{p1}";
            loserScoreText.text = "";
        }
    }

    // Llamado por Animation Event del presentador intro
    public void OnIntroFinished()
    {
        blackPanel.SetActive(false);

        if (isPlayer1Winner)
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

    public void OnContinueButton()
    {
        continueButton.GetComponent<Button>().interactable = false;
        StartCoroutine(MoveCameraAndStartCinematic());
    }
  
    private IEnumerator MoveCameraAndStartCinematic()
    {
        if (titleText != null)
            titleText.text = titleText.text;
        Camera cam = Camera.main;
     
        StartCoroutine(PunchDisappear(winnerScoreObject));
        StartCoroutine(PunchDisappear(loserScoreObject));
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
        // Activar solo el perdedor correcto abajo
        GameObject activeLoserBottom;
        if (isPlayer1Winner)
        {
            choppyLoserBottom.SetActive(true);
            activeLoserBottom = choppyLoserBottom;
            gloppyLoserBottom.SetActive(false);
        }
        else
        {
            gloppyLoserBottom.SetActive(true);
            activeLoserBottom = gloppyLoserBottom;
            choppyLoserBottom.SetActive(false);
        }

        cinematicPresenter.SetActive(true);
        cinematicPresenterHand.SetActive(true);

        // Fade in de todos los objetos de abajo juntos
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
        SetAlpha(cinematicPresenter, 1f);
        SetAlpha(cinematicPresenterHand, 1f);

        // Activar animator del presentador cinematico
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
    private IEnumerator PunchDisappear(GameObject target)
    {
        Transform t = target.transform;
        float elapsed = 0f;
        float halfDuration = punchDuration * 0.5f;

        // Scale up leve
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, punchScale, elapsed / halfDuration);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;

        // Scale down a cero
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
}