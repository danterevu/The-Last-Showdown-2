using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;


/// SpaceEventManager
///
/// SETUP en el Inspector:
///   1. totalZones           cuántas zonas tiene el minijuego (ej: 5)
///   2. fixedZoneEvents      lista de eventos de zona fija.
///                            Para cada entrada elegís: zoneIndex + eventType.
///                            Esas zonas quedan excluidas del sorteo random.
///   3. randomEventPool      qué eventos random pueden sortear (arrastrá los tipos que querés).
///   4. randomChance         probabilidad de que aparezca un evento random (default 0.5 = 50%).
///
/// FLUJO:
///   - En Awake() se sortea todo (qué zonas tienen random y cuál evento).
///   - SpaceMinigame llama SetActiveZone(index) al cambiar de zona.
///   - El manager activa/desactiva los eventos correspondientes.
///   - SpaceMinigame llama NotifyKill() para que el Battle Royale resetee su contador.

public class SpaceEventManager : MonoBehaviour
{
    public static SpaceEventManager Instance { get; private set; }

    [Header("Configuración General")]
    [Tooltip("Cantidad total de zonas del minijuego")]
    [SerializeField] private int totalZones = 5;

    [Header("Eventos de Zona Fija")]
    [Tooltip("Definí qué evento fijo aparece en qué zona. Esas zonas no participan del sorteo random.")]
    [SerializeField] private FixedZoneEvent[] fixedZoneEvents;

    [Header("Pool de Eventos Random")]
    [Tooltip("Qué tipos de eventos pueden aparecer de forma aleatoria")]
    [SerializeField] private SpaceEventType[] randomEventPool;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que aparezca un evento random en una zona libre (0=nunca, 1=siempre)")]
    [SerializeField] private float randomChance = 0.5f;

    [Header("Battle Royale (Universal - todas las zonas)")]
    [Tooltip("El componente de la zona Battle Royale que se achica")]
    [SerializeField] private SpaceBattleRoyaleZone battleRoyaleZone;

    // Resultado del sorteo 
    // Para cada zona: qué evento le tocó (None = sin evento random)
    private SpaceEventType[] zoneSortedEvent;

    // Zonas que tienen evento fijo (para excluirlas del random)
    private HashSet<int> fixedZoneIndices = new HashSet<int>();

    // Zona activa actualmente
    private int currentZoneIndex = -1;

    // Clases serializables 
    [System.Serializable]
    public class FixedZoneEvent
    {
        [Tooltip("Índice de la zona (0 = primera zona)")]
        public int zoneIndex;
        [Tooltip("Qué evento fijo aparece en esta zona")]
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

    /// Sortea al inicio qué evento le toca a cada zona random.
    private void SortEvents()
    {
        zoneSortedEvent = new SpaceEventType[totalZones];

        // Registrar zonas fijas
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

        // Sortear zonas libres (las que no tienen evento fijo)
        for (int i = 0; i < totalZones; i++)
        {
            if (fixedZoneIndices.Contains(i)) continue;

            // Tirada de moneda: ¿aparece evento?
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

        // Activar evento de la nueva zona
        if (zoneIndex >= 0 && zoneIndex < totalZones)
            ActivateZoneEvent(zoneIndex);
    }

    /// Llamado por SpaceMinigame.RegisterKill() para resetear el contador del Battle Royale.
    public void NotifyKill()
    {
        battleRoyaleZone?.ResetTimer();
    }

    // Activación / Desactivación 

    private void ActivateZoneEvent(int zoneIndex)
    {
        SpaceEventType eventType = zoneSortedEvent[zoneIndex];

        bool isFixed = fixedZoneIndices.Contains(zoneIndex);
        Debug.Log($"[SpaceEventManager] Zona {zoneIndex} -> Evento: {eventType} ({(isFixed ? "FIJO" : "RANDOM")})");

        switch (eventType)
        {
            case SpaceEventType.None:
                // Sin evento: no hacer nada
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

    //  Stubs de eventos (implementar uno por uno) 
    // Cada evento va a tener sus propios referencias en el Inspector.
    // Por ahora son stubs: solo loggean. Reemplazá el contenido al implementar cada uno.

    private void ActivateBlackHole(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? ACTIVAR BlackHole en zona {zone}");
        // TODO: SpaceBlackHoleEvent.Instance?.Activate(zone);
    }

    private void DeactivateBlackHole(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? DESACTIVAR BlackHole en zona {zone}");
        // TODO: SpaceBlackHoleEvent.Instance?.Deactivate();
    }

    private void ActivateDimensionalPortals(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? ACTIVAR DimensionalPortals en zona {zone}");
        // TODO: SpacePortalEvent.Instance?.Activate(zone);
    }

    private void DeactivateDimensionalPortals(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? DESACTIVAR DimensionalPortals en zona {zone}");
        // TODO: SpacePortalEvent.Instance?.Deactivate();
    }

    private void ActivatePowerUpRain(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? ACTIVAR PowerUpRain en zona {zone}");
        // TODO: SpacePowerUpRainEvent.Instance?.Activate(zone);
    }

    private void DeactivatePowerUpRain(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? DESACTIVAR PowerUpRain en zona {zone}");
        // TODO: SpacePowerUpRainEvent.Instance?.Deactivate();
    }

    private void ActivateAlienShips(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? ACTIVAR AlienShips en zona {zone}");
        // TODO: SpaceAlienShipsEvent.Instance?.Activate(zone);
    }

    private void DeactivateAlienShips(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? DESACTIVAR AlienShips en zona {zone}");
        // TODO: SpaceAlienShipsEvent.Instance?.Deactivate();
    }

    private void ActivateMediumShip(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? ACTIVAR MediumShip en zona {zone}");
        // TODO: SpaceMediumShipEvent.Instance?.Activate(zone);
    }

    private void DeactivateMediumShip(int zone)
    {
        Debug.Log($"[SpaceEventManager] ? DESACTIVAR MediumShip en zona {zone}");
        // TODO: SpaceMediumShipEvent.Instance?.Deactivate();
    }

    // Debug 

    private void LogSortResult()
    {
        
        Debug.Log("       SORTEO DE EVENTOS - RESULTADO        ");
        

        for (int i = 0; i < totalZones; i++)
        {
            bool isFixed = fixedZoneIndices.Contains(i);
            string tag = isFixed ? "[FIJO]   " : "[RANDOM] ";
            string eventName = zoneSortedEvent[i] == SpaceEventType.None ? "Sin evento" : zoneSortedEvent[i].ToString();
            Debug.Log($"  Zona {i}: {tag}{eventName,-20}  ");
        }

        
    }

    /// Para debug en el Inspector (botón en el contexto del componente).
    [ContextMenu("Re-sortear Eventos (Debug)")]
    private void DebugResort()
    {
        SortEvents();
    }
}