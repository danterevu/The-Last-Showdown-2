using System.Collections;
using UnityEngine;

public class RepulsionCircleHandler : MonoBehaviour
{
    private int ownerPlayer;
    private float radius;
    private Color targetColor;
    private float fadeInTime;
    private float totalDuration;
    private SpriteRenderer sr;
    private bool hasSprite;

    public void Init(int owner, float rad, Color color, float fadeIn, float duration, bool useSprite)
    {
        ownerPlayer = owner;
        radius = rad;
        targetColor = color;
        fadeInTime = fadeIn;
        totalDuration = duration;
        hasSprite = useSprite;
        if (hasSprite)
            sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(LifeCycleRoutine());
    }

    private IEnumerator LifeCycleRoutine()
    {
        if (hasSprite && sr != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeInTime)
            {
                float t = elapsed / fadeInTime;
                float alpha = Mathf.Lerp(0f, targetColor.a, t);
                sr.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            sr.color = targetColor;

            float remainingTime = totalDuration - fadeInTime;
            elapsed = 0f;
            while (elapsed < remainingTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            float fadeOutTime = 0.15f;
            elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                float t = elapsed / fadeOutTime;
                float alpha = Mathf.Lerp(targetColor.a, 0f, t);
                float scale = Mathf.Lerp(1f, 1.5f, t);
                sr.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
                transform.localScale = Vector3.one * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(totalDuration);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject root = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;

        Projectile projectile = root.GetComponentInChildren<Projectile>(true);
        if (projectile != null)
        {
            Destroy(projectile.gameObject);
            return;
        }

        SlowGrandeProjectile slowGrande = root.GetComponentInChildren<SlowGrandeProjectile>(true);
        if (slowGrande != null)
        {
            Destroy(slowGrande.gameObject);
            return;
        }

        HomingMissile homingMissile = root.GetComponentInChildren<HomingMissile>(true);
        if (homingMissile != null)
        {
            Destroy(homingMissile.gameObject);
            return;
        }

        SplittableObject splittable = root.GetComponentInChildren<SplittableObject>(true);
        if (splittable != null)
        {
            Vector2 hitDir = (root.transform.position - transform.position).normalized;
            splittable.Split(hitDir);
            return;
        }
    }
}