using System.Collections.Generic;
using UnityEngine;

/// SpaceEventManager
///
/// SETUP en el Inspector:
///   1. totalZones           cuantas zonas tiene el minijuego (ej: 5)
///   2. fixedZoneEvents      lista de eventos de zona fija.
///                            Para cada entrada elegis: zoneIndex + eventType.
///                            Esas zonas quedan excluidas del sorteo random.
///   3. randomEventPool      que eventos random pueden sortear.
///   4. randomChance         probabilidad de evento random (default 0.5 = 50%).
///   5. mediumShipEvent      arrastra el GO que tiene SpaceMediumShipEvent (NO el prefab).
///
/// FLUJO:
///   - En Awake() se sortea todo.
///   - SpaceMinigame llama SetActiveZone(index) al cambiar de zona.
///   - El manager activa/desactiva los eventos.
///   - SpaceMinigame llama NotifyKill() en cada kill.

public class SpaceEventManager : MonoBehaviour
{
    public static SpaceEventManager Instance { get; private set; }

    [Header("Configuracion General")]
    [Tooltip("Cantidad total de zonas del minijuego")]
    [SerializeField] private int totalZones = 5;

    [Header("Eventos de Zona Fija")]
    [Tooltip("Define que evento fijo aparece en que zona. Esas zonas no participan del sorteo random.")]
    [SerializeField] private FixedZoneEvent[] fixedZoneEvents;

    [Header("Pool de Eventos Random")]
    [Tooltip("Que tipos de eventos pueden aparecer de forma aleatoria")]
    [SerializeField] private SpaceEventType[] randomEventPool;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que aparezca un evento random en una zona libre (0=nunca, 1=siempre)")]
    [SerializeField] private float randomChance = 0.5f;

    [Header("Battle Royale (Universal - todas las zonas)")]
    [Tooltip("El componente de la zona Battle Royale que se achica")]
    [SerializeField] private SpaceBattleRoyaleZone battleRoyaleZone;

    [Header("Evento - Nave Mediana (MediumShip)")]
    [Tooltip("Arrastra aqui el GO de la escena que tiene SpaceMediumShipEvent. NO el prefab de la nave.")]
    [SerializeField] private SpaceMediumShipEvent mediumShipEvent;

    // Para cada zona: que evento le toco (None = sin evento)
    private SpaceEventType[] zoneSortedEvent;

    // Zonas con evento fijo (excluidas del random)
    private HashSet<int> fixedZoneIndices = new HashSet<int>();

    // Zona activa actualmente
    private int currentZoneIndex = -1;

    [System.Serializable]
    public class FixedZoneEvent
    {
        [Tooltip("Indice de la zona (0 = primera zona)")]
        public int zoneIndex;
        [Tooltip("Que evento fijo aparece en esta zona")]
        public SpaceEventType eventType;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SortEvents();
    }

    private void SortEvents()
    {
        zoneSortedEvent = new SpaceEventType[totalZones];

        fixedZoneIndices.Clear();
        if (fixedZoneEvents != null)
        {
            foreach (var fixedEvent in fixedZoneEvents)
            {
                if (fixedEvent.zoneIndex >= 0 && fixedEvent.zoneIndex < totalZones)
                {
                    fixedZoneIndices.Add(fixedEvent.zoneIndex);
                    zoneSortedEvent[fixedEvent.zoneIndex] = fixedEvent.eventType;
                }
            }
        }

        for (int i = 0; i < totalZones; i++)
        {
            if (fixedZoneIndices.Contains(i)) continue;

            bool hasEvent = Random.value < randomChance;

            if (hasEvent && randomEventPool != null && randomEventPool.Length > 0)
            {
                int randomIndex = Random.Range(0, randomEventPool.Length);
                zoneSortedEvent[i] = randomEventPool[randomIndex];
            }
            else
            {
                zoneSortedEvent[i] = SpaceEventType.None;
            }
        }

        LogSortResult();
    }

