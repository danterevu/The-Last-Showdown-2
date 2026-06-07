using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// SpaceMediumShipEvent
///
/// SETUP en la escena:
///   1. GO activo en la escena con este script (nunca desactivarlo desde el Inspector)
///   2. crossingShipPrefab: prefab con SpaceCrossingShip.cs
///   3. backgroundShips: arrastra el GO hijo que tiene SpaceBackgroundShips
///   4. routes: GOs vacios startPoint y endPoint para cada ruta
///   5. En SpaceEventManager: arrastra este GO al campo mediumShipEvent

public class SpaceMediumShipEvent : MonoBehaviour
{
    public static SpaceMediumShipEvent Instance { get; private set; }

    [Header("Prefab de la nave cruzadora")]
    [SerializeField] private GameObject crossingShipPrefab;

    [Header("Naves de fondo")]
    [SerializeField] private SpaceBackgroundShips backgroundShips;

    [Header("Rutas (GOs vacios en la escena, fuera de la zona)")]
    [SerializeField] private CrossingRoute[] routes;

    [Header("Probabilidad del evento")]
    [Tooltip("Probabilidad inicial al entrar a la zona (0 a 1)")]
    [SerializeField] private float baseChance = 0.30f;
    [Tooltip("Cuanto sube por segundo (0 a 1)")]
    [SerializeField] private float chanceIncreasePerSecond = 0.02f;
    [Tooltip("Segundos de espera tras un evento antes de volver a contar")]
    [SerializeField] private float cooldownAfterEvent = 2f;

    [Header("Nave doble")]
    [Tooltip("Probabilidad inicial de evento doble (0 a 1)")]
    [SerializeField] private float baseDoubleShipChance = 0.20f;
    [Tooltip("Cuanto sube la prob de doble por cada nave sola que paso (0 a 1)")]
    [SerializeField] private float doubleShipChancePerSingle = 0.05f;
    [Tooltip("Segundos entre la primera y segunda nave en evento doble")]
    [SerializeField] private float delayBetweenDoubleShips = 1f;

    [Header("Velocidad de las naves cruzadoras")]
    [SerializeField] private float crossingSpeed = 12f;

    [System.Serializable]
    public class CrossingRoute
    {
        public Transform startPoint;
        public Transform endPoint;
    }

    private float currentChance;
    private float currentDoubleChance;
    private int singleShipPassCount = 0;
    private bool isActive = false;
    private Coroutine mainLoopCoroutine;

