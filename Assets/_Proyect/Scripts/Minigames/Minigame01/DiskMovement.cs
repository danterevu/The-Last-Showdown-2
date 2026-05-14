using System.Collections;
using UnityEngine;

public class DiskMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float initialSpeed = 4f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float acceleration = 0.1f;


    [Header("Squash & Stretch")]
    [SerializeField] private float squashAmount = 0.4f;
    [SerializeField] private float stretchAmount = 0.3f;
    [SerializeField] private float squashDuration = 0.08f;
    [SerializeField] private float recoverDuration = 0.12f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    private Vector3 originalScale;
    private Coroutine squashCoroutine;
    [Header("Debug")]
    [SerializeField] private float currentSpeed;


    private Rigidbody2D rb;
    private Vector2 direction;
    private DodgeDisk dodgeDisk;
    private bool moving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dodgeDisk = FindFirstObjectByType<DodgeDisk>();

        originalScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        if (!moving) return;

        // aumento constante de velocidad
        currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
        rb.linearVelocity = direction * currentSpeed;
    }

    public void Launch()
    {
        direction = Random.insideUnitCircle.normalized;

        while (Mathf.Abs(direction.x) < 0.2f || Mathf.Abs(direction.y) < 0.2f) //se asegura que no se lance de forma horizontal, asi saliendo de forma inclinada
        {
            direction = Random.insideUnitCircle.normalized;
        }

        currentSpeed = initialSpeed;
        moving = true;
        rb.linearVelocity = direction * currentSpeed;
    }
    private IEnumerator SquashOnImpact(Vector2 impactNormal)
    {
        bool hitHorizontalWall = Mathf.Abs(impactNormal.x) > Mathf.Abs(impactNormal.y);

      
            
        Vector3 squashedScale = hitHorizontalWall
            ? new Vector3(originalScale.x * (1 - squashAmount), originalScale.y * (1f + stretchAmount), originalScale.z)
            : new Vector3(originalScale.x * (1f + stretchAmount), originalScale.y * (1f - squashAmount), originalScale.z);

        float elapsed = 0f;
        while (elapsed<squashDuration)
        {
            transform.localScale = Vector3.Lerp(squashedScale, originalScale, elapsed / recoverDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale=originalScale;
        squashCoroutine = null;

    }    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.contacts[0].normal;
        direction = Vector2.Reflect(direction, normal).normalized; //rebota y se normaliza

        AudioManager.Instance?.PlaySFX(SoundID.BounceDisk);

        // TRUCO: Si la dirección en algún eje es casi 0, le damos un empujoncito
        // para evitar que se quede pegado o rebote perfectamente recto.
        if (Mathf.Abs(direction.x) < 0.1f) direction.x += (direction.x > 0 ? 0.1f : -0.1f);
        if (Mathf.Abs(direction.y) < 0.1f) direction.y += (direction.y > 0 ? 0.1f : -0.1f);

        direction = direction.normalized;
        rb.linearVelocity = direction * currentSpeed;
        if (squashCoroutine != null) StopCoroutine(squashCoroutine);
        squashCoroutine = StartCoroutine(SquashOnImpact(normal));

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
            dodgeDisk.TryHitPlayer(1);
        else if (other.CompareTag("Player2"))
            dodgeDisk.TryHitPlayer(2);
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        rb.linearVelocity = direction * currentSpeed;
    }

    public void Stop()
    {
        moving = false;
        rb.linearVelocity = Vector2.zero;
        direction = Vector2.zero;

        transform.localScale = originalScale;
    }
}