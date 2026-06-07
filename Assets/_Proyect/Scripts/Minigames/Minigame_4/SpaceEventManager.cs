using System.Collections.Generic;
using UnityEngine;

public class SpaceEventManager : MonoBehaviour
{
    public static SpaceEventManager Instance { get; private set; }

    [Header("Configuracion General")]
    [SerializeField] private int totalZones = 5;

    [Header("Eventos de Zona Fija")]
    [SerializeField] private FixedZoneEvent[] fixedZoneEvents;

    [Header("Pool de Eventos Random")]
    [SerializeField] private SpaceEventType[] randomEventPool;

    [Range(0f, 1f)]
    [SerializeField] private float randomChance = 0.5f;

    [Header("Battle Royale (Universal - todas las zonas)")]
    [SerializeField] private SpaceBattleRoyaleZone battleRoyaleZone;

    [Header("Evento - Nave Mediana (MediumShip)")]
    [Tooltip("Arrastra el GO de la escena con SpaceMediumShipEvent. NO el prefab.")]
    [SerializeField] private SpaceMediumShipEvent mediumShipEvent;

    [Header("Evento - Naves Alien (AlienShips)")]
    [Tooltip("Arrastra el GO de la escena con SpaceAlienShipsEvent. NO el prefab.")]
    [SerializeField] private SpaceAlienShipsEvent alienShipsEvent;

    private SpaceEventType[] zoneSortedEvent;
    private HashSet<int> fixedZoneIndices = new HashSet<int>();
    private int currentZoneIndex = -1;

    [System.Serializable]
    public class FixedZoneEvent
    {
        public int zoneIndex;
        public SpaceEventType eventType;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SortEvents();
    }

    private void SortEvents()
    {
        zoneSortedEvent = new SpaceEventType[totalZones];
        fixedZoneIndices.Clear();

        if (fixedZoneEvents != null)
        {
            foreach (var fe in fixedZoneEvents)
            {
                if (fe.zoneIndex >= 0 && fe.zoneIndex < totalZones)
                {
                    fixedZoneIndices.Add(fe.zoneIndex);
                    zoneSortedEvent[fe.zoneIndex] = fe.eventType;
                }
            }
        }

        for (int i = 0; i < totalZones; i++)
        {
            if (fixedZoneIndices.Contains(i)) continue;

            if (Random.value < randomChance && randomEventPool != null && randomEventPool.Length > 0)
                zoneSortedEvent[i] = randomEventPool[Random.Range(0, randomEventPool.Length)];
            else
                zoneSortedEvent[i] = SpaceEventType.None;
        }

        LogSortResult();
    }

    public void SetActiveZone(int zoneIndex)
    {
        if (currentZoneIndex >= 0)
            DeactivateZoneEvent(currentZoneIndex);

        battleRoyaleZone?.ForceStop();
        currentZoneIndex = zoneIndex;

        if (zoneIndex >= 0 && zoneIndex < totalZones)
            ActivateZoneEvent(zoneIndex);
    }

    public void NotifyKill()
    {
        battleRoyaleZone?.ResetTimer();
        mediumShipEvent?.NotifyKill();

        if (currentZoneIndex >= 0 && currentZoneIndex < totalZones
            && zoneSortedEvent[currentZoneIndex] == SpaceEventType.AlienShips)
        {
            alienShipsEvent?.NotifyKill();
        }
    }

    private void ActivateZoneEvent(int zoneIndex)
    {
        SpaceEventType eventType = zoneSortedEvent[zoneIndex];
        bool isFixed = fixedZoneIndices.Contains(zoneIndex);
        Debug.Log($"[SpaceEventManager] Zona {zoneIndex} -> Evento: {eventType} ({(isFixed ? "FIJO" : "RANDOM")})");

        switch (eventType)
        {
            case SpaceEventType.None: break;
            case SpaceEventType.BlackHole: ActivateBlackHole(zoneIndex); break;
            case SpaceEventType.DimensionalPortals: ActivateDimensionalPortals(zoneIndex); break;
            case SpaceEventType.PowerUpRain: ActivatePowerUpRain(zoneIndex); break;
            case SpaceEventType.AlienShips: ActivateAlienShips(zoneIndex); break;
            case SpaceEventType.MediumShip: ActivateMediumShip(zoneIndex); break;
        }
    }

