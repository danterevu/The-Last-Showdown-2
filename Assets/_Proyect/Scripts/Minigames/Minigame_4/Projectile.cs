using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem hitParticles;

    [Header("Asteroid Push")]
    [SerializeField] private float asteroidPushForce = 25f;

    private float speed;
    private float damage;
    private float range;
    private int ownerPlayer;
    private float traveledDistance;
    private WeaponData.WeaponType weaponType;

    private Rigidbody2D rb;
    private bool hitVfxSpawned;
    private Vector2 lastMoveDir = Vector2.right;

    // Propiedades publicas para que otros scripts (SpaceLaserTurret, etc.) puedan leer estos valores
    public int OwnerPlayer => ownerPlayer;
    public float Damage => damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
    }

    public void Init(Vector2 direction, float speed, float damage, float range, int ownerPlayer, WeaponData.WeaponType weaponType = WeaponData.WeaponType.Pistol)
    {
        this.speed = speed;
        this.damage = damage;
        this.range = range;
        this.ownerPlayer = ownerPlayer;
        this.weaponType = weaponType;

        if (direction.sqrMagnitude > 0.0001f)
            lastMoveDir = direction.normalized;
        rb.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
            lastMoveDir = rb.linearVelocity.normalized;

        traveledDistance += speed * Time.deltaTime;
        if (traveledDistance >= range)
        {
            SpawnHitVfx();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) { HandleCollision(other.gameObject); }
    private void OnCollisionEnter2D(Collision2D collision) { HandleCollision(collision.gameObject); }

    private void HandleCollision(GameObject other)
    {
        // Debug.Log($"Colision con: {other.name} | tag: {other.tag} | layer: {LayerMask.LayerToName(other.layer)}");

        if (other.GetComponent<SpaceZoneBoundary>() != null) return;
        
        // Ignorar el detector de rango de la torreta y la layer de cabeza de torreta
        if (other.GetComponent<TurretRangeDetector>() != null) return;
        if (LayerMask.LayerToName(other.layer) == "TurretHead") return;

        // --- ALIEN ---
        SpaceAlien alien = other.GetComponent<SpaceAlien>();
        if (alien != null)
        {
            alien.TakeDamage(ownerPlayer);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        HomingMissile homingMissile = other.GetComponent<HomingMissile>();
        if (homingMissile != null)
        {
            homingMissile.TakeDamage(1);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        BreakableAsteroid breakableAsteroid = other.GetComponent<BreakableAsteroid>();
        if (breakableAsteroid != null)
        {
            Vector2 hitDirection = (other.transform.position - transform.position).normalized;
            breakableAsteroid.TakeDamage(1, weaponType == WeaponData.WeaponType.Laser, hitDirection);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        SplittableObject splittableObject = other.GetComponent<SplittableObject>();
        if (splittableObject != null)
        {
            Vector2 hitDirection = (other.transform.position - transform.position).normalized;
            splittableObject.Split(hitDirection);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // --- TORRETA ---
        SpaceLaserTurret turret = other.GetComponent<SpaceLaserTurret>();
        if (turret == null) turret = other.GetComponentInParent<SpaceLaserTurret>();
        if (turret != null)
        {
            turret.ReceiveDamageFromProjectile(damage);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        int hitPlayer = 0;
        string tag = other.tag.Trim();
        if (tag == "Player1") hitPlayer = 1;
        else if (tag == "Player2") hitPlayer = 2;
        else
        {
            InteractiveAsteroid asteroid = other.GetComponent<InteractiveAsteroid>();
            if (asteroid == null) asteroid = other.GetComponentInParent<InteractiveAsteroid>();

            if (asteroid != null)
            {
                Rigidbody2D asteroidRb = asteroid.GetComponent<Rigidbody2D>();
                if (asteroidRb != null)
                {
                    Vector2 pushDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                    if (pushDir.sqrMagnitude < 0.0001f) pushDir = lastMoveDir;
                    asteroidRb.AddForce(pushDir * (asteroidPushForce + speed * 0.5f), ForceMode2D.Impulse);
                }
            }

            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        if (hitPlayer == ownerPlayer) return;

        float killPointsMultiplier = 1f;
        bool isGoldenKill = ModifierManager.Instance != null && ModifierManager.Instance.IsGoldenKillAvailable();
        if (isGoldenKill)
        {
            killPointsMultiplier = ModifierManager.Instance.goldenKillMultiplier;
            ModifierManager.Instance.ConsumeGoldenKill();
        }

        SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);
        int killBonus = Mathf.RoundToInt((damage / 2f) * killPointsMultiplier);
        GameManager.Instance?.AddPoints(ownerPlayer, killBonus);

        GetComponent<Collider2D>().enabled = false;
        SpawnHitVfx();
        Destroy(gameObject);
    }

    private void SpawnHitVfx()
    {
        if (hitVfxSpawned) return;
        hitVfxSpawned = true;
        if (hitParticles == null) return;

        Vector2 dir = lastMoveDir.sqrMagnitude > 0.0001f ? lastMoveDir : Vector2.up;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(dir.x, dir.y, 0f));
        ParticleSystem instance = Instantiate(hitParticles, transform.position, rotation);
        float lifetime = instance.main.duration + instance.main.startLifetime.constantMax;
        if (lifetime > 0f) Destroy(instance.gameObject, lifetime);
    }
}