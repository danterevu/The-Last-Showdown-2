using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

    //[SerializeField] private float shakeDuration = 0.1f;
    //[SerializeField] private float shakeMagnitude = 0.08f;
    private void Awake()
    {
        if(Instance != null && Instance != this)
                    {
            Destroy(gameObject);
            return;
        }   
        Instance = this;

    }
    
    void Start()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    } 
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float damptedMagnitude = magnitude * (1f - progress);

            Vector2 offset = Random.insideUnitCircle * damptedMagnitude;
            transform.localPosition = originalLocalPos + new Vector3(offset.x, offset.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;

    }
   
}
