using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// SETUP del prefab:
///   - CircleCollider2D con Is Trigger = true, radio ajustable
///   - SpriteRenderer opcional para visualizar el campo
///   - Este script
[RequireComponent(typeof(CircleCollider2D))]
public class SlowField : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private bool showRuntimeCircle = true;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float circleLineWidth = 0.06f;
    [SerializeField] private Color circleColor = new Color(0.55f, 0.85f, 1f, 0.9f);
    [SerializeField] private float circleGrowTime = 0.15f;

    [Header("Editor")]
    [SerializeField] private bool showRangeGizmo = true;
    [SerializeField] private Color rangeGizmoColor = new Color(0.55f, 0.85f, 1f, 0.9f);

    [Header("Configuracion")]
    [Tooltip("Multiplicador de velocidad aplicado a naves dentro del campo (0.3 = 30% de velocidad)")]
    [SerializeField] private float speedMultiplier = 0.3f;

    [Tooltip("Multiplicador aplicado a proyectiles dentro del campo")]
    [SerializeField] private float bulletSpeedMultiplier = 0.4f;

    [Tooltip("Duracion del campo en segundos")]
    [SerializeField] private float duration = 4f;

    // Naves dentro del campo en este momento
    private List<SpaceShipController> shipsInside = new List<SpaceShipController>();
    // Proyectiles dentro del campo
    private List<Rigidbody2D> bulletsInside = new List<Rigidbody2D>();

    // Velocidades originales guardadas para restaurar
    private Dictionary<SpaceShipController, float> originalMaxSpeeds = new Dictionary<SpaceShipController, float>();
    private Dictionary<SpaceShipController, float> originalAccelerations = new Dictionary<SpaceShipController, float>();
    private LineRenderer circleRenderer;

    private void Start()
    {
        if (showRuntimeCircle)
            CreateCircle();

        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
            ps.Play(true);

        StartCoroutine(LifetimeRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nave
        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship != null && !shipsInside.Contains(ship))
        {
            shipsInside.Add(ship);
            ApplySlowToShip(ship);
            return;
        }

        // Proyectil (tiene Projectile y Rigidbody2D)
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null && !bulletsInside.Contains(rb))
            {
                bulletsInside.Add(rb);
                rb.linearVelocity *= bulletSpeedMultiplier;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Nave sale del campo: restaurar velocidad
        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship != null && shipsInside.Contains(ship))
        {
            shipsInside.Remove(ship);
            RestoreShip(ship);
            return;
        }

        // Proyectil sale: no restauramos porque el campo ya lo afecto mientras estuvo dentro
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null) bulletsInside.Remove(rb);
        }
    }

    private void ApplySlowToShip(SpaceShipController ship)
    {
        if (originalMaxSpeeds.ContainsKey(ship)) return;

        // Guardar valores originales
        originalMaxSpeeds[ship] = ship.MaxSpeed;
        originalAccelerations[ship] = ship.Acceleration;

        // Aplicar slow
        ship.SetMaxSpeed(ship.MaxSpeed * speedMultiplier);
        ship.SetAcceleration(ship.Acceleration * speedMultiplier);

        // Frenar la velocidad actual inmediatamente
        ship.SetVelocity(ship.GetVelocity() * speedMultiplier);
    }

    private void RestoreShip(SpaceShipController ship)
    {
        if (!originalMaxSpeeds.ContainsKey(ship)) return;

        ship.SetMaxSpeed(originalMaxSpeeds[ship]);
        ship.SetAcceleration(originalAccelerations[ship]);

        originalMaxSpeeds.Remove(ship);
        originalAccelerations.Remove(ship);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);

        // Restaurar todas las naves que siguen dentro al expirar
        foreach (var ship in new List<SpaceShipController>(shipsInside))
            RestoreShip(ship);

        Destroy(gameObject);
    }

    private void CreateCircle()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) return;

        int segments = Mathf.Max(8, circleSegments);
        GameObject circleObj = new GameObject("SlowFieldCircle");
        circleObj.transform.SetParent(transform, false);
        circleObj.transform.localPosition = Vector3.zero;
        circleObj.transform.localRotation = Quaternion.identity;
        circleObj.transform.localScale = Vector3.zero;

        circleRenderer = circleObj.AddComponent<LineRenderer>();
        circleRenderer.useWorldSpace = false;
        circleRenderer.loop = true;
        circleRenderer.positionCount = segments;
        circleRenderer.startWidth = circleLineWidth;
        circleRenderer.endWidth = circleLineWidth;
        circleRenderer.startColor = circleColor;
        circleRenderer.endColor = circleColor;
        circleRenderer.numCapVertices = 2;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            circleRenderer.material = new Material(shader);

        float step = (Mathf.PI * 2f) / segments;
        float r = Mathf.Max(0.01f, col.radius);
        for (int i = 0; i < segments; i++)
        {
            float angle = step * i;
            circleRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
        }

        StartCoroutine(GrowCircleRoutine(circleObj.transform));
    }

    private IEnumerator GrowCircleRoutine(Transform t)
    {
        float d = Mathf.Max(0.01f, circleGrowTime);
        float e = 0f;
        while (e < d)
        {
            float a = e / d;
            a = a * a * (3f - 2f * a);
            if (t != null)
                t.localScale = Vector3.one * a;
            e += Time.deltaTime;
            yield return null;
        }
        if (t != null)
            t.localScale = Vector3.one;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo) return;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) return;

        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        float r = col.radius * scale;
        Vector3 center = transform.TransformPoint(col.offset);

        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawWireSphere(center, r);
    }
}
