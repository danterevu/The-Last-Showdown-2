using UnityEngine;
using System.Collections;

public class HookProjectile : MonoBehaviour
{
    public enum HitType { Ground, Player, None }

    public event System.Action<HitType, Vector2, Collider2D> OnHit;

    [Header("Parpadeo")]
    [SerializeField] private float blinkInterval = 0.08f;
    [SerializeField] private int blinkCount = 5;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool hasHit = false;
    private bool isMissed = false;
    private float maxDistance;
    private Vector2 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void Launch(Vector2 initialVelocity, float maxDist, Vector2 origin)
    {
        rb.linearVelocity = initialVelocity;
        maxDistance = maxDist;
        startPosition = origin;
        StartCoroutine(MonitorDistance());
    }

    // Llamado cuando el gancho alcanza la distancia máxima sin golpear nada
    public void MarkAsMissed()
    {
        if (hasHit) return;
        isMissed = true;
        // Ahora el gancho caerá libremente y al chocar se destruirá sin arrastrar
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // Determinar qué se golpeó
        HitType type = HitType.None;
        string tag = collision.collider.tag;

        if (tag == "Player1" || tag == "Player2")
        {
            type = HitType.Player;
        }
        else
        {
            // Verificar si es suelo (puedes usar capa también)
            if (collision.collider.CompareTag("Ground") ||
                collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                type = HitType.Ground;
            }
            else
            {
                type = HitType.None; // cualquier otra cosa no cuenta
            }
        }

        // Si el gancho ya estaba marcado como "fallo", lo tratamos como None
        if (isMissed) type = HitType.None;

        RegisterHit(type, collision.contacts[0].point, collision.collider);
    }

    private void RegisterHit(HitType type, Vector2 point, Collider2D hitCollider)
    {
        if (hasHit) return;
        hasHit = true;
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        transform.position = point;

        OnHit?.Invoke(type, point, hitCollider);

        if (type == HitType.None)
        {
            // Fallo: parpadeo y destrucción
            StartCoroutine(BlinkAndDestroy());
        }
        else
        {
            // Éxito: se destruye después de que el arrastre comience (para no interferir)
            Destroy(gameObject, 0.1f);
        }
    }

    private IEnumerator MonitorDistance()
    {
        float traveled = 0f;
        while (!hasHit)
        {
            traveled = Vector2.Distance(startPosition, (Vector2)transform.position);
            if (traveled >= maxDistance && !hasHit && !isMissed)
            {
                MarkAsMissed();
                break;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator BlinkAndDestroy()
    {
        // Parpadeo para dar feedback visual
        for (int i = 0; i < blinkCount; i++)
        {
            if (sr != null)
                sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
        Destroy(gameObject);
    }
}