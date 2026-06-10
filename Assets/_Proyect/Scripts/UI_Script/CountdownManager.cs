using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

public class CountdownManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Configuración del contador")]
    [SerializeField] private int startNumber = 3;
    [SerializeField] private float durationPerNumber = 0.5f;
    [SerializeField] private string finalText = "Ya!";

    [Header("Animación")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float scaleMultiplier = 1.3f;
    [SerializeField] private float shakeAmount = 10f;

    [Header("Evento")]
    public UnityEngine.Events.UnityEvent onCountdownComplete;

    private Coroutine currentCountdownCoroutine;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private void Awake()
    {
        if (countdownText != null)
        {
            originalScale = countdownText.transform.localScale;
            originalPosition = countdownText.transform.localPosition;
        }
    }

    public void StartCountdown()
    {
        // Asegurarnos que el GameObject esté activo
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        if (currentCountdownCoroutine != null)
        {
            StopCoroutine(currentCountdownCoroutine);
        }

        currentCountdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public void StopCountdown()
    {
        if (currentCountdownCoroutine != null)
        {
            StopCoroutine(currentCountdownCoroutine);
            currentCountdownCoroutine = null;
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        Debug.Log("CountdownCoroutine INICIADO");
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.transform.localScale = originalScale;
            countdownText.transform.localPosition = originalPosition;
        }

        for (int i = startNumber; i >= 0; i--)
        {
            if (countdownText != null)
            {
                if (i > 0)
                {
                    countdownText.text = i.ToString();

                    if (useAnimation)
                    {
                        yield return StartCoroutine(AnimateCountdownText());
                    }
                    else
                    {
                        yield return new WaitForSeconds(durationPerNumber);
                    }
                }
                else
                {
                    countdownText.text = finalText;
                }
            }
            else if (i == 0)
            {
                yield return null;
            }

            if (i > 0 && !useAnimation)
            {
                yield return new WaitForSeconds(durationPerNumber);
            }
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        onCountdownComplete?.Invoke();
        Debug.Log("onCountdownComplete INVOCADO");
    }

    private IEnumerator AnimateCountdownText()
    {
        float duration = durationPerNumber;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;

            float scale = Mathf.Lerp(1f, scaleMultiplier, Mathf.PingPong(progress * 2, 1f));
            countdownText.transform.localScale = originalScale * scale;

            float shakeX = Random.Range(-shakeAmount, shakeAmount) * (1f - progress);
            float shakeY = Random.Range(-shakeAmount, shakeAmount) * (1f - progress);
            countdownText.transform.localPosition = originalPosition + new Vector3(shakeX, shakeY, 0f);

            yield return null;
        }

        countdownText.transform.localScale = originalScale;
        countdownText.transform.localPosition = originalPosition;
    }

    // Métodos públicos para configurar desde el Inspector o eventos
    public void SetStartNumber(int number)
    {
        startNumber = number;
    }

    public void SetFinalText(string text)
    {
        finalText = text;
    }

    public void SetDurationPerNumber(float duration)
    {
        durationPerNumber = duration;
    }
}
