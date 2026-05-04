using UnityEngine;

/// <summary>
/// Va en el prefab de proyectil. Se mueve en línea recta y se destruye
/// al alcanzar el rango o al impactar contra algo.
///
/// SETUP del prefab:
///   - SpriteRenderer con el sprite del proyectil
///   - Rigidbody2D: Gravity=0, Collision Detection=Continuous
///   - CircleCollider2D con Is Trigger = true
///   - Este script
/// </summary>
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
        Debug.Log($"Impacto: {other.gameObject.name} tag={other.tag}");
        // Determinar si impactó a un jugador
        int hitPlayer = 0;
        if (other.CompareTag("Player1")) hitPlayer = 1;
        else if (other.CompareTag("Player2")) hitPlayer = 2;
        else
        {
           
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
