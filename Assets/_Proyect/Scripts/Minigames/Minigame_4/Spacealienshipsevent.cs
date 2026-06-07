using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// SpaceAlienShipsEvent
///
/// SETUP en la escena:
///   1. GO activo en la escena con este script (nunca desactivarlo).
///   2. motherShipPrefab: prefab con SpaceMotherShip.cs
///   3. alienPrefab: prefab con SpaceAlien.cs
///   4. landingPoint: GO vacio DENTRO de la zona donde aterra la nave nodriza.
///   5. spawnOrigin: GO vacio fuera de la zona desde donde entra la nave nodriza.
///   6. En SpaceEventManager: arrastra este GO al campo alienShipsEvent.
///
/// FLUJO:
///   - Activate() se llama al entrar a la zona.
///   - NotifyKill() dispara el evento (una sola vez por activacion).
///   - Deactivate() limpia todo al salir de la zona o al pasar de ronda.

public class SpaceAlienShipsEvent : MonoBehaviour
{
    public static SpaceAlienShipsEvent Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("Prefab de la nave nodriza. Debe tener SpaceMotherShip.cs")]
    [SerializeField] private GameObject motherShipPrefab;
    [Tooltip("Prefab del alien gusano. Debe tener SpaceAlien.cs")]
    [SerializeField] private GameObject alienPrefab;

    [Header("Puntos de la nave nodriza")]
    [Tooltip("GO vacio FUERA de la zona. Desde aqui entra la nave nodriza.")]
    [SerializeField] private Transform spawnOrigin;
    [Tooltip("GO vacio DENTRO de la zona. Aqui aterra la nave nodriza y spawnea aliens.")]
    [SerializeField] private Transform landingPoint;

    [Header("Aliens")]
    [Tooltip("Cuantos aliens spawnea la nave nodriza")]
    [SerializeField] private int alienCount = 4;
    [Tooltip("Segundos entre cada alien spawneado")]
    [SerializeField] private float alienSpawnInterval = 0.6f;
    [Tooltip("Puntos que da matar un alien")]
    [SerializeField] private int pointsPerAlien = 1;
    [Tooltip("Segundos de slow que aplica el alien al colisionar con un jugador")]
    [SerializeField] private float alienSlowDuration = 2f;
    [Tooltip("Cuanto reduce la velocidad el slow (0.5 = mitad de velocidad)")]
    [SerializeField] private float alienSlowMultiplier = 0.4f;

    [Header("Delay inicial antes de arrancar el evento tras la kill")]
    [Tooltip("Segundos que espera antes de que entre la nave nodriza")]
    [SerializeField] private float delayBeforeEntry = 1.5f;

    private bool isActive = false;
    private bool eventAlreadyFired = false;
    private Coroutine eventCoroutine;

    private SpaceMotherShip currentMotherShip;
    private List<SpaceAlien> activeAliens = new List<SpaceAlien>();

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

    // -------------------------------------------------------------------------
    //  API PUBLICA
    // -------------------------------------------------------------------------

    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        eventAlreadyFired = false;
        Debug.Log("[SpaceAlienShipsEvent] Activado. Esperando kill para disparar el evento.");
    }

    public void Deactivate()
    {
        if (!isActive) return;

        isActive = false;

        if (eventCoroutine != null)
        {
            StopCoroutine(eventCoroutine);
            eventCoroutine = null;
        }

        CleanUp();
        Debug.Log("[SpaceAlienShipsEvent] Desactivado. Todo limpiado.");
    }

    /// Llamado por SpaceEventManager cuando hay una kill en esta zona.
    public void NotifyKill()
    {
        if (!isActive) return;
        if (eventAlreadyFired) return;

        eventAlreadyFired = true;
        Debug.Log("[SpaceAlienShipsEvent] Kill detectada. Disparando evento.");

        if (eventCoroutine != null)
            StopCoroutine(eventCoroutine);

        eventCoroutine = StartCoroutine(RunEvent());
    }

    /// Llamado por SpaceAlien cuando muere.
    public void OnAlienDied(SpaceAlien alien, int killerPlayer)
    {
        activeAliens.Remove(alien);
        Debug.Log($"[SpaceAlienShipsEvent] Alien muerto. Quedan {activeAliens.Count}. Puntos para jugador {killerPlayer}.");
        GameManager.Instance?.AddPoints(killerPlayer, pointsPerAlien);
    }

    /// Llamado por SpaceAlien cuando colisiona con un jugador.
    public float GetSlowDuration() => alienSlowDuration;
    public float GetSlowMultiplier() => alienSlowMultiplier;

    // -------------------------------------------------------------------------
    //  FLUJO PRINCIPAL
    // -------------------------------------------------------------------------

    private IEnumerator RunEvent()
    {
        yield return new WaitForSeconds(delayBeforeEntry);

        if (!isActive) yield break;

        // Spawnear nave nodriza
        if (motherShipPrefab == null || spawnOrigin == null || landingPoint == null)
        {
            Debug.LogError("[SpaceAlienShipsEvent] Faltan referencias (motherShipPrefab, spawnOrigin o landingPoint).");
            yield break;
        }

        GameObject msGO = Instantiate(motherShipPrefab, spawnOrigin.position, Quaternion.identity);
        currentMotherShip = msGO.GetComponent<SpaceMotherShip>();

        if (currentMotherShip == null)
        {
            Debug.LogError("[SpaceAlienShipsEvent] El prefab de la nave nodriza no tiene SpaceMotherShip.cs");
            Destroy(msGO);
            yield break;
        }

        // Decirle a la nave que vuele al landing point
        currentMotherShip.FlyTo(landingPoint.position);

        // Esperar a que llegue
        while (currentMotherShip != null && !currentMotherShip.HasLanded)
            yield return null;

        if (!isActive) yield break;

        // Spawnear aliens de a uno
        yield return StartCoroutine(SpawnAliens());

        // Esperar a que la nave nodriza se vaya (se auto-destruye tras spawnear)
        if (currentMotherShip != null)
            currentMotherShip.Depart();

        Debug.Log("[SpaceAlienShipsEvent] Nave nodriza partiendo. Aliens en juego: " + activeAliens.Count);
    }

    private IEnumerator SpawnAliens()
    {
        if (alienPrefab == null)
        {
            Debug.LogError("[SpaceAlienShipsEvent] alienPrefab es NULL.");
            yield break;
        }

        for (int i = 0; i < alienCount; i++)
        {
            if (!isActive) yield break;

            Vector3 spawnPos = landingPoint.position;

            GameObject alienGO = Instantiate(alienPrefab, spawnPos, Quaternion.identity);
            SpaceAlien alien = alienGO.GetComponent<SpaceAlien>();

            if (alien == null)
            {
                Debug.LogError("[SpaceAlienShipsEvent] El prefab del alien no tiene SpaceAlien.cs");
                Destroy(alienGO);
            }
            else
            {
                alien.Init(this);
                activeAliens.Add(alien);
                Debug.Log($"[SpaceAlienShipsEvent] Alien {i + 1}/{alienCount} spawneado.");
            }

            yield return new WaitForSeconds(alienSpawnInterval);
        }
    }

    private void CleanUp()
    {
        // Destruir nave nodriza si sigue activa
        if (currentMotherShip != null)
        {
            Destroy(currentMotherShip.gameObject);
            currentMotherShip = null;
        }

        // Destruir todos los aliens vivos
        foreach (var alien in activeAliens)
        {
            if (alien != null)
                Destroy(alien.gameObject);
        }
        activeAliens.Clear();
    }

    // -------------------------------------------------------------------------
    //  GIZMOS
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnOrigin != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
            Gizmos.DrawWireSphere(spawnOrigin.position, 0.6f);
            UnityEditor.Handles.Label(spawnOrigin.position + Vector3.up * 0.8f, "Nave: ORIGEN");
        }

        if (landingPoint != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.9f);
            Gizmos.DrawWireSphere(landingPoint.position, 0.6f);
            UnityEditor.Handles.Label(landingPoint.position + Vector3.up * 0.8f, "Nave: LANDING");
        }

        if (spawnOrigin != null && landingPoint != null)
        {
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            Gizmos.DrawLine(spawnOrigin.position, landingPoint.position);
        }
    }
#endif
}