using UnityEngine;

public class MeteorMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float fallDuration = 1.2f;

    [Header("Escala")]
    [SerializeField] private float startScale = 12f;
    [SerializeField] private float endScale = 2f;

    [Header("Rotacion")]
    [SerializeField] private float rotateSpeed = 360f;

    [Header("Impacto")]
    [SerializeField] private float impactRadius = 6f;
    [SerializeField] private float impactForce = 25f;

    [SerializeField] private GameObject impactVfx;

    [Header("Editor")]
    [SerializeField] private bool showImpactRadiusGizmo = true;
    [SerializeField] private Color impactRadiusGizmoColor = new Color(1f, 0.6f, 0.1f, 0.9f);

    private Vector2 targetPosition;
    private Vector2 startPosition;

    private float timer;
    private int ownerPlayer;
    private Rigidbody2D rb;

    public void Initialize(Vector2 target, int ownerPlayer = 0)
    {
        targetPosition = target;
        this.ownerPlayer = ownerPlayer;

        Vector2 randomOffset =
            new Vector2(
                Random.Range(-20f, -12f),
                Random.Range(10f, 16f)
            );

        startPosition =
            target + randomOffset;

        transform.position = startPosition;

        transform.localScale =
            Vector3.one * startScale;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = timer / fallDuration;

        // Movimiento
        Vector2 newPos = Vector2.Lerp(startPosition, targetPosition, t);
        if (rb != null)
            rb.MovePosition(newPos);
        else
            transform.position = newPos;

        // Escala
        float scale =
            Mathf.Lerp(
                startScale,
                endScale,
                t
            );

        transform.localScale =
            Vector3.one * scale;

        // Rotacion
        transform.Rotate(
            0f,
            0f,
            rotateSpeed * Time.deltaTime
        );

        // Impacto
        if (t >= 1f)
        {
            Impact();
        }
    }

    private void Impact()
    {
        Collider2D[] hits =
     Physics2D.OverlapCircleAll(targetPosition, impactRadius);

        foreach (var hit in hits)
        {
            // Verificar si es un BreakableAsteroid y destruirlo de una vez
            BreakableAsteroid breakableAsteroid = hit.GetComponent<BreakableAsteroid>();
            if (breakableAsteroid == null)
                breakableAsteroid = hit.GetComponentInParent<BreakableAsteroid>();

            if (breakableAsteroid != null)
            {
                Vector2 direction = (hit.transform.position - (Vector3)targetPosition).normalized;
                breakableAsteroid.TakeDamage(999, false, direction);
                continue;
            }

            Transform root = hit.transform.root;
            int hitPlayer = 0;
            if (root.CompareTag("Player1")) hitPlayer = 1;
            else if (root.CompareTag("Player2")) hitPlayer = 2;

            if (hitPlayer != 0 && (ownerPlayer == 0 || hitPlayer != ownerPlayer))
                SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);

            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb == null) continue;

            Vector2 dir =
                (rb.position - targetPosition).normalized;

            rb.AddForce(dir * impactForce, ForceMode2D.Impulse);
        }
        if (impactVfx != null)
        {
            Instantiate(
            impactVfx,
            targetPosition,
            Quaternion.Euler(-90f, 0f, 0f)
        );
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showImpactRadiusGizmo) return;
        Gizmos.color = impactRadiusGizmoColor;
        Vector3 center = Application.isPlaying ? (Vector3)targetPosition : transform.position;
        Gizmos.DrawWireSphere(center, impactRadius);
    }
}
