using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DebrisPiece : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 2f;

    [Header("Daño")]
    [SerializeField] private bool canKill = true;
    [SerializeField] private float lethalSpeed = 8f;

    private SpriteRenderer sr;
    private float timer;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = lifetime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // fade out en el ultimo segundo
        float alpha = Mathf.Clamp01(timer);
        if (sr != null)
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);

        // Actualizar estado de canKill según velocidad
        if (rb != null && rb.linearVelocity.magnitude < lethalSpeed)
        {
            canKill = false;
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Colisión con borde
        if (collision.gameObject.CompareTag("Border"))
        {
            if (rb != null && rb.linearVelocity.magnitude >= lethalSpeed)
            {
                Destroy(gameObject);
            }
            return;
        }

        // Colisión con jugador
        if (!collision.gameObject.CompareTag("Player1") && !collision.gameObject.CompareTag("Player2"))
            return;

        if (!canKill) return;
        if (rb == null || rb.linearVelocity.magnitude < lethalSpeed) return;

        int hitPlayer = collision.gameObject.CompareTag("Player1") ? 1 : 2;
        SpaceMinigame.Instance?.RegisterKill(0, hitPlayer);
    }

    // llamado desde Explodable para sincronizar el lifetime
    public void SetLifetime(float value)
    {
        lifetime = value;
        timer = value;
    }
}