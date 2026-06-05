using UnityEngine;
using System.Collections;
public class Mine : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float armDelay = 1f;           // tiempo tras golpear pared para volverse invisible
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float armedAlpha = 0.15f;      // transparencia cuando armada

    [Header("DNA")]
    [SerializeField] private float dnaThrowForceX = 6f;
    [SerializeField] private float dnaThrowForceY = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D triggerCollider;   // para detectar jugadores
    private Collider2D physicsCollider;   // para chocar con paredes
    private bool isArmed = false;
    private int ownerPlayer;
    private bool hasHitWall = false;      // para que solo se active una vez al golpear pared

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Buscar los dos colliders: el trigger y el físico
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col.isTrigger)
                triggerCollider = col;
            else
                physicsCollider = col;
        }

        // Inicialmente: el trigger desactivado, el físico activado (para rebotar contra paredes)
        triggerCollider.enabled = false;
        if (physicsCollider != null) physicsCollider.enabled = true;

        // El cuerpo dinámico por defecto
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void Init(int owner)
    {
        ownerPlayer = owner;
        // Opcional: darle un pequeño impulso hacia adelante al instanciarla
        // Puedes añadir una velocidad inicial aquí si la lanzas desde el jugador
        // Por ejemplo: rb.linearVelocity = direction * throwForce;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Si ya golpeó una pared, ignorar más colisiones
        if (hasHitWall) return;

        // Comprobar si colisiona con la capa "Walls"
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            hasHitWall = true;

            // Detener la mina en el punto de impacto
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Desactivar el collider físico (ya no necesita chocar con paredes)
            if (physicsCollider != null) physicsCollider.enabled = false;

            // Iniciar la secuencia de armado (volverse trigger y casi invisible)
            StartCoroutine(ArmSequence());
        }
    }

    private IEnumerator ArmSequence()
    {
        // Esperar el tiempo de armado (puede ser 0 si quieres instantáneo)
        yield return new WaitForSeconds(armDelay);

        // Volverse casi invisible
        Color c = sr.color;
        c.a = armedAlpha;
        sr.color = c;

        // Activar el trigger para detectar jugadores
        triggerCollider.enabled = true;
        isArmed = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isArmed) return;

        // Identificar al jugador que toca la mina
        int hitPlayer = other.CompareTag("Player1") ? 1 : other.CompareTag("Player2") ? 2 : 0;
        if (hitPlayer == 0 || hitPlayer == ownerPlayer) return; // no daña al dueño

        PlayerControllerDNA target = other.GetComponent<PlayerControllerDNA>();
        if (target == null) return;

        // Dirección de la explosión (aleatoria X, siempre hacia arriba)
        float dirX = Random.value > 0.5f ? 1f : -1f;
        Vector2 knockDir = new Vector2(dirX, 0.5f).normalized;

        // Aplicar knockback y stun
        target.ReceiveMineHit(knockDir, knockbackForce, stunDuration);

        // Si tiene DNA, hacerlo volar
        if (target.HasDNA() && target.GetCarriedDNA() != null)
        {
            DNA dna = target.GetCarriedDNA();
            dna.transform.position = target.transform.position;
            dna.gameObject.SetActive(true);

            Vector2 throwDir = new Vector2(
                dirX * Random.Range(0.8f, 1.2f),
                Random.Range(0.8f, 1.2f)
            );
            //  Pasar el ownerPlayer como lanzador (el que puso la mina)
            dna.ThrowByHit(throwDir, ownerPlayer);
            dna.SetSpinEffect();
            target.DropDNA();
        }

        // Destruir la mina
        Destroy(gameObject);
    }
}