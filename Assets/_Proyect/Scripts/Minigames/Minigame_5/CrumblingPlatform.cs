using UnityEngine;
using System.Collections;



[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CrumblingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Tiempo de shake antes de caer (segundos).")]
    [SerializeField] private float shakeTime = 0.8f;

    [Header("Shake visual")]
    [Tooltip("Magnitud del shake en unidades world.")]
    [SerializeField] private float shakeMagnitude = 0.05f;

    [Header("Caída")]
    [Tooltip("Gravedad del Rigidbody2D al caer. Mayor = cae más rápido.")]
    [SerializeField] private float fallGravityScale = 3f;

    [Header("Culling")]
    [Tooltip("Cuántas unidades por debajo de la kill zone se destruye.")]
    [SerializeField] private float destroyBelowKillZone = 4f;

    // Componentes

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator animator;
    private ChaseRunCamera chaseCamera;

    // Estado 

    private bool triggered = false;
    private bool falling = false;
    private Vector3 originalPosition;

    // Ciclo de vida 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        originalPosition = transform.position;
    }

    private void Start()
    {
        chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();
    }

    private void Update()
    {
        if (!falling || chaseCamera == null) return;

        // Destruir cuando salga completamente de la cámara
        float killBound = chaseCamera.GetKillZoneBound();

        bool outOfBounds = chaseCamera.CurrentPhase == ChaseRunManager.RunPhase.PhaseY
            ? transform.position.y < killBound - destroyBelowKillZone
            : transform.position.x < killBound - destroyBelowKillZone;

        if (outOfBounds)
            Destroy(gameObject);
    }

    // Detección de jugador 

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;

        // Solo activar si el jugador cae encima (contacto desde arriba)
        ChaseRunPlayerController player = collision.gameObject.GetComponent<ChaseRunPlayerController>();
        if (player == null) return;

        // Verificar que el contacto sea desde arriba
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)   
            {
                triggered = true;
                StartCoroutine(DoCrumble());
                break;
            }
        }
    }

    // Secuencia de rotura 

    private IEnumerator DoCrumble()
    {
        // Animación de shake si hay animator
        if (animator != null)
            animator.SetTrigger("shake");

        // Shake manual como fallback o complemento
        float elapsed = 0f;
        while (elapsed < shakeTime)
        {
            float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
            float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restablecer posición antes de caer
        transform.position = originalPosition;

        // Activar caída
        falling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;

        // Desactivar colisión para que los jugadores no queden atrapados
        col.enabled = false;

        if (animator != null)
            animator.SetTrigger("fall");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Indicador visual de que es una plataforma crumbling
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        var c = GetComponent<Collider2D>();
        if (c != null)
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
#endif
}
