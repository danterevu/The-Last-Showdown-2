using UnityEngine;

public class Crate : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float throwBaseForce = 5f;
    [SerializeField] private float throwRunningForce = 15f;
    [SerializeField] private float throwUpward = 3f;
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private float minSpeedToStun = 5f;
    [SerializeField] private float stunCooldown = 1f;
    private PlayerControllerDNA lastThrower;   // quien lanzó la caja
    private float ignoreThrowerUntil = 0f;     // tiempo hasta ignorar al lanzador

    private Rigidbody2D rb;
    private Collider2D physicsCollider;
    private SpriteRenderer sr;
    private bool isHeld = false;
    private PlayerControllerDNA holder;
    private bool wasThrown = false;
    private float throwTime = 0f;
    private float lastStunTime = -100f;
    private Vector3 originalScale; // Escala original de la caja

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        physicsCollider = GetComponent<Collider2D>(); // Asume que solo hay un collider (no trigger)
        originalScale = transform.localScale;

        SetPhysicsEnabled(true);
        sr.enabled = true;
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (physicsCollider != null) physicsCollider.enabled = enabled;
        if (enabled)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void LateUpdate()
    {
        // Si la caja está agarrada, sigue al punto de agarre del jugador
        if (isHeld && holder != null && holder.GetCrateHoldPoint() != null)
        {
            Transform holdPoint = holder.GetCrateHoldPoint();
            transform.position = holdPoint.position;
            transform.rotation = holdPoint.rotation;
            // Mantener la escala original de la caja (no heredar escalas del padre)
            transform.localScale = originalScale;
        }
    }

    public bool TryPickUp(PlayerControllerDNA newHolder)
    {
        if (isHeld) return false;
        if (newHolder.HasDNA()) return false;
        if (newHolder.IsStunned()) return false;
        if (newHolder.GetCrateHoldPoint() == null) return false;

        // Al agarrar, también limpiamos el lanzador
        lastThrower = null;
        ignoreThrowerUntil = 0f;

        holder = newHolder;
        isHeld = true;
        wasThrown = false;
        SetPhysicsEnabled(false);
        sr.enabled = true;
        return true;
    }

    public void Throw(Vector2 direction, float playerVelocityMagnitude)
    {
        if (!isHeld) return;
        lastThrower = holder;
        ignoreThrowerUntil = Time.time + 0.3f;

        isHeld = false;
        holder = null;

        float t = Mathf.Clamp01(playerVelocityMagnitude / 10f);
        float force = Mathf.Lerp(throwBaseForce, throwRunningForce, t);
        Vector2 throwForce = new Vector2(direction.x * force, throwUpward);

        SetPhysicsEnabled(true);
        rb.AddForce(throwForce, ForceMode2D.Impulse);
        rb.angularVelocity = Random.Range(-360f, 360f);

        wasThrown = true;
        throwTime = Time.time;
        lastStunTime = -100f;
    }

    public void DropAtPlace()
    {
        if (!isHeld) return;
        if (holder == null) return;

        lastThrower = null;
        ignoreThrowerUntil = 0f;

        // Separar la caja del hold point (si estaba siguiendo al jugador)
        transform.SetParent(null);

        // Calcular posición delante del jugador según dirección que mira
        Vector2 dropPos = (Vector2)holder.transform.position + (holder.IsFacingRight() ? Vector2.right : Vector2.left) * 0.8f;
        transform.position = dropPos;

        // Restablecer estado
        isHeld = false;
        holder = null;
        wasThrown = false;

        // Asegurar visibilidad y físicas
        gameObject.SetActive(true);
        sr.enabled = true;
        SetPhysicsEnabled(true);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isHeld) return;
        if (!wasThrown || Time.time - throwTime > 1f) return;
        if (rb.linearVelocity.magnitude < minSpeedToStun) return;
        if (Time.time - lastStunTime < stunCooldown) return;

        PlayerControllerDNA target = collision.gameObject.GetComponent<PlayerControllerDNA>();
        if (target == null) return;
        if (target.IsShieldActive()) return;

        // CORRECCIÓN: ignorar al lanzador durante la ventana de exclusión
        if (target == lastThrower && Time.time < ignoreThrowerUntil) return;

        // Aplicar stun
        target.Stun(stunDuration);
        lastStunTime = Time.time;

        int throwerPlayer = (lastThrower != null) ? (lastThrower.CompareTag("Player1") ? 1 : 2) : 0;


        if (target.HasDNA())
        {
            DNA dna = target.GetCarriedDNA();

            if (dna != null)
            {
                dna.transform.position = target.transform.position;
                dna.gameObject.SetActive(true);
                Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
                dna.ThrowByHit(randomDir, throwerPlayer);
                dna.SetSpinEffect();
                target.DropDNA();
            }
        }
        rb.linearVelocity = rb.linearVelocity * 0.5f;
        wasThrown = false;
    
    }

    public void DestroyCrate()
    {
        Destroy(gameObject);
    }
}