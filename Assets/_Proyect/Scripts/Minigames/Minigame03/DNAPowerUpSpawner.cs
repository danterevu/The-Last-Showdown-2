using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class ZoneSpawnPoints
{
    public Transform[] points;
}

public class DNAPowerUpSpawner : MonoBehaviour
{

    [Header("Spawn por zonas")]
    [SerializeField] private ZoneSpawnPoints[] zones;
    [SerializeField] private GameObject powerUpPrefab;

    [Header("Tiempos")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float respawnDelay = 5f;

    private List<Transform> availablePoints = new List<Transform>();
    private List<GameObject> activePowerUps = new List<GameObject>(); // para limpiar al cambiar zona
    private int currentZone = -1;
    private Coroutine spawnLoopCoroutine;

    private void Start()
    {

    }

    // Llamado desde MutantDNAManager cuando se activa una nueva zona
    public void SetActiveZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zones.Length) return;
        ClearAllPowerUps();
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);

        currentZone = zoneIndex;
        availablePoints.Clear();
        foreach (Transform point in zones[zoneIndex].points)
            if (point != null) availablePoints.Add(point);

        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (availablePoints.Count > 0)
                SpawnPowerUp();
        }
    }

    private void SpawnPowerUp()
    {
        int index = Random.Range(0, availablePoints.Count);
        Transform point = availablePoints[index];
        availablePoints.RemoveAt(index);

        GameObject obj = Instantiate(powerUpPrefab, point.position, Quaternion.identity);
        DNAPowerUpPickup pickup = obj.GetComponent<DNAPowerUpPickup>();
        pickup.Initialize(this, point);
        activePowerUps.Add(obj);
    }

    public void OnPickupCollected(Transform point)
    {
        StartCoroutine(RespawnPoint(point));
    }

    private IEnumerator RespawnPoint(Transform point)
    {
        yield return new WaitForSeconds(respawnDelay);
        // Verificar que el punto pertenece a la zona actual
        if (currentZone >= 0 && System.Array.IndexOf(zones[currentZone].points, point) != -1)
            availablePoints.Add(point);
    }

    private void ClearAllPowerUps()
    {
        foreach (GameObject obj in activePowerUps)
        {
            if (obj != null)
                Destroy(obj);
        }
        activePowerUps.Clear();
    }
}