    /// Llamado por SpaceMinigame.ActivateZone(). Activa el evento de la nueva zona.
    public void SetActiveZone(int zoneIndex)
    {
        // Desactivar evento de la zona anterior
        if (currentZoneIndex >= 0)
            DeactivateZoneEvent(currentZoneIndex);

        // Siempre limpiar el Battle Royale al cambiar de zona
        battleRoyaleZone?.ForceStop();

        currentZoneIndex = zoneIndex;

        if (zoneIndex >= 0 && zoneIndex < totalZones)
            ActivateZoneEvent(zoneIndex);
    }

    /// Llamado por SpaceMinigame.RegisterKill().
    public void NotifyKill()
    {
        battleRoyaleZone?.ResetTimer();
        mediumShipEvent?.NotifyKill();
    }

    // -------------------------------------------------------------------------
    //  Activacion / Desactivacion
    // -------------------------------------------------------------------------

    private void ActivateZoneEvent(int zoneIndex)
    {
        SpaceEventType eventType = zoneSortedEvent[zoneIndex];

        bool isFixed = fixedZoneIndices.Contains(zoneIndex);
        Debug.Log($"[SpaceEventManager] Zona {zoneIndex} -> Evento: {eventType} ({(isFixed ? "FIJO" : "RANDOM")})");

        switch (eventType)
        {
            case SpaceEventType.None:
                break;

            case SpaceEventType.BlackHole:
                ActivateBlackHole(zoneIndex);
                break;

            case SpaceEventType.DimensionalPortals:
                ActivateDimensionalPortals(zoneIndex);
                break;

            case SpaceEventType.PowerUpRain:
                ActivatePowerUpRain(zoneIndex);
                break;

            case SpaceEventType.AlienShips:
                ActivateAlienShips(zoneIndex);
                break;

            case SpaceEventType.MediumShip:
                ActivateMediumShip(zoneIndex);
                break;
        }
    }

    private void DeactivateZoneEvent(int zoneIndex)
    {
        SpaceEventType eventType = zoneSortedEvent[zoneIndex];

        switch (eventType)
        {
            case SpaceEventType.BlackHole:
                DeactivateBlackHole(zoneIndex);
                break;
            case SpaceEventType.DimensionalPortals:
                DeactivateDimensionalPortals(zoneIndex);
                break;
            case SpaceEventType.PowerUpRain:
                DeactivatePowerUpRain(zoneIndex);
                break;
            case SpaceEventType.AlienShips:
                DeactivateAlienShips(zoneIndex);
                break;
            case SpaceEventType.MediumShip:
                DeactivateMediumShip(zoneIndex);
                break;
        }
    }

    // -------------------------------------------------------------------------
    //  Stubs de eventos (implementar uno por uno)
    // -------------------------------------------------------------------------

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
        // TODO: SpaceAlienShipsEvent.Instance?.Activate(zone);
    }

    private void DeactivateAlienShips(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR AlienShips en zona {zone}");
        // TODO: SpaceAlienShipsEvent.Instance?.Deactivate();
    }

    private void ActivateMediumShip(int zone)
    {
        Debug.Log($"[SpaceEventManager] ACTIVAR MediumShip en zona {zone}");

        if (mediumShipEvent == null)
        {
            Debug.LogError("[SpaceEventManager] mediumShipEvent es NULL. Arrastra el GO de la escena (no el prefab) en el Inspector.");
            return;
        }

        mediumShipEvent.Activate();
    }

    private void DeactivateMediumShip(int zone)
    {
        Debug.Log($"[SpaceEventManager] DESACTIVAR MediumShip en zona {zone}");
        mediumShipEvent?.Deactivate();
    }

    // -------------------------------------------------------------------------
    //  Debug
    // -------------------------------------------------------------------------

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
    private void DebugResort()
    {
        SortEvents();
    }
}