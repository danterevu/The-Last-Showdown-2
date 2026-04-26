using System.Collections;
using UnityEngine;

public class AudienceAnimator : MonoBehaviour
{
    [Header("Movimiento constante")]
    [SerializeField] private float normalHeight = 0.0f;
    [SerializeField] private float normalSpeed = 2f;
    [SerializeField] private float normalStretchY = 1.08f;

    [Header("Celebracion")]
    [SerializeField] private float hypeHeight = 0.35f;
    [SerializeField] private float hypeSpeed = 5f;
    [SerializeField] private float hypeStretchY = 1.3f;
    [SerializeField] private float hypeDuration = 1.5f;

    private Vector3 originalPos;
    private Vector3 originalScale;

    private float currentHeight;
    private float currentSpeed;
    private float currentStretchY;

    private Coroutine hypeCoroutine;
    void Start()
    {
        originalPos = transform.localPosition;
        originalScale = transform.localScale;

        currentHeight = normalHeight;
        currentSpeed = normalSpeed;
        currentStretchY = normalStretchY;

        StartCoroutine(BounceLoop());
    }
    private IEnumerator BounceLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.4f));
        while(true)
        {
            float t = (Mathf.Sin(Time.time * currentSpeed) + 1f) / 2f; // 0 a 1

            float yOffset = t * currentHeight;
            float scaleY = Mathf.Lerp(originalScale.y, originalScale.y * currentStretchY, t);

            transform.localPosition = new Vector3(originalPos.x, originalPos.y + yOffset, originalPos.z);
            transform.localScale= new Vector3(originalScale.x, scaleY, originalScale.z);

            yield return null;
        }

    }
    
    public void Hype()
    {
        if (hypeCoroutine != null) StopCoroutine(hypeCoroutine);
        hypeCoroutine = StartCoroutine(HypeSequence());
    }
    private IEnumerator HypeSequence()
    {
        float elapsed = 0f;
        float transitionIn = 0.1f;

        while(elapsed<transitionIn)
        {
            float t = elapsed / transitionIn;
            currentHeight = Mathf.Lerp(normalHeight, hypeHeight, t);
            currentSpeed = Mathf.Lerp(normalSpeed, hypeSpeed, t);
            currentStretchY = Mathf.Lerp(normalStretchY, hypeStretchY, t);
            elapsed += Time.deltaTime;
            yield return null;

        }
        yield return new WaitForSeconds(hypeDuration);

        elapsed = 0f;
        float transitionOut = 0.5f;

        while (elapsed < transitionOut)
        {
            float t = elapsed / transitionOut;
            currentHeight = Mathf.Lerp(hypeHeight, normalHeight, t);
            currentSpeed = Mathf.Lerp(hypeSpeed, normalSpeed, t);
            currentStretchY = Mathf.Lerp(hypeStretchY, normalStretchY, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentHeight = normalHeight;
        currentSpeed = normalSpeed;
        currentStretchY = normalStretchY;
        hypeCoroutine = null;

    }
}
