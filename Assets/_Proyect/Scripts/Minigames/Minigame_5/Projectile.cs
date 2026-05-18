using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem hitParticles;

    [Header("Asteroid Push")]
    [SerializeField] private float asteroidPushForce = 25f;

    // -----------------------------------------------------------------------
    //  ESTADO (seteado por WeaponController al instanciar)
    // -----------------------------------------------------------------------

    private float speed;
    private float damage;
    private float range;
    private int ownerPlayer;   // 1 o 2, para no dañarse a sí mismo
    private float traveledDistance;
    private WeaponData.WeaponType weaponType;

    private Rigidbody2D rb;
    private bool hitVfxSpawned;
    private Vector2 lastMoveDir = Vector2.right;

    // -----------------------------------------------------------------------
    //  INICIALIZACIÓN
    // -----------------------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
    }

    /// <summary>
    /// Llamado por WeaponController justo después de Instantiate.
    /// </summary>
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

    // -----------------------------------------------------------------------
    //  MOVIMIENTO Y RANGO
    // -----------------------------------------------------------------------

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

    // -----------------------------------------------------------------------
    //  IMPACTO
    // -----------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        Debug.Log($"Colisión con: {other.name} | tag: {other.tag} | layer: {LayerMask.LayerToName(other.layer)}");

        // Verificar si es un HomingMissile
        HomingMissile homingMissile = other.GetComponent<HomingMissile>();
        if (homingMissile != null)
        {
            homingMissile.TakeDamage(1);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // Verificar si es un BreakableAsteroid
        BreakableAsteroid breakableAsteroid = other.GetComponent<BreakableAsteroid>();
        if (breakableAsteroid != null)
        {
            Vector2 hitDirection = (other.transform.position - transform.position).normalized;
            breakableAsteroid.TakeDamage(1, weaponType == WeaponData.WeaponType.Laser, hitDirection);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // Determinar si impactó a un jugador
        int hitPlayer = 0;
        Debug.Log($"Impacto: {other.name} tag={other.tag}");
        Debug.Log($"hitPlayer: {hitPlayer} | ownerPlayer: {ownerPlayer}");
        Debug.Log($"SpaceMinigame.Instance: {SpaceMinigame.Instance}");
        Debug.Log($"Tag length: {other.tag.Length} | bytes: {string.Join(",", System.Text.Encoding.UTF8.GetBytes(other.tag))}");
        string tag = other.tag.Trim();
        if (tag == "Player1") hitPlayer = 1;
        else if (tag == "Player2") hitPlayer = 2;
        else
        {
            // Buscar InteractiveAsteroid por componente (más fiable que layer)
            InteractiveAsteroid asteroid = other.GetComponent<InteractiveAsteroid>();
            if (asteroid == null)
            {
                asteroid = other.GetComponentInParent<InteractiveAsteroid>();
            }

            if (asteroid != null)
            {
                Rigidbody2D asteroidRb = asteroid.GetComponent<Rigidbody2D>();
                if (asteroidRb != null)
                {
                    Vector2 pushDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                    if (pushDir.sqrMagnitude < 0.0001f)
                    {
                        pushDir = lastMoveDir;
                    }

                    // Empuje base + bonus por velocidad de la bala
                    float finalPushForce = asteroidPushForce + (speed * 0.5f);
                    asteroidRb.AddForce(pushDir * finalPushForce, ForceMode2D.Impulse);
                }
            }

            // Impactó contra una pared u otro objeto
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // No dañarse a sí mismo
        if (hitPlayer == ownerPlayer) return;

        // --- MODIFICADOR: Golden Kill ---
        // Si es la primera kill de la ronda con este mod activo, los puntos
        // del owner se multiplican por goldenKillMultiplier (x3 por defecto).
        float killPointsMultiplier = 1f;
        bool isGoldenKill = ModifierManager.Instance != null
                            && ModifierManager.Instance.IsGoldenKillAvailable();

        if (isGoldenKill)
        {
            killPointsMultiplier = ModifierManager.Instance.goldenKillMultiplier;
            ModifierManager.Instance.ConsumeGoldenKill();
            Debug.Log($"[GoldenKill] ¡Primera kill! Los puntos del jugador {ownerPlayer} son x{killPointsMultiplier}");
        }

        // Restar puntos al jugador impactado (daño directo, sin modificador)
        SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);

        // Sumar puntos al dueño del proyectil (kill bonus con posible multiplicador)
        int killBonus = Mathf.RoundToInt((damage / 2f) * killPointsMultiplier);
        GameManager.Instance?.AddPoints(ownerPlayer, killBonus);

        // CameraShake al impacto
        CameraShake.Instance?.Shake(0.1f, 0.08f);
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
        float lifetime = instance.main.duration;
        var startLifetime = instance.main.startLifetime;
        lifetime += startLifetime.constantMax;
        if (lifetime > 0f)
            Destroy(instance.gameObject, lifetime);
    }
}
