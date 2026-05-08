using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InteractiveAsteroid : MonoBehaviour
{
    [Header("Velocidad necesaria para matar")]
    [SerializeField] private float lethalSpeed = 10f;

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
}