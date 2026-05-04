using UnityEngine;

/// SETUP del prefab:
///   - SpriteRenderer con sprite de la pelota
///   - Rigidbody2D: Gravity=0, Collision Detection=Continuous
///   - CircleCollider2D con Is Trigger = true
///   - Este script
[RequireComponent(typeof(Rigidbody2D))]
public class SlowGrandeProjectile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float range = 12f;

    [Header("Explosion")]
    [SerializeField] private GameObject slowFieldPrefab;

    private float traveledDistance;
    private int ownerPlayer;

    public void Init(Vector2 direction, int ownerPlayer)
    {
        this.ownerPlayer = ownerPlayer;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
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
        // Ignorar al dueno
        if (other.CompareTag("Player1") && ownerPlayer == 1) return;
        if (other.CompareTag("Player2") && ownerPlayer == 2) return;

        // Ignorar objetos que no deben detonar el proyectil
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
            Instantiate(slowFieldPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
