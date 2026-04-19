using UnityEngine;

public class PowerUpMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Spawn Bounds")]
    [SerializeField] private float boundsX = 4f;
    [SerializeField] private float boundsY = 2.5f;

    private Rigidbody2D rb;
    private Vector2 direction;
    private PowerUpManager powerUpManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        powerUpManager = FindFirstObjectByType<PowerUpManager>();
    }

    private void Start()
    {
        Launch();
    }

    //MOVEMENT 
    private void Launch()
    {
        direction = Random.insideUnitCircle.normalized;

        while (Mathf.Abs(direction.x) < 0.2f || Mathf.Abs(direction.y) < 0.2f)
        {
            direction = Random.insideUnitCircle.normalized;
        }

        rb.linearVelocity = direction * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.contacts[0].normal;
        direction = Vector2.Reflect(direction, normal).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    // PICKUP DETECTION 
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
            powerUpManager.OnPlayerPickup(1);
        else if (other.CompareTag("Player2"))
            powerUpManager.OnPlayerPickup(2);
    }

    // REPOSITION 
    public void Reposition()
    {
        float x = Random.Range(-boundsX, boundsX);
        float y = Random.Range(-boundsY, boundsY);
        transform.position = new Vector2(x, y);
        gameObject.SetActive(true);
        Launch();
    }
}