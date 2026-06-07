using UnityEngine;
using System.Collections;

/// SpaceAlien
///
/// SETUP del prefab:
///   - SpriteRenderer con el sprite del alien gusano
///   - Rigidbody2D: Gravity Scale=0, Body Type=Dynamic, Collision Detection=Continuous
///   - CircleCollider2D: Is Trigger = TRUE (para detectar jugadores sin chocar fisicamente)
///   - Un CircleCollider2D adicional: Is Trigger = FALSE, radio pequeño (para detectar paredes)
///     O configurar la deteccion de paredes solo con raycasts (recomendado, ver abajo)
///   - Este script
///
/// CAPAS: la capa del alien NO debe colisionar fisicamente con casi nada.
/// La deteccion de paredes y obstaculos se hace por Raycast, no por fisica.

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SpaceAlien : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  ESTADOS
    // -------------------------------------------------------------------------

    private enum AlienState
    {
        Exploring,   // Vaga organicamente por la zona
        Chasing,     // Persigue al jugador mas cercano
        Investigating // Fue al ultimo punto donde vio al jugador
    }

    // -------------------------------------------------------------------------
    //  INSPECTOR
    // -------------------------------------------------------------------------

    [Header("Movimiento base")]
    [Tooltip("Velocidad normal de exploracion")]
    [SerializeField] private float exploreSpeed = 2.5f;
    [Tooltip("Velocidad al perseguir un jugador")]
    [SerializeField] private float chaseSpeed = 4f;
    [Tooltip("Variacion de velocidad organica (se suma/resta aleatoriamente)")]
    [SerializeField] private float speedVariation = 0.5f;

    [Header("Movimiento serpenteo")]
    [Tooltip("Cuanto oscila lateralmente mientras avanza (gusano)")]
    [SerializeField] private float snakeAmplitude = 0.8f;
    [Tooltip("Frecuencia del serpenteo")]
    [SerializeField] private float snakeFrequency = 1.5f;

    [Header("Cambio de direccion (exploracion)")]
    [Tooltip("Segundos minimos antes de cambiar de direccion")]
    [SerializeField] private float minDirChangeInterval = 1.5f;
    [Tooltip("Segundos maximos antes de cambiar de direccion")]
    [SerializeField] private float maxDirChangeInterval = 3.5f;
    [Tooltip("Suavidad de los giros (menor = giro mas suave, mayor = mas brusco)")]
    [SerializeField] private float turnSmoothSpeed = 2.5f;

    [Header("Deteccion de obstaculos")]
    [Tooltip("Distancia a la que detecta una pared y empieza a girar")]
    [SerializeField] private float obstacleDetectDist = 1.5f;
    [Tooltip("Angulo de los rayos laterales para detectar paredes")]
    [SerializeField] private float obstacleRayAngle = 35f;
    [Tooltip("Tags y layers que considera obstaculo (Borders)")]
    [SerializeField] private string borderTag = "Borders";
    [Tooltip("Layer de asteroides interactuables")]
    [SerializeField] private string interactiveAsteroidLayer = "InteractiveAsteroid";

    [Header("Deteccion de jugadores")]
    [Tooltip("Radio de deteccion. Dentro de este circulo el alien persigue al jugador.")]
    [SerializeField] private float detectionRadius = 4f;
    [Tooltip("Segundos investigando el ultimo punto visto antes de volver a explorar")]
    [SerializeField] private float investigateDuration = 2f;

    [Header("Rotacion del sprite")]
    [Tooltip("Offset de rotacion del sprite. 0 = apunta a la derecha, -90 = apunta arriba")]
    [SerializeField] private float spriteRotationOffset = 0f;

    // -------------------------------------------------------------------------
    //  PRIVADAS
    // -------------------------------------------------------------------------

    private SpaceAlienShipsEvent eventManager;
    private Rigidbody2D rb;
    private CircleCollider2D triggerCollider;

    private AlienState state = AlienState.Exploring;

    private Vector2 moveDirection;
    private Vector2 targetDirection;
    private float currentSpeed;

    private float dirChangeTimer = 0f;
    private float dirChangeInterval = 2f;
    private float snakeTimer = 0f;
    private float speedVariationTimer = 0f;
    private float speedVariationTarget = 0f;

    private Transform chasedPlayer = null;
    private Vector2 lastSeenPosition;
    private float investigateTimer = 0f;

    private int interactiveAsteroidLayerIndex;
    private int[] obstacleLayers;

    private bool isDead = false;

    // -------------------------------------------------------------------------
    //  INIT
    // -------------------------------------------------------------------------

    public void Init(SpaceAlienShipsEvent manager)
    {
        eventManager = manager;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        rb.freezeRotation = true;

        // El CircleCollider2D principal como trigger para jugadores
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        interactiveAsteroidLayerIndex = LayerMask.NameToLayer(interactiveAsteroidLayer);

        // Layers que el raycast considera obstaculo
        obstacleLayers = new int[]
        {
            LayerMask.NameToLayer("Default"),
            interactiveAsteroidLayerIndex
        };
    }

    private void Start()
    {
        // Direccion inicial aleatoria
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        targetDirection = moveDirection;
        currentSpeed = exploreSpeed;

        dirChangeInterval = Random.Range(minDirChangeInterval, maxDirChangeInterval);
        dirChangeTimer = 0f;

        speedVariationTarget = Random.Range(-speedVariation, speedVariation);
    }

    // -------------------------------------------------------------------------
    //  UPDATE
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (isDead) return;

        UpdateSpeedVariation();
        UpdateState();
        HandleObstacleAvoidance();
        SmoothDirection();
        UpdateSnakeMovement();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        float speed = currentSpeed + speedVariationTarget;
        speed = Mathf.Max(speed, 0.5f);
        rb.linearVelocity = moveDirection * speed;
    }

    // -------------------------------------------------------------------------
    //  MAQUINA DE ESTADOS
    // -------------------------------------------------------------------------

    private void UpdateState()
    {
        switch (state)
        {
            case AlienState.Exploring:
                UpdateExploring();
                break;

            case AlienState.Chasing:
                UpdateChasing();
                break;

            case AlienState.Investigating:
                UpdateInvestigating();
                break;
        }
    }

    private void UpdateExploring()
    {
        currentSpeed = exploreSpeed;

        // Cambio de direccion organico
        dirChangeTimer += Time.deltaTime;
        if (dirChangeTimer >= dirChangeInterval)
        {
            PickNewExploreDirection();
            dirChangeTimer = 0f;
            dirChangeInterval = Random.Range(minDirChangeInterval, maxDirChangeInterval);
        }

        // Buscar jugador cercano
        Transform player = GetNearestPlayerInRange();
        if (player != null)
        {
            chasedPlayer = player;
            state = AlienState.Chasing;
            Debug.Log("[SpaceAlien] Jugador detectado. Persiguiendo.");
        }
    }

    private void UpdateChasing()
    {
        currentSpeed = chaseSpeed;

        if (chasedPlayer == null)
        {
            StartInvestigating(lastSeenPosition);
            return;
        }

        // Verificar que el jugador sigue en rango (con un margen extra para no perderlo enseguida)
        float dist = Vector2.Distance(transform.position, chasedPlayer.position);
        if (dist > detectionRadius * 1.5f)
        {
            lastSeenPosition = chasedPlayer.position;
            chasedPlayer = null;
            StartInvestigating(lastSeenPosition);
            return;
        }

        lastSeenPosition = chasedPlayer.position;

        // Dirigirse suavemente hacia el jugador
        Vector2 toPlayer = ((Vector2)chasedPlayer.position - (Vector2)transform.position).normalized;
        targetDirection = toPlayer;
    }

    private void UpdateInvestigating()
    {
        currentSpeed = exploreSpeed * 0.8f;

        investigateTimer -= Time.deltaTime;

        // Ir al ultimo punto visto
        Vector2 toTarget = (lastSeenPosition - (Vector2)transform.position);
        if (toTarget.magnitude > 0.3f)
            targetDirection = toTarget.normalized;
        else
            targetDirection = moveDirection; // ya llegamos, dar vueltas

        // Buscar jugador mientras investiga
        Transform player = GetNearestPlayerInRange();
        if (player != null)
        {
            chasedPlayer = player;
            state = AlienState.Chasing;
            return;
        }

        if (investigateTimer <= 0f)
        {
            state = AlienState.Exploring;
            PickNewExploreDirection();
            Debug.Log("[SpaceAlien] Fin de investigacion. Volviendo a explorar.");
        }
    }

    private void StartInvestigating(Vector2 lastPos)
    {
        lastSeenPosition = lastPos;
        investigateTimer = investigateDuration;
        state = AlienState.Investigating;
        Debug.Log("[SpaceAlien] Jugador perdido. Investigando ultimo punto visto.");
    }

    // -------------------------------------------------------------------------
    //  MOVIMIENTO ORGANICO
    // -------------------------------------------------------------------------

    private void UpdateSpeedVariation()
    {
        speedVariationTimer -= Time.deltaTime;
        if (speedVariationTimer <= 0f)
        {
            speedVariationTarget = Random.Range(-speedVariation, speedVariation);
            speedVariationTimer = Random.Range(0.8f, 2f);
        }
    }

    private void PickNewExploreDirection()
    {
        // Giro organico: elegir un angulo relativo a la direccion actual
        float currentAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        float turnAngle = Random.Range(-90f, 90f);
        float newAngle = (currentAngle + turnAngle) * Mathf.Deg2Rad;
        targetDirection = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
    }

    private void SmoothDirection()
    {
        // Interpolar suavemente hacia la target direction
        moveDirection = Vector2.Lerp(moveDirection, targetDirection, turnSmoothSpeed * Time.deltaTime).normalized;
    }

    private void UpdateSnakeMovement()
    {
        // Añadir oscilacion lateral para efecto gusano
        snakeTimer += Time.deltaTime;
        float sineValue = Mathf.Sin(snakeTimer * snakeFrequency) * snakeAmplitude * Time.deltaTime;

        Vector2 perp = new Vector2(-moveDirection.y, moveDirection.x);
        Vector2 snakeOffset = perp * sineValue;

        // No reemplazamos moveDirection con esto, lo aplicamos directo al rb en fixed
        // para no acumular drift. Lo guardamos aparte.
        rb.linearVelocity += snakeOffset;
    }

    private void UpdateRotation()
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + spriteRotationOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // -------------------------------------------------------------------------
    //  EVASION DE OBSTACULOS
    // -------------------------------------------------------------------------

    private void HandleObstacleAvoidance()
    {
        int layerMask = BuildObstacleMask();

        // Rayo central
        bool centerHit = Physics2D.Raycast(transform.position, moveDirection, obstacleDetectDist, layerMask);

        // Rayo izquierda
        Vector2 leftDir = Rotate(moveDirection, obstacleRayAngle);
        bool leftHit = Physics2D.Raycast(transform.position, leftDir, obstacleDetectDist * 0.8f, layerMask);

        // Rayo derecha
        Vector2 rightDir = Rotate(moveDirection, -obstacleRayAngle);
        bool rightHit = Physics2D.Raycast(transform.position, rightDir, obstacleDetectDist * 0.8f, layerMask);

        // Tambien chequear con el borderTag usando OverlapCircle
        bool borderNear = IsBorderNear();

        if (!centerHit && !leftHit && !rightHit && !borderNear) return;

        // Elegir direccion de evasion
        Vector2 evadeDir;

        if (centerHit && leftHit && rightHit)
        {
            // Bloqueado de frente y ambos lados: dar vuelta completa
            evadeDir = -moveDirection;
        }
        else if (centerHit || borderNear)
        {
            // Girar hacia el lado mas libre
            evadeDir = rightHit ? leftDir : rightDir;
        }
        else if (leftHit)
        {
            evadeDir = rightDir;
        }
        else
        {
            evadeDir = leftDir;
        }

        targetDirection = evadeDir.normalized;
    }

    private bool IsBorderNear()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, obstacleDetectDist * 0.6f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.CompareTag(borderTag)) return true;
        }
        return false;
    }

    private int BuildObstacleMask()
    {
        // Incluir Default (que tiene Borders) + InteractiveAsteroid + objetos sin layer/tag (layer 0)
        int mask = LayerMask.GetMask("Default");

        if (interactiveAsteroidLayerIndex >= 0)
            mask |= (1 << interactiveAsteroidLayerIndex);

        return mask;
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // -------------------------------------------------------------------------
    //  DETECCION DE JUGADORES
    // -------------------------------------------------------------------------

    private Transform GetNearestPlayerInRange()
    {
        GameObject p1 = GameObject.FindWithTag("Player1");
        GameObject p2 = GameObject.FindWithTag("Player2");

        Transform nearest = null;
        float nearestDist = detectionRadius;

        if (p1 != null)
        {
            float d = Vector2.Distance(transform.position, p1.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = p1.transform;
            }
        }

        if (p2 != null)
        {
            float d = Vector2.Distance(transform.position, p2.transform.position);
            if (d < nearestDist)
            {
                nearest = p2.transform;
            }
        }

        return nearest;
    }

    // -------------------------------------------------------------------------
    //  COLISION CON JUGADOR
    // -------------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        bool isP1 = other.CompareTag("Player1");
        bool isP2 = other.CompareTag("Player2");

        if (!isP1 && !isP2) return;

        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship == null) return;

        Die(isP1 ? 2 : 1); // el que mato al alien es el rival del que fue golpeado

        // Aplicar slow al jugador golpeado
        if (eventManager != null)
        {
            float slowDuration = eventManager.GetSlowDuration();
            float slowMultiplier = eventManager.GetSlowMultiplier();
            ship.ApplySlow(slowDuration, slowMultiplier);
        }

        Debug.Log($"[SpaceAlien] Colisiono con {other.gameObject.name}. Slow aplicado.");
    }

    // -------------------------------------------------------------------------
    //  MUERTE
    // -------------------------------------------------------------------------

    private void Die(int killerPlayer)
    {
        if (isDead) return;
        isDead = true;

        eventManager?.OnAlienDied(this, killerPlayer);
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    //  GIZMOS
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Radio de deteccion
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Rayos de evasion
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + moveDirection * obstacleDetectDist);

            Gizmos.color = Color.cyan;
            Vector2 l = Rotate(moveDirection, obstacleRayAngle);
            Vector2 r = Rotate(moveDirection, -obstacleRayAngle);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + l * obstacleDetectDist * 0.8f);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + r * obstacleDetectDist * 0.8f);
        }
    }
#endif
}