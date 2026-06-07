using UnityEngine;
using System.Collections.Generic;

/// SpaceBackgroundShips
///
/// SETUP:
///   - GO en la escena con este script
///   - shipPrefab: prefab con solo SpriteRenderer (sin collider, sin Rigidbody)
///   - Si tenes un BoxCollider2D en la zona, asignalo a zoneBounds
///   - Si no, configura manualCenter y manualSize en el Inspector

public class SpaceBackgroundShips : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab de la nave de fondo. Solo necesita SpriteRenderer.")]
    [SerializeField] private GameObject shipPrefab;

    [Header("Cantidad")]
    [SerializeField] private int shipCount = 10;

    [Header("Velocidad")]
    [SerializeField] private float minSpeed = 1.5f;
    [SerializeField] private float maxSpeed = 4f;

    [Header("Escala")]
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 0.8f;

    [Header("Opacidad")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.08f;
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.22f;

    [Header("Sprite rotation offset")]
    [Tooltip("0 si el sprite apunta a la derecha, -90 si apunta arriba")]
    [SerializeField] private float spriteRotationOffset = 0f;

    [Header("Limites de la zona")]
    [Tooltip("BoxCollider2D de la zona. Si no hay, usar los valores manuales de abajo.")]
    [SerializeField] private BoxCollider2D zoneBounds;
    [Tooltip("Centro manual si no hay zoneBounds")]
    [SerializeField] private Vector2 manualCenter = Vector2.zero;
    [Tooltip("Tamano manual si no hay zoneBounds")]
    [SerializeField] private Vector2 manualSize = new Vector2(20f, 14f);

    private static readonly Vector2[] directions = new Vector2[]
    {
        new Vector2(1, 0),
        new Vector2(-1, 0),
        new Vector2(0, 1),
        new Vector2(0, -1),
        new Vector2(1, 1).normalized,
        new Vector2(-1, 1).normalized,
        new Vector2(1, -1).normalized,
        new Vector2(-1, -1).normalized,
    };

    private class BackgroundShipData
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector2 direction;
        public float speed;
    }

    private List<BackgroundShipData> ships = new List<BackgroundShipData>();
    private bool isRunning = false;

    public void Activate()
    {
        if (isRunning) return;
        isRunning = true;

        CreatePool();

        Bounds b = GetZoneBounds();
        Debug.Log($"[SpaceBackgroundShips] Activando. Bounds center={b.center} size={b.size}. Naves={ships.Count}");

        if (b.size == Vector3.zero)
            Debug.LogError("[SpaceBackgroundShips] Bounds son CERO. Asigna zoneBounds o configura manualCenter/manualSize.");

        foreach (var ship in ships)
        {
            PlaceShipRandom(ship);
            ship.go.SetActive(true);
        }
    }

    public void Deactivate()
    {
        isRunning = false;
        foreach (var ship in ships)
        {
            if (ship.go != null)
                ship.go.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        Bounds bounds = GetZoneBounds();

        foreach (var ship in ships)
        {
            if (ship.go == null || !ship.go.activeSelf) continue;

            ship.go.transform.position += (Vector3)(ship.direction * ship.speed * Time.deltaTime);

            Vector2 pos = ship.go.transform.position;
            if (!bounds.Contains(new Vector3(pos.x, pos.y, 0f)))
                RecycleShip(ship, bounds);
        }
    }

    private void CreatePool()
    {
        foreach (var s in ships)
        {
            if (s.go != null) Destroy(s.go);
        }
        ships.Clear();

        if (shipPrefab == null)
        {
            Debug.LogError("[SpaceBackgroundShips] shipPrefab es NULL. Asignalo en el Inspector.");
            return;
        }

        for (int i = 0; i < shipCount; i++)
        {
            GameObject go = Instantiate(shipPrefab, transform);
            go.name = "BgShip_" + i;
            go.SetActive(false);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                Debug.LogWarning("[SpaceBackgroundShips] El prefab no tiene SpriteRenderer en el root.");

            Collider2D col = go.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            ships.Add(new BackgroundShipData
            {
                go = go,
                sr = sr,
                direction = Vector2.right,
                speed = 1f
            });
        }
    }

    private void PlaceShipRandom(BackgroundShipData ship)
    {
        Bounds bounds = GetZoneBounds();
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        ship.go.transform.position = new Vector3(x, y, 0f);
        AssignRandomProperties(ship);
    }

    private void RecycleShip(BackgroundShipData ship, Bounds bounds)
    {
        Vector2 dir = ship.direction;
        Vector2 pos = ship.go.transform.position;

        float newX = pos.x;
        float newY = pos.y;

        if (pos.x > bounds.max.x) newX = bounds.min.x;
        else if (pos.x < bounds.min.x) newX = bounds.max.x;

        if (pos.y > bounds.max.y) newY = bounds.min.y;
        else if (pos.y < bounds.min.y) newY = bounds.max.y;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            newY = Random.Range(bounds.min.y, bounds.max.y);
        else
            newX = Random.Range(bounds.min.x, bounds.max.x);

        ship.go.transform.position = new Vector3(newX, newY, 0f);
    }

    private void AssignRandomProperties(BackgroundShipData ship)
    {
        ship.direction = directions[Random.Range(0, directions.Length)];
        ship.speed = Random.Range(minSpeed, maxSpeed);

        float scale = Random.Range(minScale, maxScale);
        ship.go.transform.localScale = new Vector3(scale, scale, 1f);

        float angle = Mathf.Atan2(ship.direction.y, ship.direction.x) * Mathf.Rad2Deg + spriteRotationOffset;
        ship.go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (ship.sr != null)
        {
            float alpha = Random.Range(minAlpha, maxAlpha);
            Color c = ship.sr.color;
            c.a = alpha;
            ship.sr.color = c;
        }
    }

    private Bounds GetZoneBounds()
    {
        if (zoneBounds != null)
            return zoneBounds.bounds;

        return new Bounds(manualCenter, manualSize);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Bounds b = GetZoneBounds();
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
