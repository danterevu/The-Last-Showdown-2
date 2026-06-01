using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InteractiveAsteroid : MonoBehaviour
{
    [Header("Velocidad necesaria para matar")]
    [SerializeField] private float lethalSpeed = 15f;

    [Header("Seguridad: No matar a quien lo empujó")]
    [SerializeField] private float safetyTime = 2f;

    [Header("Bordes")]
    [SerializeField] private float bounceBreakSpeed = 15f;
    [SerializeField] private GameObject[] debrisPrefabs;
    [SerializeField] private float debrisLaunchForce = 8f;

    [Header("Debug")]
    [SerializeField] private float currentSpeed;

    private Rigidbody2D rb;
    private int lastPusherPlayerIndex = 0;
    private float lastPushTime = -100f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log($"[INTERACTIVE ASTEROID] {gameObject.name} initialized with safetyTime={safetyTime}s");
    }

    public void SetLastPusher(int playerIndex)
    {
        lastPusherPlayerIndex = playerIndex;
        lastPushTime = Time.time;
    }

    private void Update()
    {
        currentSpeed = rb.linearVelocity.magnitude;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[INTERACTIVE ASTEROID] Colisión con: {collision.gameObject.name} | Tag: {collision.gameObject.tag}");
        
        // Colisión con borde
        if (collision.gameObject.CompareTag("Border"))
        {
            Debug.Log($"[INTERACTIVE ASTEROID] Chocó con borde | Velocidad: {currentSpeed:F2} | Bounce break: {bounceBreakSpeed}");
            if (currentSpeed >= bounceBreakSpeed)
            {
                Debug.Log("[INTERACTIVE ASTEROID] Se rompe por velocidad contra borde");
                SpawnDebris(collision.contacts[0].normal);
                Destroy(gameObject);
            }
            else
            {
                if (rb != null)
                {
                    rb.linearVelocity *= 0.7f;
                }
            }
            return;
        }

        // Solo jugadores
        int hitPlayer = 0;
        if (collision.gameObject.CompareTag("Player1"))
            hitPlayer = 1;
        else if (collision.gameObject.CompareTag("Player2"))
            hitPlayer = 2;
        else
        {
            Debug.Log($"[INTERACTIVE ASTEROID] No es un jugador, ignorando colisión");
            return;
        }

        
        // Si el jugador acaba de empujar el meteorito, no lo matamos
        if (hitPlayer == lastPusherPlayerIndex) {
            Debug.Log($"[INTERACTIVE ASTEROID] hitPlayer == lastPusherPlayerIndex");
            float timeSincePush = Time.time - lastPushTime;
            if (timeSincePush < safetyTime) {
                Debug.Log($"[INTERACTIVE ASTEROID] No matamos: es el último empujador y está dentro del safety time (t={timeSincePush:F2}s < {safetyTime}s");
                return;
            } else {
                Debug.Log($"[INTERACTIVE ASTEROID] Sí matamos: es el último empujador pero se pasó el safety time (t={timeSincePush:F2}s >= {safetyTime}s");
            }
        }

        // Calcular velocidad de CIERRE para determinar si mata (solo si el asteroide se acerca al jugador)
        SpaceShipController ship = collision.gameObject.GetComponent<SpaceShipController>();
        float closingSpeed = 0f;
        if (ship != null)
        {
            Vector2 shipVelocity = ship.GetVelocity();
            Vector2 relativeVelocity = rb.linearVelocity - shipVelocity;
            Vector2 toPlayer = (Vector2)collision.gameObject.transform.position - rb.position;
            closingSpeed = Vector2.Dot(relativeVelocity, toPlayer.normalized);
            Debug.Log($"[INTERACTIVE ASTEROID] Velocidad asteroide: {currentSpeed:F2} | Velocidad jugador: {shipVelocity.magnitude:F2} | Velocidad relativa: {relativeVelocity.magnitude:F2} | Velocidad de cierre: {closingSpeed:F2}");
        }

        // Si la velocidad de CIERRE no es suficientemente alta (no se acerca), no matamos
        if (closingSpeed < lethalSpeed)
        {
            Debug.Log($"[INTERACTIVE ASTEROID] No matamos: velocidad de cierre {closingSpeed:F2} < letal {lethalSpeed:F2} | Marcando como último empujador: {hitPlayer}");
            // Si el jugador choca con el meteorito, marcarlo como el último empujador
            SetLastPusher(hitPlayer);
            return;
        }

        Debug.LogWarning($"[INTERACTIVE ASTEROID] ¡¡MATANDO JUGADOR {hitPlayer}!! | Velocidad asteroide: {currentSpeed:F2} | Letal: {lethalSpeed:F2} | Cierre: {closingSpeed:F2}");
        SpaceMinigame.Instance?.RegisterKill(0, hitPlayer);
    }

    private void SpawnDebris(Vector2 collisionNormal)
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            Debug.LogWarning("[INTERACTIVE ASTEROID] No hay debrisPrefabs asignados, no se spawnean fragmentos");
            return;
        }

        int count = Random.Range(2, debrisPrefabs.Length + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (prefab == null)
            {
                Debug.LogWarning("[INTERACTIVE ASTEROID] Uno de los debrisPrefabs es NULL, saltando este fragmento");
                continue;
            }

            GameObject debris = Instantiate(prefab, transform.position, Quaternion.identity);
            Rigidbody2D debrisRb = debris.GetComponent<Rigidbody2D>();
            if (debrisRb != null)
            {
                Vector2 randomDir = (collisionNormal + Random.insideUnitCircle).normalized;
                debrisRb.AddForce(randomDir * debrisLaunchForce, ForceMode2D.Impulse);
            }
        }
    }
}