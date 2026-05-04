using UnityEngine;

/// Script para el misil teledirigido del power up HomingMissile.
/// SETUP del prefab:
///   - SpriteRenderer con sprite del misil
///   - Rigidbody2D: Gravity=0, Collision Detection=Continuous
///   - CircleCollider2D con Is Trigger = true
///   - Este script
[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 120f;   // grados por segundo
    [SerializeField] private float range = 30f;        // distancia maxima antes de destruirse

    private Transform target;
    private int ownerPlayer;
    private float traveledDistance;
    private Rigidbody2D rb;

    public void Init(Transform target, int ownerPlayer)
    {
        this.target = target;
        this.ownerPlayer = ownerPlayer;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        traveledDistance += speed * Time.fixedDeltaTime;
        if (traveledDistance >= range)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null)
        {
            // El target fue destruido; seguir recto
            rb.linearVelocity = transform.right * speed;
            return;
        }

        // Calcular angulo hacia el objetivo
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.z;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

        rb.linearVelocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int hitPlayer = 0;
        if (other.CompareTag("Player1")) hitPlayer = 1;
        else if (other.CompareTag("Player2")) hitPlayer = 2;
        else
        {
            // Ignorar pickups y campos
            if (other.GetComponent<WeaponPickup>() != null) return;
            if (other.GetComponent<SpacePowerUpPickup>() != null) return;
            if (other.GetComponent<SlowField>() != null) return;
            Destroy(gameObject);
            return;
        }

        if (hitPlayer == ownerPlayer) return;

        GameManager.Instance?.RemovePoints(hitPlayer, 5);
        GameManager.Instance?.AddPoints(ownerPlayer, 5);
        SpaceMinigame.Instance?.RegisterKill(ownerPlayer, hitPlayer);
        CameraShake.Instance?.Shake(0.15f, 0.1f);

        Destroy(gameObject);
    }
}
