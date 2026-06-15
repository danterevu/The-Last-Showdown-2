using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float speedMultiplier = 0.3f;
    [SerializeField] private float bulletSpeedMultiplier = 0.4f;
    [SerializeField] private float duration = 4f;

    [Header("Alien")]
    [Tooltip("Multiplicador de velocidad aplicado a aliens dentro del campo")]
    [SerializeField] private float alienSpeedMultiplier = 0.3f;

    private List<SpaceShipController> shipsInside = new List<SpaceShipController>();
    private List<Rigidbody2D> bulletsInside = new List<Rigidbody2D>();
    private List<SpaceAlien> aliensInside = new List<SpaceAlien>();

    private Dictionary<SpaceShipController, float> originalMaxSpeeds = new Dictionary<SpaceShipController, float>();
    private Dictionary<SpaceShipController, float> originalAccelerations = new Dictionary<SpaceShipController, float>();
    private LineRenderer circleRenderer;

    private int ownerPlayer = 0;

    public void SetOwnerPlayer(int player) { ownerPlayer = player; }

    private void Start()
    {
        if (showRuntimeCircle) CreateCircle();
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true)) ps.Play(true);
        StartCoroutine(LifetimeRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship != null && !shipsInside.Contains(ship)) { shipsInside.Add(ship); ApplySlowToShip(ship); return; }

        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null && !bulletsInside.Contains(rb)) { bulletsInside.Add(rb); rb.linearVelocity *= bulletSpeedMultiplier; }
        }

        SpaceAlien alien = other.GetComponent<SpaceAlien>();
        if (alien != null && !aliensInside.Contains(alien))
        {
            aliensInside.Add(alien);
            alien.ApplySlowEffect(alienSpeedMultiplier, duration);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship != null && shipsInside.Contains(ship)) { shipsInside.Remove(ship); RestoreShip(ship); return; }

        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null) { Rigidbody2D rb = other.GetComponent<Rigidbody2D>(); if (rb != null) bulletsInside.Remove(rb); }

        SpaceAlien alien = other.GetComponent<SpaceAlien>();
        if (alien != null) aliensInside.Remove(alien);
    }

    private void ApplySlowToShip(SpaceShipController ship)
    {
        if (!originalMaxSpeeds.ContainsKey(ship))
        {
            originalMaxSpeeds[ship] = ship.OriginalMaxSpeed;
            originalAccelerations[ship] = ship.OriginalAcceleration;
        }

        ship.SetMaxSpeed(originalMaxSpeeds[ship] * speedMultiplier);
        ship.SetAcceleration(originalAccelerations[ship] * speedMultiplier);
        ship.SetVelocity(ship.GetVelocity() * speedMultiplier);
        ship.SetInSlowField(true);
    }

    private void RestoreShip(SpaceShipController ship)
    {
        if (!originalMaxSpeeds.ContainsKey(ship)) return;
        ship.SetInSlowField(false);

        if (!ship.isInSlowField)
        {
            ship.SetMaxSpeed(originalMaxSpeeds[ship]);
            ship.SetAcceleration(originalAccelerations[ship]);
        }

        originalMaxSpeeds.Remove(ship);
        originalAccelerations.Remove(ship);
    }

    public static void RemoveShipFromAllSlowFields(SpaceShipController ship)
    {
        SlowField[] allSlowFields = FindObjectsByType<SlowField>(FindObjectsSortMode.None);
        foreach (SlowField field in allSlowFields)
            if (field.shipsInside.Contains(ship)) { field.shipsInside.Remove(ship); field.RestoreShip(ship); }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);
        foreach (var ship in new List<SpaceShipController>(shipsInside)) RestoreShip(ship);
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
        if (shader != null) circleRenderer.material = new Material(shader);

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
            float a = e / d; a = a * a * (3f - 2f * a);
            if (t != null) t.localScale = Vector3.one * a;
            e += Time.deltaTime;
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo) return;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) return;
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        Gizmos.color = rangeGizmoColor;
        Gizmos.DrawWireSphere(transform.TransformPoint(col.offset), col.radius * scale);
    }
}