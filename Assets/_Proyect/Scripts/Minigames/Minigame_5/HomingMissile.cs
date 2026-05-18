using UnityEngine;

/// Script para el misil teledirigido del power up HomingMissile.
/// SETUP del prefab:
///   - SpriteRenderer con sprite del misil
///   - Rigidbody2D: Gravity=0, Collision Detection=Continuous
///   - CircleCollider2D con Is Trigger = true
///   - Este script
[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 180f;   // grados por segundo
    [SerializeField] private float range = 30f;        // distancia maxima antes de destruirse
    [SerializeField] private float predictionTime = 0.5f;  // para IA mejorada

    [Header("Vida")]
    [SerializeField] private int health = 3;

    [Header("Explosión")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 10f;
    [SerializeField] private GameObject explosionVfxPrefab;

    private Transform target;
    private int ownerPlayer;
    private float traveledDistance;
    private Rigidbody2D rb;
    private Vector2 lastTargetPosition;

    public void Init(Transform target, int ownerPlayer)
    {
        this.target = target;
        this.ownerPlayer = ownerPlayer;
        if (target != null)
            lastTargetPosition = target.position;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Explode();
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        traveledDistance += speed * Time.fixedDeltaTime;
        if (traveledDistance >= range)
        {
            Explode();
            return;
        }

        if (target == null)
        {
            // Si no hay target, seguir en la última dirección conocida
            rb.linearVelocity = transform.right * speed;
            return;
        }

        // IA Mejorada: Predicción de movimiento del target
        Vector2 targetVelocity = Vector2.zero;
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            targetVelocity = targetRb.linearVelocity;
        }
        else
        {
            targetVelocity = ((Vector2)target.position - lastTargetPosition) / Time.fixedDeltaTime;
        }
        lastTargetPosition = target.position;

        Vector2 predictedPosition = (Vector2)target.position + targetVelocity * predictionTime;

        // Calcular angulo hacia la posición predicha
        Vector2 toTarget = (predictedPosition - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.z;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

        rb.linearVelocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int hitPlayer = 0;
        if (other.CompareTag("Player1")) hitPlayer = 1;
        else if (other.CompareTag("Player2")) hitPlayer = 2;
        else
        {
            // Ignorar pickups y campos
            if (other.GetComponent<WeaponPickup>() != null) return;
            if (other.GetComponent<SpacePowerUpPickup>() != null) return;
            if (other.GetComponent<SlowField>() != null) return;

            // Si choca con un asteroide, explotar
            if (other.GetComponent<InteractiveAsteroid>() != null || other.GetComponent<BreakableAsteroid>() != null)
            {
                Explode();
            }
            return;
        }

        if (hitPlayer == ownerPlayer) return;

        GameManager.Instance?.RemovePoints(hitPlayer, 5);
        GameManager.Instance?.AddPoints(ownerPlayer, 5);
        SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);
        CameraShake.Instance?.Shake(0.15f, 0.1f);

        Explode();
    }

    private void Explode()
    {
        // Spawn VFX de explosión
        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        // Daño en área
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            // Dañar jugadores enemigos
            int hitPlayer = 0;
            if (hit.CompareTag("Player1")) hitPlayer = 1;
            else if (hit.CompareTag("Player2")) hitPlayer = 2;

            if (hitPlayer != 0 && hitPlayer != ownerPlayer)
            {
                GameManager.Instance?.RemovePoints(hitPlayer, Mathf.RoundToInt(explosionDamage));
                SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);
            }

            // Dañar asteroides
            HomingMissile otherMissile = hit.GetComponent<HomingMissile>();
            if (otherMissile != null && otherMissile != this)
            {
                otherMissile.TakeDamage(999);
            }
        }

        CameraShake.Instance?.Shake(0.2f, 0.15f);
        Destroy(gameObject);
    }
}
