using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]


public class Projectile : MonoBehaviour
{
    // -----------------------------------------------------------------------
    //  ESTADO (seteado por WeaponController al instanciar)
    // -----------------------------------------------------------------------

    private float speed;
    private float damage;
    private float range;
    private int ownerPlayer;   // 1 o 2, para no dañarse a sí mismo
    private float traveledDistance;

    private Rigidbody2D rb;

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
    public void Init(Vector2 direction, float speed, float damage, float range, int ownerPlayer)
    {
        this.speed = speed;
        this.damage = damage;
        this.range = range;
        this.ownerPlayer = ownerPlayer;
        rb.linearVelocity = direction.normalized * speed;
    }

    // -----------------------------------------------------------------------
    //  MOVIMIENTO Y RANGO
    // -----------------------------------------------------------------------

    private void Update()
    {
        traveledDistance += speed * Time.deltaTime;
        if (traveledDistance >= range)
            Destroy(gameObject);
    }

    // -----------------------------------------------------------------------
    //  IMPACTO
    // -----------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Colisión con: {other.gameObject.name} | tag: {other.tag} | layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        // Determinar si impactó a un jugador
        int hitPlayer = 0;
        Debug.Log($"Impacto: {other.gameObject.name} tag={other.tag}");
        Debug.Log($"hitPlayer: {hitPlayer} | ownerPlayer: {ownerPlayer}");
        Debug.Log($"SpaceMinigame.Instance: {SpaceMinigame.Instance}");
        Debug.Log($"Tag length: {other.tag.Length} | bytes: {string.Join(",", System.Text.Encoding.UTF8.GetBytes(other.tag))}");
        string tag = other.tag.Trim();
        if (tag == "Player1") hitPlayer = 1;
        else if (tag == "Player2") hitPlayer = 2;
        else
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("InteractiveAsteroid"))
            {
                Rigidbody2D asteroidRb = other.attachedRigidbody;

                if (asteroidRb != null)
                {
                    Vector2 pushDir =
                        ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

                    asteroidRb.AddForce(
                        pushDir * 5f,
                        ForceMode2D.Impulse
                    );
                }
            }
            // Impactó contra una pared u otro objeto
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
        Destroy(gameObject);
    }
}
