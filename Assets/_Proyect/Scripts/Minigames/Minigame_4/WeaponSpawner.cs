using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Armas disponibles en el pool")]
    [Tooltip("Todas las armas que pueden aparecer. Se elige una al azar por spawn.")]
    [SerializeField] private WeaponData[] weaponPool;

    [Header("Prefab base del pickup")]
    [SerializeField] private GameObject weaponPickupPrefab;

    [Header("Configuración de tiempo")]
    [SerializeField] private float spawnInterval = 5f;  // segundos entre spawns
    [SerializeField] private float respawnDelay = 8f;   // segundos para volver a aparecer tras recogerse

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

 

    /// SpaceMinigame llama esto al activar una zona nueva.
    /// Resetea los puntos disponibles a los de la nueva zona.
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

            if (availablePoints.Count > 0 && weaponPool.Length > 0)
                SpawnWeapon();
        }
    }

    private void SpawnWeapon()
    {
        // Elegir punto de spawn al azar
        int pointIndex = Random.Range(0, availablePoints.Count);
        Transform point = availablePoints[pointIndex];
        availablePoints.RemoveAt(pointIndex);

        // Elegir arma al azar del pool
        WeaponData chosenWeapon = weaponPool[Random.Range(0, weaponPool.Length)];

        // Instanciar el pickup
        GameObject obj = Instantiate(weaponPickupPrefab, point.position, Quaternion.identity);
        WeaponPickup pickup = obj.GetComponent<WeaponPickup>();
        pickup.Init(chosenWeapon, this, point);
    }

  

    /// <summary>Llamado por WeaponPickup al ser recogido.</summary>
    public void OnPickupCollected(Transform point)
    {
        StartCoroutine(RespawnPoint(point, currentZoneIndex));
    }

    private IEnumerator RespawnPoint(Transform point, int zoneAtPickup)
    {
        yield return new WaitForSeconds(respawnDelay);

        // Ignorar si ya cambiamos de zona
        if (currentZoneIndex != zoneAtPickup) yield break;
        if (zones == null || currentZoneIndex >= zones.Length) yield break;

        // Verificar que el punto pertenezca a la zona activa antes de devolverlo
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