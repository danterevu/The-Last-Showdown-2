using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlowGrandeProjectile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float range = 12f;
    [Tooltip("Segundos antes de terminar el recorrido para activar la explosión")]
    [SerializeField] private float secondsBeforeExplosion = 1f;

    [Header("Explosion")]
    [SerializeField] private GameObject slowFieldPrefab;
    [SerializeField] private ParticleSystem explosionParticles;
    [Tooltip("Cuánto más grande que el SlowField normal es la explosión")]
    [SerializeField] private float explosionScale = 3f;
    [SerializeField] private bool hideFieldVisual = true;

    [Header("Colision")]
    [SerializeField] private float collisionDisableTime = 0.5f;

    [Header("Void (Agujero Negro)")]
    [SerializeField] private GameObject voidPrefab;
    [Tooltip("Tiempo después del cual se destruye el Void (segundos)")]
    [SerializeField] private float voidLifetime = 3f;

    private float traveledDistance;
    private int ownerPlayer;
    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 lastPosition;
    private bool deployed;
    private bool collisionWasDisabled;
    private bool explosionTriggered;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (collisionDisableTime > 0f && col != null)
        {
            collisionWasDisabled = true;
            StartCoroutine(EnableCollisionAfterDelay(collisionDisableTime));
        }
    }

    public void Init(Vector2 direction, int ownerPlayer)
    {
        this.ownerPlayer = ownerPlayer;

        // Fallback: si direction es zero, lanzar hacia adelante
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;

        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        lastPosition = rb.position;
    }

    private void Start()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude < 0.0001f)
            rb.linearVelocity = (Vector2)transform.right * speed;

        if (rb != null)
            lastPosition = rb.position;
        else
            lastPosition = transform.position;
    }

    private void Update()
    {
        if (deployed) return;

        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        traveledDistance += Vector2.Distance(currentPosition, lastPosition);
        lastPosition = currentPosition;

        // Calcular cuánto tiempo falta para llegar al rango
        float distanceLeft = range - traveledDistance;
        float timeLeft = distanceLeft / speed;

        if (timeLeft <= secondsBeforeExplosion && !explosionTriggered)
        {
            TriggerExplosion();
        }

        if (traveledDistance >= range)
            Deploy();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (deployed) return;
        if (other.CompareTag("Player1") && ownerPlayer == 1) return;
        if (other.CompareTag("Player2") && ownerPlayer == 2) return;
        if (other.GetComponent<SpaceZoneBoundary>() != null) return;
        if (other.GetComponent<SlowField>() != null) return;
        if (other.GetComponent<WeaponPickup>() != null) return;
        if (other.GetComponent<SpacePowerUpPickup>() != null) return;
        if (other.GetComponent<HomingMissile>() != null) return;

        TriggerExplosion();
        Deploy();
    }

    private void TriggerExplosion()
    {
        if (explosionTriggered) return;
        explosionTriggered = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.black;
        }

        if (explosionParticles != null)
        {
            ParticleSystem instance = Instantiate(explosionParticles, transform.position, explosionParticles.transform.rotation);
            float lifetime = instance.main.duration + instance.main.startLifetime.constantMax;
            if (lifetime > 0f) Destroy(instance.gameObject, lifetime);
        }
    }

    private void Deploy()
    {
        if (deployed) return;
        deployed = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Instanciar el Void
        if (voidPrefab != null)
        {
            GameObject spawnedVoidInstance = Instantiate(voidPrefab, transform.position, Quaternion.identity);
            spawnedVoidInstance.SetActive(true);
            // Destruir el Void después del tiempo establecido
            Destroy(spawnedVoidInstance, voidLifetime);
        }

        // Instanciar el SlowField
        if (slowFieldPrefab != null)
        {
            GameObject field = Instantiate(slowFieldPrefab, transform.position, Quaternion.identity);

            // Guardar escala original del Void antes de escalar el padre
            Transform voidChild = field.transform.Find("Void");
            Vector3 originalVoidScale = voidChild != null ? voidChild.localScale : Vector3.one;

            // Aplicar escala de explosión al campo completo
            field.transform.localScale *= explosionScale;

            // Restaurar escala original del Void (para que mantenga su tamaño visual)
            if (voidChild != null)
                voidChild.localScale = originalVoidScale;

            if (hideFieldVisual)
            {
                foreach (SpriteRenderer sr in field.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.enabled = false;
            }

            foreach (ParticleSystem ps in field.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play(true);
        }

        Destroy(gameObject);
    }



    private IEnumerator EnableCollisionAfterDelay(float delay)
    {
        col.enabled = false;
        yield return new WaitForSeconds(delay);
        if (!deployed && col != null)
            col.enabled = true;
    }
}
