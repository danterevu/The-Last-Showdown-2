using UnityEngine;

public class SlimeProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private LayerMask wallLayer;

    private int ownerPlayer;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    public void Init(Vector2 direction, int owner)
    {
        ownerPlayer = owner;
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Destruirse con paredes y piso
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        // Impactar jugador rival
        int hitPlayer = other.CompareTag("Player1") ? 1 : other.CompareTag("Player2") ? 2 : 0;
        if (hitPlayer == 0 || hitPlayer == ownerPlayer) return;

        PlayerControllerDNA target = other.GetComponent<PlayerControllerDNA>();
        if (target == null) return;

        target.ApplySlimeEffect();
        Destroy(gameObject);
    }
}