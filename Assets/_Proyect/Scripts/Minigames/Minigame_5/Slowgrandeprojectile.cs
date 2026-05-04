using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlowGrandeProjectile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float range = 12f;

    [Header("Explosion")]
    [SerializeField] private GameObject slowFieldPrefab;
    [Tooltip("Cuánto más grande que el SlowField normal es la explosión")]
    [SerializeField] private float explosionScale = 3f;

    private float traveledDistance;
    private int ownerPlayer;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
    }

    public void Init(Vector2 direction, int ownerPlayer)
    {
        this.ownerPlayer = ownerPlayer;

        // Fallback: si direction es zero, lanzar hacia adelante
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;

        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        traveledDistance += speed * Time.deltaTime;
        if (traveledDistance >= range)
            Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1") && ownerPlayer == 1) return;
        if (other.CompareTag("Player2") && ownerPlayer == 2) return;
        if (other.GetComponent<SpaceZoneBoundary>() != null) return;
        if (other.GetComponent<SlowField>() != null) return;
        if (other.GetComponent<WeaponPickup>() != null) return;
        if (other.GetComponent<SpacePowerUpPickup>() != null) return;
        if (other.GetComponent<HomingMissile>() != null) return;

        Explode();
    }

    private void Explode()
    {
        if (slowFieldPrefab != null)
        {
            GameObject field = Instantiate(slowFieldPrefab, transform.position, Quaternion.identity);
            // Agrandar el radio de la explosion respecto al SlowField normal
            field.transform.localScale *= explosionScale;
        }

        Destroy(gameObject);
    }
}