    private void DeactivateZoneEvent(int zoneIndex)
    {
        SpaceEventType eventType = zoneSortedEvent[zoneIndex];

        switch (eventType)
        {
            case SpaceEventType.BlackHole: DeactivateBlackHole(zoneIndex); break;
            case SpaceEventType.DimensionalPortals: DeactivateDimensionalPortals(zoneIndex); break;
            case SpaceEventType.PowerUpRain: DeactivatePowerUpRain(zoneIndex); break;
            case SpaceEventType.AlienShips: DeactivateAlienShips(zoneIndex); break;
            case SpaceEventType.MediumShip: DeactivateMediumShip(zoneIndex); break;
        }
    }

    private void ActivateBlackHole(int zone)
    {
        Debug.Log($"[SpaceEventManager] ACTIVAR BlackHole en zona {zone}");
        // TODO: SpaceBlackHoleEvent.Instance?.Activate(zone);
    }

    private void DeactivateBlackHole(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR BlackHole en zona {zone}");
        // TODO: SpaceBlackHoleEvent.Instance?.Deactivate();
    }

    private void ActivateDimensionalPortals(int zone)
    {
        Debug.Log($"[SpaceEventManager] ACTIVAR DimensionalPortals en zona {zone}");
        // TODO: SpacePortalEvent.Instance?.Activate(zone);
    }

    private void DeactivateDimensionalPortals(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR DimensionalPortals en zona {zone}");
        // TODO: SpacePortalEvent.Instance?.Deactivate();
    }

    private void ActivatePowerUpRain(int zone)
    {
        Debug.Log($"[SpaceEventManager] ACTIVAR PowerUpRain en zona {zone}");
        // TODO: SpacePowerUpRainEvent.Instance?.Activate(zone);
    }

    private void DeactivatePowerUpRain(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR PowerUpRain en zona {zone}");
        // TODO: SpacePowerUpRainEvent.Instance?.Deactivate();
    }

    private void ActivateAlienShips(int zone)
    {
        Debug.Log($"[SpaceEventManager] ACTIVAR AlienShips en zona {zone}");

        if (alienShipsEvent == null)
        {
            Debug.LogError("[SpaceEventManager] alienShipsEvent es NULL. Arrastra el GO de la escena con SpaceAlienShipsEvent en el Inspector.");
            return;
        }

        alienShipsEvent.Activate();
    }

    private void DeactivateAlienShips(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR AlienShips en zona {zone}");
        alienShipsEvent?.Deactivate();
    }

    private void ActivateMediumShip(int zone)
    {
        Debug.Log($"[SpaceEventManager] ACTIVAR MediumShip en zona {zone}");

        if (mediumShipEvent == null)
        {
            Debug.LogError("[SpaceEventManager] mediumShipEvent es NULL. Arrastra el GO de la escena con SpaceMediumShipEvent en el Inspector.");
            return;
        }

        mediumShipEvent.Activate();
    }

    private void DeactivateMediumShip(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR MediumShip en zona {zone}");
        mediumShipEvent?.Deactivate();
    }

    private void LogSortResult()
    {
        Debug.Log("=== SORTEO DE EVENTOS - RESULTADO ===");
        for (int i = 0; i < totalZones; i++)
        {
            bool isFixed = fixedZoneIndices.Contains(i);
            string tag = isFixed ? "[FIJO]   " : "[RANDOM] ";
            string eventName = zoneSortedEvent[i] == SpaceEventType.None ? "Sin evento" : zoneSortedEvent[i].ToString();
            Debug.Log($"  Zona {i}: {tag}{eventName}");
        }
    }

    [ContextMenu("Re-sortear Eventos (Debug)")]
    private void DebugResort() { SortEvents(); }
}