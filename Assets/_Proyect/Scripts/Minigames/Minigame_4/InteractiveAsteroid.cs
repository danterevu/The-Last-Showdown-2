using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InteractiveAsteroid : MonoBehaviour
{
    [Header("Velocidad necesaria para matar")]
    [SerializeField] private float lethalSpeed = 10f;

    [Header("Bordes")]
    [SerializeField] private float bounceBreakSpeed = 15f;
    [SerializeField] private GameObject[] debrisPrefabs;
    [SerializeField] private float debrisLaunchForce = 8f;

    [Header("Debug")]
    [SerializeField] private float currentSpeed;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        currentSpeed = rb.linearVelocity.magnitude;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Colisión con borde
        if (collision.gameObject.CompareTag("Border"))
        {
            if (currentSpeed >= bounceBreakSpeed)
            {
                SpawnDebris(collision.contacts[0].normal);
                Destroy(gameObject);
            }
            else
            {
                if (rb != null)
                {
                    rb.linearVelocity *= 0.7f;
                }
            }
            return;
        }

        // Solo jugadores
        if (!collision.gameObject.CompareTag("Player1") &&
            !collision.gameObject.CompareTag("Player2"))
            return;

        // Si no tiene suficiente velocidad no mata
        if (currentSpeed < lethalSpeed)
            return;

        int hitPlayer =
            collision.gameObject.CompareTag("Player1") ? 1 : 2;

        Debug.Log("ASTEROID KILL");

        // Mata al jugador
        SpaceMinigame.Instance?.RegisterKill(0, hitPlayer);
    }

    private void SpawnDebris(Vector2 collisionNormal)
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;

        int count = Random.Range(2, debrisPrefabs.Length + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject debris = Instantiate(debrisPrefabs[Random.Range(0, debrisPrefabs.Length)], transform.position, Quaternion.identity);
            Rigidbody2D debrisRb = debris.GetComponent<Rigidbody2D>();
            if (debrisRb != null)
            {
                Vector2 randomDir = (collisionNormal + Random.insideUnitCircle).normalized;
                debrisRb.AddForce(randomDir * debrisLaunchForce, ForceMode2D.Impulse);
            }
        }
    }
}