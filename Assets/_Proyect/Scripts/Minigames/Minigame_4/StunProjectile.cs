using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StunProjectile : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float range = 12f;
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float knockbackForce = 10f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 direction;
    private bool hitPlayer1 = false;
    private bool hitPlayer2 = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
    }

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        startPosition = transform.position;
        rb.linearVelocity = direction * speed;
    }

    private void Update()
    {
        // Chequear rango máximo
        if (Vector2.Distance(transform.position, startPosition) >= range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isP1 = other.CompareTag("Player1");
        bool isP2 = other.CompareTag("Player2");

        if (!isP1 && !isP2) return;
        if (isP1 && hitPlayer1) return;
        if (isP2 && hitPlayer2) return;

        if (isP1) hitPlayer1 = true;
        if (isP2) hitPlayer2 = true;

        SpaceShipController ship = other.GetComponentInParent<SpaceShipController>();
        if (ship == null) return;

        ship.ApplyStun(stunDuration);
        ship.AddImpulse(direction * knockbackForce);

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Si choca con algo que no es un jugador, se destruye
        bool isP1 = collision.gameObject.CompareTag("Player1");
        bool isP2 = collision.gameObject.CompareTag("Player2");

        if (!isP1 && !isP2)
        {
            Destroy(gameObject);
        }
    }
}
