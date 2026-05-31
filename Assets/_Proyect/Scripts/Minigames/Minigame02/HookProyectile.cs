using UnityEngine;
using System.Collections;

public class HookProjectile : MonoBehaviour
{
    public enum HitType { Ground, Player, None }

    public event System.Action<HitType, Vector2, Collider2D> OnHit;

    [Header("Config")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private int blinkCount = 6;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 lastPosition;
    private bool hasHit = false;
    private bool isMissed = false;
    private float maxDistance = 20f;
    private Vector2 origin;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Launch(Vector2 velocity, float maxDist, Vector2 startPoint)
    {
        rb.linearVelocity = velocity;
        maxDistance = maxDist;
        origin = startPoint;
        lastPosition = transform.position;
        StartCoroutine(CheckDistanceAndTimeout());
    }

    // Marca el gancho como “errado” (alcanzó distancia máxima sin golpear nada).l
    public void Miss()
    {
        if (hasHit) return;
        isMissed = true;
        // Sigue con físicas, pero ya no procesará colisiones para arrastrar
    }

    private void FixedUpdate()
    {
        if (hasHit) return;

        // Raycast desde la posición anterior a la actual
        Vector2 currentPos = transform.position;
        RaycastHit2D hit = Physics2D.Linecast(lastPosition, currentPos, groundLayer | playerLayer);

        if (hit.collider != null)
        {
            // Evitar colisionar con el lanzador o el rival si ya lo ignoramos (se hace al instanciar)
            // pero por si acaso:
            if (hit.collider.CompareTag("Player1") || hit.collider.CompareTag("Player2"))
            {
                // Si es un jugador y NO estamos en modo "missed", procesamos
                if (!isMissed)
                {
                    RegisterHit(HitType.Player, hit.point, hit.collider);
                }
                else
                {
                    // Si errado, igual golpea al player pero no arrastra  solo desaparece con parpadeo
                    RegisterHit(HitType.None, hit.point, hit.collider);
                }
            }
            else if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
            {
                // Es suelo
                if (!isMissed)
                    RegisterHit(HitType.Ground, hit.point, hit.collider);
                else
                    RegisterHit(HitType.None, hit.point, hit.collider); // errado toca suelo  desaparece
            }
        }

        lastPosition = currentPos;
    }

    private void RegisterHit(HitType type, Vector2 point, Collider2D col)
    {
        if (hasHit) return;
        hasHit = true;
        rb.linearVelocity = Vector2.zero;
        // rb.isKinematic = true; // detener físicas
        transform.position = point;
        OnHit?.Invoke(type, point, col);
        if (type == HitType.None)
        {
            StartCoroutine(BlinkAndDestroy());
        }
        else
        {
            Destroy(gameObject, 0.1f); // se destruye después del evento (el evento arrastra)
        }
    }

    private IEnumerator CheckDistanceAndTimeout()
    {
        float traveled = 0f;
        while (!hasHit)
        {
            traveled = Vector2.Distance(origin, transform.position);
            if (traveled >= maxDistance && !hasHit && !isMissed)
            {
                Miss(); // activa modo errado, ahora cae libremente
                break;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator BlinkAndDestroy()
    {
        // Parpadeo rápido (alternar visibilidad)
        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
        Destroy(gameObject);
    }
}