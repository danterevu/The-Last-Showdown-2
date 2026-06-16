using UnityEngine;
using System.Collections.Generic;

public class SpaceBackgroundShips : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Array de prefabs de naves de fondo. Solo necesitan SpriteRenderer.")]
    [SerializeField] private GameObject[] shipPrefabs;

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

    // Solo 4 direcciones axiales para que no se vean raras
    private static readonly Vector2[] directions = new Vector2[]
    {
        new Vector2(1, 0),
        new Vector2(-1, 0),
        new Vector2(0, 1),
        new Vector2(0, -1),
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

    // -------------------------------------------------------------------------

    public void Activate()
    {
        if (isRunning) return;
        isRunning = true;

        CreatePool();

        Bounds b = GetZoneBounds();
        Debug.Log("[SpaceBackgroundShips] Activando. Bounds center=" + b.center + " size=" + b.size);

        if (b.size == Vector3.zero)
            Debug.LogError("[SpaceBackgroundShips] Bounds son CERO. Asigna zoneBounds o configura manualCenter/manualSize.");

        foreach (var ship in ships)
        {
            AssignRandomProperties(ship);
            PlaceShipAtEntryBorder(ship, GetZoneBounds());
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

            // Margen generoso para que no se recicle antes de entrar a la zona
            float margin = 1f;
            bool outX = pos.x > bounds.max.x + margin || pos.x < bounds.min.x - margin;
            bool outY = pos.y > bounds.max.y + margin || pos.y < bounds.min.y - margin;

            if (outX || outY)
            {
                // Nueva direccion y reentrada por el borde opuesto
                AssignRandomProperties(ship);
                PlaceShipAtEntryBorder(ship, bounds);
            }
        }
    }

    // -------------------------------------------------------------------------

    private void CreatePool()
    {
        foreach (var s in ships)
        {
            if (s.go != null) Destroy(s.go);
        }
        ships.Clear();

        if (shipPrefabs == null || shipPrefabs.Length == 0)
        {
            Debug.LogError("[SpaceBackgroundShips] shipPrefabs es NULL o vacío.");
            return;
        }

        for (int i = 0; i < shipCount; i++)
        {
            GameObject randomPrefab = shipPrefabs[Random.Range(0, shipPrefabs.Length)];
            GameObject go = Instantiate(randomPrefab, transform);
            go.name = "BgShip_" + i;
            go.SetActive(false);

            // Deshabilitar cualquier collider o rigidbody que pueda tener
            foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>())
                col.enabled = false;
            foreach (Rigidbody2D rb in go.GetComponentsInChildren<Rigidbody2D>())
                rb.simulated = false;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

            ships.Add(new BackgroundShipData
            {
                go = go,
                sr = sr,
                direction = Vector2.right,
                speed = minSpeed
            });
        }
    }

    /// Posiciona la nave en el borde de entrada segun su direccion de viaje.
    /// Si va hacia la derecha, entra por el borde izquierdo, etc.
    private void PlaceShipAtEntryBorder(BackgroundShipData ship, Bounds bounds)
    {
        Vector2 dir = ship.direction;
        float x, y;

        // Eje principal de movimiento determina en que borde aparece
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            // Movimiento horizontal
            // Entra por el borde opuesto a donde va
            x = dir.x > 0 ? bounds.min.x : bounds.max.x;
            y = Random.Range(bounds.min.y, bounds.max.y);
        }
        else
        {
            // Movimiento vertical
            x = Random.Range(bounds.min.x, bounds.max.x);
            y = dir.y > 0 ? bounds.min.y : bounds.max.y;
        }

        ship.go.transform.position = new Vector3(x, y, 0f);
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