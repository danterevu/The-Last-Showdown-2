using UnityEngine;

/// Singleton que ejecuta los efectos de los power ups.
/// SETUP:
///   - Crear un GameObject vacio en la escena llamado "PowerUpManager"
///   - Agregar este script
///   - Asignar los prefabs en el Inspector
public class SpacePowerUpManager : MonoBehaviour
{
    public static SpacePowerUpManager Instance { get; private set; }

    [Header("Prefabs de power ups")]
    [Tooltip("Prefab del proyectil de la Slow Grande (SlowGrandeProjectile.cs)")]
    [SerializeField] private GameObject slowGrandeProjectilePrefab;

    [Tooltip("Prefab del meteoro (puede ser un simple proyectil rapido con mucho daño)")]
    [SerializeField] private GameObject meteorPrefab;

    [Tooltip("Prefab del misil teledirigido")]
    [SerializeField] private GameObject homingMissilePrefab;

    [Header("Configuracion Rocket Sabotage")]
    [SerializeField] private float rocketSabotageAcceleration = 20f;
    [SerializeField] private float rocketSabotageDuration = 4f;

    [Header("Configuracion Repulsion")]
    [Tooltip("Fuerza del impulso de repulsion aplicado a la nave rival")]
    [SerializeField] private float repulsionForce = 20f;

    // Referencia a las naves para la repulsion
    [Header("Referencias a las naves")]
    [SerializeField] private SpaceShipController player1Ship;
    [SerializeField] private SpaceShipController player2Ship;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── Slow Grande ──────────────────────────────────────────────────────────

    /// Lanza el proyectil de slow grande desde la posicion y direccion indicadas.
    public void LaunchSlowGrande(Vector2 origin, Vector2 direction, int ownerPlayer)
    {
        if (slowGrandeProjectilePrefab == null)
        {
            Debug.LogError("[SpacePowerUpManager] Falta asignar slowGrandeProjectilePrefab.");
            return;
        }

        GameObject obj = Instantiate(slowGrandeProjectilePrefab, origin, Quaternion.identity);
        SlowGrandeProjectile proj = obj.GetComponent<SlowGrandeProjectile>();
        if (proj != null)
            proj.Init(direction, ownerPlayer);
    }

    // ── Rocket Sabotage ──────────────────────────────────────────────────────

    /// Agrega RocketSabotageEffect a la nave rival para que acelere sin control.
    public void ApplyRocketSabotage(SpaceShipController rivalShip)
    {
        if (rivalShip == null) return;

        // Evitar que se apilen multiples efectos
        if (rivalShip.GetComponent<RocketSabotageEffect>() != null) return;

        RocketSabotageEffect effect = rivalShip.gameObject.AddComponent<RocketSabotageEffect>();
        effect.Init(rocketSabotageAcceleration, rocketSabotageDuration);
    }

    // ── Meteor Strike ────────────────────────────────────────────────────────

    /// Instancia un meteoro dirigido hacia la posicion de la nave rival.
    public void LaunchMeteorStrike(Vector2 rivalPosition, int ownerPlayer)
    {
        if (meteorPrefab == null)
        {
            Debug.LogError("[SpacePowerUpManager] Falta asignar meteorPrefab.");
            return;
        }

        // Spawnear el meteoro desde fuera de la pantalla, apuntando al rival
        Vector2 spawnOffset = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * 20f;

        Vector2 origin = rivalPosition + spawnOffset;
        Vector2 direction = (rivalPosition - origin).normalized;

        GameObject obj = Instantiate(meteorPrefab, origin, Quaternion.identity);

        // Rotar el meteoro hacia su destino
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Si tiene un Projectile, inicializarlo
        Projectile proj = obj.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(direction, 18f, 20f, 40f, ownerPlayer);
        else
        {
            // Si tiene Rigidbody2D sin Projectile, moverlo manualmente
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = direction * 18f;
            }
            // Destruir automaticamente si no tiene logica propia
            Destroy(obj, 5f);
        }
    }

    // ── Homing Missile ───────────────────────────────────────────────────────

    /// Lanza un misil que persigue al rival.
    public void LaunchHomingMissile(Vector2 origin, Transform target, int ownerPlayer)
    {
        if (homingMissilePrefab == null)
        {
            Debug.LogError("[SpacePowerUpManager] Falta asignar homingMissilePrefab.");
            return;
        }

        GameObject obj = Instantiate(homingMissilePrefab, origin, Quaternion.identity);

        // Si el prefab tiene HomingMissile.cs, inicializarlo
        HomingMissile missile = obj.GetComponent<HomingMissile>();
        if (missile != null)
        {
            missile.Init(target, ownerPlayer);
        }
        else
        {
            // Fallback: si no tiene HomingMissile.cs, usarlo como Projectile recto
            Vector2 dir = target != null
                ? ((Vector2)(target.position - (Vector3)(Vector2)origin)).normalized
                : Vector2.right;

            Projectile proj = obj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(dir, 14f, 15f, 30f, ownerPlayer);
            else
                Destroy(obj, 4f);
        }
    }

    // ── Repulsion ────────────────────────────────────────────────────────────

    /// Empuja a la nave rival en direccion opuesta al que activo el power up.
    public void ActivateRepulsion(Transform activatorTransform, int activatorPlayer)
    {
        SpaceShipController rival = activatorPlayer == 1 ? player2Ship : player1Ship;

        if (rival == null)
        {
            Debug.LogWarning("[SpacePowerUpManager] No se encontro la nave rival para Repulsion. " +
                             "Asigna player1Ship y player2Ship en el Inspector.");
            return;
        }

        Vector2 pushDirection = ((Vector2)(rival.transform.position - activatorTransform.position)).normalized;
        rival.AddImpulse(pushDirection * repulsionForce);
    }
}