    private List<SpaceCrossingShip> shipPool = new List<SpaceCrossingShip>();
    private const int PoolSize = 2;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CreatePool();
        Debug.Log("[SpaceMediumShipEvent] Start() ejecutado. Pool creado con " + shipPool.Count + " naves.");
    }

    // -------------------------------------------------------------------------
    //  API PUBLICA
    // -------------------------------------------------------------------------

    public void Activate()
    {
        if (isActive)
        {
            Debug.Log("[SpaceMediumShipEvent] Activate() llamado pero ya estaba activo.");
            return;
        }

        isActive = true;
        singleShipPassCount = 0;
        ResetProbability();

        backgroundShips?.Activate();

        if (mainLoopCoroutine != null)
            StopCoroutine(mainLoopCoroutine);

        mainLoopCoroutine = StartCoroutine(MainLoop());

        Debug.Log("[SpaceMediumShipEvent] Evento ACTIVADO. baseChance=" + baseChance + " chanceIncrease=" + chanceIncreasePerSecond);
    }

    public void Deactivate()
    {
        if (!isActive) return;

        isActive = false;

        if (mainLoopCoroutine != null)
        {
            StopCoroutine(mainLoopCoroutine);
            mainLoopCoroutine = null;
        }

        foreach (var ship in shipPool)
            ship.ForceDeactivate();

        backgroundShips?.Deactivate();

        Debug.Log("[SpaceMediumShipEvent] Evento DESACTIVADO.");
    }

    public void NotifyKill()
    {
        if (!isActive) return;

        Debug.Log("[SpaceMediumShipEvent] Kill notificada: reiniciando probabilidad y contador.");
        ResetProbability();

        if (mainLoopCoroutine != null)
            StopCoroutine(mainLoopCoroutine);

        mainLoopCoroutine = StartCoroutine(MainLoop());
    }

    public void OnCrossingShipFinished(SpaceCrossingShip ship)
    {
        Debug.Log("[SpaceMediumShipEvent] Nave cruzadora termino su recorrido.");
    }

    // -------------------------------------------------------------------------
    //  LOOP PRINCIPAL
    // -------------------------------------------------------------------------

    private IEnumerator MainLoop()
    {
        Debug.Log("[SpaceMediumShipEvent] MainLoop iniciado. Chance actual: " + currentChance);

        while (isActive)
        {
            yield return new WaitForSeconds(1f);

            if (!isActive) yield break;

            currentChance += chanceIncreasePerSecond;
            currentChance = Mathf.Clamp01(currentChance);

            float roll = Random.value;
            Debug.Log("[SpaceMediumShipEvent] Tick. Chance=" + currentChance.ToString("F2") + " Roll=" + roll.ToString("F2"));

            if (roll <= currentChance)
            {
                float doubleRoll = Random.value;
                bool isDouble = doubleRoll <= currentDoubleChance;

                Debug.Log("[SpaceMediumShipEvent] EVENTO DISPARADO! Doble=" + isDouble);

                yield return StartCoroutine(LaunchEvent(isDouble));

                ResetProbability();

                yield return new WaitForSeconds(cooldownAfterEvent);
            }
        }
    }

    private IEnumerator LaunchEvent(bool isDouble)
    {
        int routeIndex1 = GetRandomRouteIndex(-1);
        if (routeIndex1 < 0)
        {
            Debug.LogError("[SpaceMediumShipEvent] No hay rutas validas configuradas.");
            yield break;
        }

        LaunchShip(routeIndex1);

        if (!isDouble)
        {
            singleShipPassCount++;
            currentDoubleChance = Mathf.Clamp01(baseDoubleShipChance + singleShipPassCount * doubleShipChancePerSingle);
            Debug.Log("[SpaceMediumShipEvent] Nave sola #" + singleShipPassCount + ". DoubleChance=" + currentDoubleChance.ToString("F2"));
        }
        else
        {
            yield return new WaitForSeconds(delayBetweenDoubleShips);

            int routeIndex2 = GetRandomRouteIndex(routeIndex1);
            if (routeIndex2 >= 0)
                LaunchShip(routeIndex2);
            else
                Debug.LogWarning("[SpaceMediumShipEvent] No hay segunda ruta disponible para evento doble.");

            singleShipPassCount = 0;
        }
    }

    private void LaunchShip(int routeIndex)
    {
        SpaceCrossingShip ship = GetAvailableShip();
        if (ship == null)
        {
            Debug.LogWarning("[SpaceMediumShipEvent] Pool sin naves disponibles.");
            return;
        }

        CrossingRoute route = routes[routeIndex];
        if (route.startPoint == null || route.endPoint == null)
        {
            Debug.LogError("[SpaceMediumShipEvent] La ruta " + routeIndex + " tiene startPoint o endPoint NULL.");
            return;
        }

        Vector2 from = route.startPoint.position;
        Vector2 to = route.endPoint.position;
        ship.Launch(from, to, crossingSpeed);
    }

    // -------------------------------------------------------------------------
    //  POOL
    // -------------------------------------------------------------------------

    private void CreatePool()
    {
        foreach (var s in shipPool)
        {
            if (s != null && s.gameObject != null)
                Destroy(s.gameObject);
        }
        shipPool.Clear();

        if (crossingShipPrefab == null)
        {
            Debug.LogError("[SpaceMediumShipEvent] crossingShipPrefab es NULL. Asignalo en el Inspector.");
            return;
        }

        for (int i = 0; i < PoolSize; i++)
        {
            GameObject go = Instantiate(crossingShipPrefab, transform);
            go.name = "CrossingShip_" + i;
            go.SetActive(false);

            SpaceCrossingShip ship = go.GetComponent<SpaceCrossingShip>();
            if (ship == null)
            {
                Debug.LogError("[SpaceMediumShipEvent] El prefab no tiene SpaceCrossingShip.cs");
                Destroy(go);
                continue;
            }

            shipPool.Add(ship);
        }

        Debug.Log("[SpaceMediumShipEvent] Pool creado: " + shipPool.Count + " naves.");
    }

    private SpaceCrossingShip GetAvailableShip()
    {
        foreach (var ship in shipPool)
        {
            if (ship != null && !ship.gameObject.activeSelf)
                return ship;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    //  HELPERS
    // -------------------------------------------------------------------------

    private void ResetProbability()
    {
        currentChance = baseChance;
        currentDoubleChance = Mathf.Clamp01(baseDoubleShipChance + singleShipPassCount * doubleShipChancePerSingle);
        Debug.Log("[SpaceMediumShipEvent] Probabilidad reseteada. Chance=" + currentChance + " DoubleChance=" + currentDoubleChance);
    }

    private int GetRandomRouteIndex(int excludeIndex)
    {
        if (routes == null || routes.Length == 0) return -1;

        List<int> available = new List<int>();
        for (int i = 0; i < routes.Length; i++)
        {
            if (i == excludeIndex) continue;
            if (routes[i] == null) continue;
            if (routes[i].startPoint == null || routes[i].endPoint == null) continue;
            available.Add(i);
        }

        if (available.Count == 0) return -1;
        return available[Random.Range(0, available.Count)];
    }

    // -------------------------------------------------------------------------
    //  GIZMOS
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (routes == null) return;

        Color[] routeColors = new Color[]
        {
            new Color(1f, 0.5f, 0f, 0.9f),
            new Color(0f, 1f, 0.5f, 0.9f),
            new Color(0.5f, 0.5f, 1f, 0.9f),
            new Color(1f, 0f, 1f, 0.9f),
            new Color(1f, 1f, 0f, 0.9f),
            new Color(0f, 1f, 1f, 0.9f),
        };

        for (int i = 0; i < routes.Length; i++)
        {
            var route = routes[i];
            if (route == null || route.startPoint == null || route.endPoint == null) continue;

            Gizmos.color = routeColors[i % routeColors.Length];

            Vector3 start = route.startPoint.position;
            Vector3 end = route.endPoint.position;

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.5f);
            Gizmos.DrawWireSphere(end, 0.3f);

            Vector3 mid = (start + end) / 2f;
            Vector3 dir = (end - start).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
            float a = 0.4f;
            Gizmos.DrawLine(mid, mid - dir * a + perp * a * 0.5f);
            Gizmos.DrawLine(mid, mid - dir * a - perp * a * 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (routes == null) return;
        for (int i = 0; i < routes.Length; i++)
        {
            var route = routes[i];
            if (route == null || route.startPoint == null || route.endPoint == null) continue;
            UnityEditor.Handles.Label(route.startPoint.position + Vector3.up * 0.6f, "Ruta " + i + " START");
            UnityEditor.Handles.Label(route.endPoint.position + Vector3.up * 0.6f, "Ruta " + i + " END");
        }
    }
#endif
}
