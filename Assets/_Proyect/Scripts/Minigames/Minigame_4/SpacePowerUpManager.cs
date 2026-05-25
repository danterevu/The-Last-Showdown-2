using System.Collections;
using System.Collections.Generic;
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

    [Header("Meteor Strike")]
    [SerializeField] private GameObject meteorWarningPrefab;
    [SerializeField] private float meteorWarningDuration = 2f;
    [SerializeField] private float meteorWarningBlinkSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float meteorWarningMinAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] private float meteorWarningMaxAlpha = 1f;

    [Tooltip("Prefab del misil teledirigido")]
    [SerializeField] private GameObject homingMissilePrefab;

    [Header("Configuracion Rocket Sabotage")]
    [SerializeField] private float rocketSabotageAcceleration = 20f;
    [SerializeField] private float rocketSabotageDuration = 4f;

    [Header("Configuracion Repulsion")]
    [Tooltip("Fuerza del impulso de repulsion aplicado a la nave rival")]
    //[SerializeField] private float repulsionForce = 20f;

    // Referencia a las naves para la repulsion
    [Header("Referencias a las naves")]
    [SerializeField] private SpaceShipController player1Ship;
    [SerializeField] private SpaceShipController player2Ship;

    [Header("Power Up HUD")]
    [SerializeField] private SpacePowerUpHUD hudPlayer1;
    [SerializeField] private SpacePowerUpHUD hudPlayer2;
    [SerializeField] private PowerUpHolder holderPlayer1;
    [SerializeField] private PowerUpHolder holderPlayer2;

    [Header("Repulsion VFX")]
    [SerializeField] private GameObject repulsionVfxPrefab;
    [SerializeField] private float repulsionRadius = 6f;
    [SerializeField] private float repulsionKnockback = 20f;
    [SerializeField] private float repulsionWaveDuration = 0.25f;
    [SerializeField] private bool showRepulsionWaveCircle = true;
    [SerializeField] private int repulsionWaveCircleSegments = 64;
    [SerializeField] private float repulsionWaveCircleLineWidth = 0.06f;
    [SerializeField] private Color repulsionWaveCircleColor = new Color(0.2f, 0.9f, 1f, 0.9f);
    [Header("Repulsion Collider Circle")]
    [SerializeField] private Sprite repulsionCircleSprite;
    [SerializeField] private Color repulsionCircleColor = new Color(0.2f, 0.9f, 1f, 0.5f);
    [SerializeField] private float repulsionCircleFadeInTime = 0.1f;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureShipReferences();
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
        if (proj == null)
            proj = obj.GetComponentInChildren<SlowGrandeProjectile>(true);

        if (proj == null)
        {
            Debug.LogError($"[SpacePowerUpManager] '{slowGrandeProjectilePrefab.name}' no tiene SlowGrandeProjectile, no se puede lanzar la granada.");
            Destroy(obj);
            return;
        }

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

        StartCoroutine(MeteorStrikeRoutine(rivalPosition, ownerPlayer));
    }

    private IEnumerator MeteorStrikeRoutine(Vector2 targetPosition, int ownerPlayer)
    {
        GameObject warning = null;
        if (meteorWarningPrefab != null)
            warning = Instantiate(meteorWarningPrefab, targetPosition, Quaternion.identity);

        if (meteorWarningDuration > 0f)
        {
            CanvasGroup canvasGroup = warning != null ? warning.GetComponent<CanvasGroup>() : null;
            SpriteRenderer[] renderers = warning != null ? warning.GetComponentsInChildren<SpriteRenderer>(true) : null;
            Color[] originalColors = null;
            if (canvasGroup == null && renderers != null && renderers.Length > 0)
            {
                originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                    originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
            }

            float elapsed = 0f;
            while (elapsed < meteorWarningDuration)
            {
                float speed = Mathf.Max(0.01f, meteorWarningBlinkSpeed);
                float wave = Mathf.Sin(elapsed * speed * Mathf.PI * 2f);
                float t = (wave + 1f) * 0.5f;
                t = t * t * (3f - 2f * t);
                float a = Mathf.Lerp(meteorWarningMinAlpha, meteorWarningMaxAlpha, t);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = a;
                }
                else if (renderers != null && renderers.Length > 0)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] == null) continue;
                        Color c = originalColors != null ? originalColors[i] : renderers[i].color;
                        c.a = a;
                        renderers[i].color = c;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (warning != null)
            Destroy(warning);

        Vector2 spawnOffset = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * 20f;

        Vector2 origin = targetPosition + spawnOffset;

        GameObject meteor = Instantiate(meteorPrefab, origin, Quaternion.identity);

        MeteorMovement movement = meteor.GetComponent<MeteorMovement>();
        if (movement == null)
            movement = meteor.GetComponentInChildren<MeteorMovement>(true);
        if (movement == null)
            movement = meteor.AddComponent<MeteorMovement>();

        movement.Initialize(targetPosition, ownerPlayer);
    }

    // ── Homing Missile ───────────────────────────────────────────────────────

    /// Lanza un misil que persigue al rival.
    public void LaunchHomingMissile(Vector2 origin, Transform target, int ownerPlayer, Quaternion shipRotation)
    {
        if (homingMissilePrefab == null)
        {
            Debug.LogError("[SpacePowerUpManager] Falta asignar homingMissilePrefab.");
            return;
        }

        // Instanciar con la rotacion de la nave en vez de Quaternion.identity
        GameObject obj = Instantiate(homingMissilePrefab, origin, shipRotation);

        HomingMissile missile = obj.GetComponent<HomingMissile>();
        if (missile != null)
        {
            missile.Init(target, ownerPlayer);
        }
        else
        {
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
        EnsureShipReferences();

        Vector2 center = activatorTransform.position;

        if (repulsionVfxPrefab != null)
        {
            Instantiate(repulsionVfxPrefab, center, Quaternion.Euler(-90f,0f,0f));
        }

        if (showRepulsionWaveCircle)
            SpawnRepulsionWaveCircle(center);

        SpawnRepulsionColliderCircle(center, activatorPlayer);

        StartCoroutine(RepulsionWaveRoutine(center, activatorPlayer));
    }

    private void SpawnRepulsionColliderCircle(Vector2 center, int activatorPlayer)
    {
        GameObject circleObj = new GameObject("RepulsionColliderCircle");
        circleObj.transform.position = center;

        if (repulsionCircleSprite != null)
        {
            SpriteRenderer sr = circleObj.AddComponent<SpriteRenderer>();
            sr.sprite = repulsionCircleSprite;
            sr.color = new Color(repulsionCircleColor.r, repulsionCircleColor.g, repulsionCircleColor.b, 0f);
            sr.sortingOrder = 100;
        }

        CircleCollider2D collider = circleObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = repulsionRadius;

        RepulsionCircleHandler handler = circleObj.AddComponent<RepulsionCircleHandler>();
        handler.Init(activatorPlayer, repulsionRadius, repulsionCircleColor, repulsionCircleFadeInTime, repulsionWaveDuration, repulsionCircleSprite != null);
    }

    private void SpawnRepulsionWaveCircle(Vector2 center)
    {
        int segments = Mathf.Max(8, repulsionWaveCircleSegments);
        GameObject circleObj = new GameObject("RepulsionWaveCircle");
        circleObj.transform.position = center;

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = repulsionWaveCircleLineWidth;
        lr.endWidth = repulsionWaveCircleLineWidth;
        lr.startColor = repulsionWaveCircleColor;
        lr.endColor = repulsionWaveCircleColor;
        lr.numCapVertices = 2;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lr.material = new Material(shader);

        float step = (Mathf.PI * 2f) / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = step * i;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
        }

        circleObj.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAndDestroyRoutine(circleObj.transform, repulsionWaveDuration, repulsionRadius));
    }

    private IEnumerator ScaleAndDestroyRoutine(Transform target, float duration, float radius)
    {
        float t = 0f;
        float d = Mathf.Max(0.01f, duration);
        Vector3 endScale = Vector3.one * radius;
        while (t < d)
        {
            float a = t / d;
            a = a * a * (3f - 2f * a);
            if (target != null)
                target.localScale = Vector3.LerpUnclamped(Vector3.zero, endScale, a);
            t += Time.deltaTime;
            yield return null;
        }
        if (target != null)
            target.localScale = endScale;

        if (target != null)
            Destroy(target.gameObject);
    }

    private IEnumerator RepulsionWaveRoutine(Vector2 center, int activatorPlayer)
    {
        float duration = Mathf.Max(0.01f, repulsionWaveDuration);
        float elapsed = 0f;
        HashSet<int> affectedObjects = new HashSet<int>();

        int projectileLayer = LayerMask.NameToLayer("Projectile");
        int projectileEnemyLayer = LayerMask.NameToLayer("ProjectileEnemy");
        int interactiveAsteroidLayer = LayerMask.NameToLayer("InteractiveAsteroid");

        int mask = 0;
        if (projectileLayer != -1) mask |= 1 << projectileLayer;
        if (projectileEnemyLayer != -1) mask |= 1 << projectileEnemyLayer;
        if (interactiveAsteroidLayer != -1) mask |= 1 << interactiveAsteroidLayer;
        if (mask == 0) mask = Physics2D.AllLayers;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentRadius = Mathf.Lerp(0f, repulsionRadius, t);

            ApplyWaveToShip(player1Ship, center, activatorPlayer, currentRadius);
            ApplyWaveToShip(player2Ship, center, activatorPlayer, currentRadius);
            ApplyWaveToObjects(center, currentRadius, mask, projectileLayer, projectileEnemyLayer, affectedObjects);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyWaveToShip(player1Ship, center, activatorPlayer, repulsionRadius);
        ApplyWaveToShip(player2Ship, center, activatorPlayer, repulsionRadius);
        ApplyWaveToObjects(center, repulsionRadius, mask, projectileLayer, projectileEnemyLayer, affectedObjects);
    }

    private void ApplyWaveToObjects(
        Vector2 center,
        float currentRadius,
        int mask,
        int projectileLayer,
        int projectileEnemyLayer,
        HashSet<int> affectedObjects
    )
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, currentRadius, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            GameObject root = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.transform.root.gameObject;
            int id = root.GetInstanceID();

            // Verificar si es un proyectil (por componente, más fiable que solo por layer)
            Projectile projectile = root.GetComponentInChildren<Projectile>(true);
            if (projectile != null)
            {
                if (!affectedObjects.Contains(id))
                {
                    affectedObjects.Add(id);
                    Destroy(projectile.gameObject);
                }
                continue;
            }

            SlowGrandeProjectile slowGrande = root.GetComponentInChildren<SlowGrandeProjectile>(true);
            if (slowGrande != null)
            {
                if (!affectedObjects.Contains(id))
                {
                    affectedObjects.Add(id);
                    Destroy(slowGrande.gameObject);
                }
                continue;
            }

            HomingMissile homingMissile = root.GetComponentInChildren<HomingMissile>(true);
            if (homingMissile != null)
            {
                if (!affectedObjects.Contains(id))
                {
                    affectedObjects.Add(id);
                    Destroy(homingMissile.gameObject);
                }
                continue;
            }

            SplittableObject splittable = root.GetComponentInChildren<SplittableObject>(true);
            if (splittable != null)
            {
                if (!affectedObjects.Contains(id))
                {
                    affectedObjects.Add(id);
                    Vector2 hitDir = (root.transform.position - (Vector3)center).normalized;
                    splittable.Split(hitDir);
                }
                continue;
            }

            // También verificar por layer, por si acaso
            int layer = root.layer;
            if ((projectileLayer != -1 && layer == projectileLayer) || (projectileEnemyLayer != -1 && layer == projectileEnemyLayer))
            {
                if (!affectedObjects.Contains(id))
                {
                    affectedObjects.Add(id);
                    Destroy(root);
                }
                continue;
            }

            InteractiveAsteroid asteroid = root.GetComponent<InteractiveAsteroid>();
            if (asteroid == null)
                asteroid = hit.GetComponentInParent<InteractiveAsteroid>();
            if (asteroid == null) continue;

            Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            Vector2 dir = (rb.position - center);
            if (dir.sqrMagnitude < 0.0001f)
                dir = Random.insideUnitCircle;

            float dist = dir.magnitude;
            if (dist > currentRadius) continue;

            float falloff = 1f - Mathf.Clamp01(dist / repulsionRadius);
            rb.AddForce(dir.normalized * repulsionKnockback * falloff * Time.deltaTime * 60f, ForceMode2D.Force);
        }
    }

    private void ApplyWaveToShip(SpaceShipController ship, Vector2 center, int activatorPlayer, float currentRadius)
    {
        if (ship == null) return;

        Vector2 dir = ((Vector2)ship.transform.position - center);
        float dist = dir.magnitude;

        if (dist > currentRadius) return;

        if (ship == player1Ship && activatorPlayer == 1) return;
        if (ship == player2Ship && activatorPlayer == 2) return;

        float falloff = 1f - Mathf.Clamp01(dist / repulsionRadius);
        ship.AddImpulse(dir.normalized * repulsionKnockback * falloff * Time.deltaTime * 60f);
    }

    private void EnsureShipReferences()
    {
        if (player1Ship == null)
        {
            GameObject p1 = GameObject.FindGameObjectWithTag("Player1");
            if (p1 != null) player1Ship = p1.GetComponent<SpaceShipController>();
        }

        if (player2Ship == null)
        {
            GameObject p2 = GameObject.FindGameObjectWithTag("Player2");
            if (p2 != null) player2Ship = p2.GetComponent<SpaceShipController>();
        }
    }

    private void Start()
    {
        InitializeHUDs();
    }

    private void InitializeHUDs()
    {
        // Buscar Holders automáticamente si no están asignados
        if (holderPlayer1 == null)
        {
            GameObject p1 = GameObject.FindGameObjectWithTag("Player1");
            if (p1 != null) holderPlayer1 = p1.GetComponent<PowerUpHolder>();
        }

        if (holderPlayer2 == null)
        {
            GameObject p2 = GameObject.FindGameObjectWithTag("Player2");
            if (p2 != null) holderPlayer2 = p2.GetComponent<PowerUpHolder>();
        }

        // Trackear los Holders con los HUDs
        if (hudPlayer1 != null && holderPlayer1 != null)
        {
            hudPlayer1.TrackHolder(holderPlayer1);
            Debug.Log("[SpacePowerUpManager] HUD Player 1 inicializado");
        }
        else if (hudPlayer1 != null)
        {
            Debug.LogWarning("[SpacePowerUpManager] No se encontró PowerUpHolder para Player 1");
        }

        if (hudPlayer2 != null && holderPlayer2 != null)
        {
            hudPlayer2.TrackHolder(holderPlayer2);
            Debug.Log("[SpacePowerUpManager] HUD Player 2 inicializado");
        }
        else if (hudPlayer2 != null)
        {
            Debug.LogWarning("[SpacePowerUpManager] No se encontró PowerUpHolder para Player 2");
        }
    }
}

