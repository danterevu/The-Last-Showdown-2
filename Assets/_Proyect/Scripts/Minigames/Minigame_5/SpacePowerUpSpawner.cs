using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// SETUP:
///   - powerUpPickupPrefab: prefab con SpacePowerUpPickup.cs
///   - zones: puntos de spawn por zona (igual estructura que WeaponSpawner)
///   - spawnInterval: segundos entre spawns
///   - respawnDelay: segundos para volver a aparecer tras recogerse
public class SpacePowerUpSpawner : MonoBehaviour
{
    [Header("Prefab del pickup")]
    [SerializeField] private GameObject powerUpPickupPrefab;

    [Header("Tiempo")]
    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private float respawnDelay = 10f;

    [System.Serializable]
    public class ZoneSpawnPoints
    {
        public Transform[] points;
    }

    [Header("Puntos de spawn por zona")]
    [SerializeField] private ZoneSpawnPoints[] zones;

    private List<Transform> availablePoints = new List<Transform>();
    private int currentZoneIndex = -1;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    /// SpaceMinigame llama esto al cambiar de zona.
    public void SetActiveZone(int zoneIndex)
    {
        currentZoneIndex = zoneIndex;
        availablePoints.Clear();

        if (zones == null || zoneIndex < 0 || zoneIndex >= zones.Length) return;

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
        int pointIndex = Random.Range(0, availablePoints.Count);
        Transform point = availablePoints[pointIndex];
        availablePoints.RemoveAt(pointIndex);

        // Elegir tipo random
        SpacePowerUpType type = (SpacePowerUpType)Random.Range(0, System.Enum.GetValues(typeof(SpacePowerUpType)).Length);

        GameObject obj = Instantiate(powerUpPickupPrefab, point.position, Quaternion.identity);
        SpacePowerUpPickup pickup = obj.GetComponent<SpacePowerUpPickup>();
        pickup.Init(type, this, point);
    }

    public void OnPickupCollected(Transform point)
    {
        StartCoroutine(RespawnPoint(point, currentZoneIndex));
    }

    private IEnumerator RespawnPoint(Transform point, int zoneAtPickup)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (currentZoneIndex != zoneAtPickup) yield break;
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