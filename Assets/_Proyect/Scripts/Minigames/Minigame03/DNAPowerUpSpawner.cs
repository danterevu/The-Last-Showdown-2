using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DNAPowerUpSpawner : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject powerUpPrefab;

    private List<Transform> availablePoints = new List<Transform>();

    private void Start()
    {
        foreach (Transform point in spawnPoints)
            availablePoints.Add(point);

        StartCoroutine(SpawnLoop());
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
    }

    public void OnPickupCollected(Transform point)
    {
        StartCoroutine(RespawnPoint(point));
    }

    private IEnumerator RespawnPoint(Transform point)
    {
        yield return new WaitForSeconds(respawnDelay);
        availablePoints.Add(point);
    }
}
