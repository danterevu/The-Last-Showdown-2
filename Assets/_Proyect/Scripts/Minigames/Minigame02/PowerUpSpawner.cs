using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float respawnDelay = 5f;

    [System.Serializable]
    public class ZoneSpawnPoints
    {
        public Transform[] points;
    }

    [SerializeField] private ZoneSpawnPoints[] zones;
    [SerializeField] private GameObject powerUpPrefab;

    private List<Transform> availablePoints = new List<Transform>();
    private int currentZoneIndex = 0;

    private void Start()
    {
        // No iniciar SpawnLoop aqui - KingOfHill llama SetActiveZone antes de que
        // pasen los primeros 10 segundos, pero igual esperamos a que este seteado
        StartCoroutine(SpawnLoop());
    }

    // KingOfHill llama esto al cambiar de zona
    public void SetActiveZone(int zoneIndex)
    {
        currentZoneIndex = zoneIndex;
        availablePoints.Clear();
        if (zones == null || zoneIndex >= zones.Length) return;
        foreach (Transform point in zones[zoneIndex].points)
            availablePoints.Add(point);
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
        PowerUpPickup pickup = obj.GetComponent<PowerUpPickup>();
        pickup.Initialize(this, point);
    }

    public void OnPickupCollected(Transform point)
    {
        StartCoroutine(RespawnPoint(point, currentZoneIndex));
    }

    private IEnumerator RespawnPoint(Transform point, int zoneAtPickup)
    {
        yield return new WaitForSeconds(respawnDelay);

        
        // evita agregar puntos de zonas viejas a la zona activa
        if (currentZoneIndex != zoneAtPickup) yield break;

        // verificar que el punto pertenezca a la zona activa
        if (zones == null || currentZoneIndex >= zones.Length) yield break;
        foreach (Transform p in zones[currentZoneIndex].points)
        {
            if (p == point)
            {
                availablePoints.Add(point);
                break;
            }
        }
    }
}
