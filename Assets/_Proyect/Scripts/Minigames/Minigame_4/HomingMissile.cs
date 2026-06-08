using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float range = 30f;
    [SerializeField] private float predictionTime = 0.5f;

    [Header("Vida")]
    [SerializeField] private int health = 3;

    [Header("Explosion")]
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
        {
            lastTargetPosition = target.position;
            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Explode();
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
        if (traveledDistance >= range) { Explode(); return; }

        if (target == null) { rb.linearVelocity = transform.right * speed; return; }

        Vector2 targetVelocity = Vector2.zero;
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null) targetVelocity = targetRb.linearVelocity;
        else targetVelocity = ((Vector2)target.position - lastTargetPosition) / Time.fixedDeltaTime;
        lastTargetPosition = target.position;

        Vector2 predictedPosition = (Vector2)target.position + targetVelocity * predictionTime;
        Vector2 toTarget = (predictedPosition - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float newAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, turnSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

        rb.linearVelocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // --- ALIEN: el misil lo mata y explota ---
        SpaceAlien alien = other.GetComponent<SpaceAlien>();
        if (alien != null)
        {
            alien.TakeDamage(ownerPlayer);
            Explode();
            return;
        }

        int hitPlayer = 0;
        if (other.CompareTag("Player1")) hitPlayer = 1;
        else if (other.CompareTag("Player2")) hitPlayer = 2;
        else
        {
            if (other.GetComponent<WeaponPickup>() != null) return;
            if (other.GetComponent<SpacePowerUpPickup>() != null) return;
            if (other.GetComponent<SlowField>() != null) return;

            if (other.GetComponent<InteractiveAsteroid>() != null ||
                other.GetComponent<BreakableAsteroid>() != null ||
                other.GetComponent<SplittableObject>() != null)
                Explode();
            return;
        }

        if (hitPlayer == ownerPlayer) return;

        GameManager.Instance?.RemovePoints(hitPlayer, 5);
        GameManager.Instance?.AddPoints(ownerPlayer, 5);
        SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);
        Explode();
    }

    private void Explode()
    {
        if (explosionVfxPrefab != null)
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            // --- ALIEN en area de explosion ---
            SpaceAlien alien = hit.GetComponent<SpaceAlien>();
            if (alien != null) { alien.TakeDamage(ownerPlayer); continue; }

            int hitPlayer = 0;
            if (hit.CompareTag("Player1")) hitPlayer = 1;
            else if (hit.CompareTag("Player2")) hitPlayer = 2;

            if (hitPlayer != 0 && hitPlayer != ownerPlayer)
            {
                GameManager.Instance?.RemovePoints(hitPlayer, Mathf.RoundToInt(explosionDamage));
                SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);
            }

            HomingMissile otherMissile = hit.GetComponent<HomingMissile>();
            if (otherMissile != null && otherMissile != this) { otherMissile.TakeDamage(999); continue; }

            BreakableAsteroid breakable = hit.GetComponent<BreakableAsteroid>();
            if (breakable != null) { Vector2 d = (hit.transform.position - transform.position).normalized; breakable.TakeDamage(999, true, d); continue; }

            InteractiveAsteroid interactive = hit.GetComponent<InteractiveAsteroid>();
            if (interactive != null) { Destroy(interactive.gameObject); continue; }

            SplittableObject splittable = hit.GetComponent<SplittableObject>();
            if (splittable != null) { splittable.Split((hit.transform.position - transform.position).normalized); }
        }

        Destroy(gameObject);
    }
}