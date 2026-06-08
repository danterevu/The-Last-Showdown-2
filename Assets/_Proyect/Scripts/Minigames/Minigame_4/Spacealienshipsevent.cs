using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceAlienShipsEvent : MonoBehaviour
{
    public static SpaceAlienShipsEvent Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject motherShipPrefab;
    [SerializeField] private GameObject alienPrefab;

    [Header("Puntos de la nave nodriza")]
    [SerializeField] private Transform spawnOrigin;
    [SerializeField] private Transform landingPoint;

    [Header("Aliens")]
    [SerializeField] private int alienCount = 4;
    [SerializeField] private float alienSpawnInterval = 0.6f;
    [SerializeField] private int pointsPerAlien = 1;
    [SerializeField] private float alienSlowDuration = 2f;
    [SerializeField] private float alienSlowMultiplier = 0.4f;

    [Header("Delay antes de que entre la nave tras la kill")]
    [SerializeField] private float delayBeforeEntry = 1.5f;

    private bool isActive = false;
    private bool eventAlreadyFired = false;
    private Coroutine eventCoroutine;

    private SpaceMotherShip currentMotherShip;
    private List<SpaceAlien> activeAliens = new List<SpaceAlien>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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
        Debug.Log("[SpaceAlienShipsEvent] Activado. Esperando kill.");
    }

    public void Deactivate()
    {
        if (!isActive) return;
        isActive = false;

        if (eventCoroutine != null) { StopCoroutine(eventCoroutine); eventCoroutine = null; }

        CleanUp();
        Debug.Log("[SpaceAlienShipsEvent] Desactivado. Todo limpiado.");
    }

    public void NotifyKill()
    {
        if (!isActive || eventAlreadyFired) return;
        eventAlreadyFired = true;
        Debug.Log("[SpaceAlienShipsEvent] Kill detectada. Disparando evento.");

        if (eventCoroutine != null) StopCoroutine(eventCoroutine);
        eventCoroutine = StartCoroutine(RunEvent());
    }

    /// Alien muerto por un jugador: dar puntos.
    public void OnAlienDied(SpaceAlien alien, int killerPlayer)
    {
        activeAliens.Remove(alien);
        Debug.Log($"[SpaceAlienShipsEvent] Alien muerto por jugador {killerPlayer}. Quedan {activeAliens.Count}.");
        GameManager.Instance?.AddPoints(killerPlayer, pointsPerAlien);
    }

    /// Alien muerto sin responsable (zona cambió, limpieza): no dar puntos.
    public void OnAlienDiedNoPoints(SpaceAlien alien)
    {
        activeAliens.Remove(alien);
    }

    public float GetSlowDuration() => alienSlowDuration;
    public float GetSlowMultiplier() => alienSlowMultiplier;

    // -------------------------------------------------------------------------
    //  FLUJO
    // -------------------------------------------------------------------------

    private IEnumerator RunEvent()
    {
        yield return new WaitForSeconds(delayBeforeEntry);
        if (!isActive) yield break;

        if (motherShipPrefab == null || spawnOrigin == null || landingPoint == null)
        {
            Debug.LogError("[SpaceAlienShipsEvent] Faltan referencias.");
            yield break;
        }

        GameObject msGO = Instantiate(motherShipPrefab, spawnOrigin.position, Quaternion.identity);
        currentMotherShip = msGO.GetComponent<SpaceMotherShip>();

        if (currentMotherShip == null)
        {
            Debug.LogError("[SpaceAlienShipsEvent] El prefab no tiene SpaceMotherShip.cs");
            Destroy(msGO);
            yield break;
        }

        currentMotherShip.FlyTo(landingPoint.position);

        while (currentMotherShip != null && !currentMotherShip.HasLanded)
            yield return null;

        if (!isActive) yield break;

        yield return StartCoroutine(SpawnAliens());

        if (currentMotherShip != null)
            currentMotherShip.Depart();
    }

    private IEnumerator SpawnAliens()
    {
        if (alienPrefab == null) { Debug.LogError("[SpaceAlienShipsEvent] alienPrefab es NULL."); yield break; }

        for (int i = 0; i < alienCount; i++)
        {
            if (!isActive) yield break;

            GameObject alienGO = Instantiate(alienPrefab, landingPoint.position, Quaternion.identity);
            SpaceAlien alien = alienGO.GetComponent<SpaceAlien>();

            if (alien == null) { Debug.LogError("[SpaceAlienShipsEvent] Prefab sin SpaceAlien.cs"); Destroy(alienGO); }
            else { alien.Init(this); activeAliens.Add(alien); }

            yield return new WaitForSeconds(alienSpawnInterval);
        }
    }

    private void CleanUp()
    {
        if (currentMotherShip != null) { Destroy(currentMotherShip.gameObject); currentMotherShip = null; }

        foreach (var alien in activeAliens)
            if (alien != null) Destroy(alien.gameObject);

        activeAliens.Clear();
    }

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