using UnityEngine;

public class BreakableAsteroid : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int health = 3;

    [Header("Solo Laser puede romper?")]
    [SerializeField] private bool onlyLaserCanBreak = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject interactiveAsteroidPrefab;
    [SerializeField] private GameObject[] debrisPrefabs;

    [Header("Fuerzas")]
    [SerializeField] private float debrisLaunchForce = 8f;
    [SerializeField] private float laserForceMultiplier = 15f;

    public void TakeDamage(int amount, bool isLaser, Vector2 hitDirection)
    {
        if (onlyLaserCanBreak && !isLaser)
        {
            return;
        }

        health -= amount;

        if (isLaser || health <= 0)
        {
            if (isLaser && interactiveAsteroidPrefab != null)
            {
                SpawnInteractiveAsteroid(hitDirection);
            }
            else if (health <= 0 && debrisPrefabs != null && debrisPrefabs.Length > 0)
            {
                SpawnDebris();
            }

            Destroy(gameObject);
        }
    }

    private void SpawnInteractiveAsteroid(Vector2 hitDirection)
    {
        GameObject asteroid = Instantiate(interactiveAsteroidPrefab, transform.position, transform.rotation);
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(hitDirection * laserForceMultiplier, ForceMode2D.Impulse);
        }
    }

    private void SpawnDebris()
    {
        int count = Random.Range(2, debrisPrefabs.Length + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject debris = Instantiate(debrisPrefabs[Random.Range(0, debrisPrefabs.Length)], transform.position, Quaternion.identity);
            Rigidbody2D rb = debris.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                rb.AddForce(randomDir * debrisLaunchForce, ForceMode2D.Impulse);
            }
        }
    }
}
