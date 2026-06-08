using System.Collections;
using UnityEngine;

public class RotatingGrowOnHitShrink : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Grow")]
    [SerializeField] private Vector3 maxScale = new Vector3(3f, 3f, 3f);
    [SerializeField] private float growSpeed = 1f;

    [Header("Hit Shrink")]
    [SerializeField] private Vector3 minScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float shrinkSpeed = 2f;
    [SerializeField] private float blinkDuration = 0.35f;
    [SerializeField] private float blinkInterval = 0.06f;
    [SerializeField] private float recoverDelay = 5f;
    [SerializeField] private bool destroyProjectileOnHit = true;

    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;
    private Coroutine _hitRoutine;
    private bool _canGrow = true;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (_renderers != null)
        {
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _originalColors[i] = _renderers[i] != null ? _renderers[i].color : Color.white;
        }
    }

    private void OnEnable()
    {
        RestoreColors();
    }

    private void OnDisable()
    {
        if (_hitRoutine != null)
        {
            StopCoroutine(_hitRoutine);
            _hitRoutine = null;
        }
        _canGrow = true;
        RestoreColors();
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (!_canGrow) return;

        Vector3 targetScale = Vector3.Max(minScale, maxScale);
        if (transform.localScale.x < targetScale.x || transform.localScale.y < targetScale.y || transform.localScale.z < targetScale.z)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Mathf.Max(0f, growSpeed) * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other != null ? other.gameObject : null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision != null ? collision.gameObject : null);
    }

    private void HandleHit(GameObject other)
    {
        if (other == null) return;

        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null)
            projectile = other.GetComponentInParent<Projectile>();

        if (projectile == null) return;

        if (destroyProjectileOnHit)
            Destroy(projectile.gameObject);

        if (_hitRoutine != null)
            StopCoroutine(_hitRoutine);

        _hitRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        _canGrow = false;

        float blinkTime = Mathf.Max(0f, blinkDuration);
        float interval = Mathf.Max(0.01f, blinkInterval);
        float elapsed = 0f;
        bool white = true;

        Vector3 shrinkTarget = Vector3.Max(Vector3.zero, minScale);

        SetBlinkWhite(true);

        while (elapsed < blinkTime)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, shrinkTarget, Mathf.Max(0f, shrinkSpeed) * interval);

            yield return new WaitForSeconds(interval);
            elapsed += interval;

            white = !white;
            SetBlinkWhite(white);
        }

        RestoreColors();

        while (transform.localScale.x > shrinkTarget.x || transform.localScale.y > shrinkTarget.y || transform.localScale.z > shrinkTarget.z)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, shrinkTarget, Mathf.Max(0f, shrinkSpeed) * Time.deltaTime);
            yield return null;
        }

        if (recoverDelay > 0f)
            yield return new WaitForSeconds(recoverDelay);

        _canGrow = true;
        _hitRoutine = null;
    }

    private void SetBlinkWhite(bool white)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].color = white ? Color.white : (_originalColors != null && i < _originalColors.Length ? _originalColors[i] : _renderers[i].color);
        }
    }

    private void RestoreColors()
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].color = _originalColors != null && i < _originalColors.Length ? _originalColors[i] : Color.white;
        }
    }
}

