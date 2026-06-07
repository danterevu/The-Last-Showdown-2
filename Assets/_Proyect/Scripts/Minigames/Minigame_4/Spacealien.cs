using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SpaceAlien : MonoBehaviour
{
    private enum AlienState { Exploring, Chasing, Investigating }

    [Header("Movimiento base")]
    [SerializeField] private float exploreSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float speedVariation = 0.5f;

    [Header("Movimiento serpenteo")]
    [SerializeField] private float snakeAmplitude = 0.5f;
    [SerializeField] private float snakeFrequency = 1.5f;

    [Header("Cambio de direccion (exploracion)")]
    [SerializeField] private float minDirChangeInterval = 1.5f;
    [SerializeField] private float maxDirChangeInterval = 3.5f;
    [SerializeField] private float turnSmoothSpeed = 2.5f;

    [Header("Deteccion de obstaculos")]
    [Tooltip("Distancia a la que detecta una pared y empieza a girar")]
    [SerializeField] private float obstacleDetectDist = 1.2f;
    [Tooltip("Angulo de los rayos laterales")]
    [SerializeField] private float obstacleRayAngle = 45f;
    [Tooltip("Tag de los bordes de la zona")]
    [SerializeField] private string borderTag = "Border";
    [Tooltip("Layer de asteroides interactuables")]
    [SerializeField] private string interactiveAsteroidLayer = "InteractiveAsteroid";
    [Tooltip("Radio del OverlapCircle para detectar bordes cercanos")]
    [SerializeField] private float borderCheckRadius = 1.0f;

    [Header("Deteccion de jugadores")]
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float investigateDuration = 2f;

    [Header("Rotacion del sprite")]
    [SerializeField] private float spriteRotationOffset = 0f;

    // -------------------------------------------------------------------------

    private SpaceAlienShipsEvent eventManager;
    private Rigidbody2D rb;

    private AlienState state = AlienState.Exploring;

    private Vector2 moveDirection;
    private Vector2 targetDirection;
    private float currentSpeed;

    private float dirChangeTimer = 0f;
    private float dirChangeInterval = 2f;
    private float snakeTimer = 0f;
    private float speedVariationTimer = 0f;
    private float speedVariationTarget = 0f;

    // Snake offset calculado en Update, aplicado en FixedUpdate
    private Vector2 pendingSnakeOffset = Vector2.zero;

    private Transform chasedPlayer = null;
    private Vector2 lastSeenPosition;
    private float investigateTimer = 0f;

    private int interactiveAsteroidLayerIndex;
    private int obstacleMask;

    private bool isDead = false;

    // -------------------------------------------------------------------------

    public void Init(SpaceAlienShipsEvent manager)
    {
        eventManager = manager;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 3f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = true;

        interactiveAsteroidLayerIndex = LayerMask.NameToLayer(interactiveAsteroidLayer);

        // Construir mask una sola vez
        obstacleMask = LayerMask.GetMask("Default");
        if (interactiveAsteroidLayerIndex >= 0)
            obstacleMask |= (1 << interactiveAsteroidLayerIndex);
    }

    private void Start()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        targetDirection = moveDirection;
        currentSpeed = exploreSpeed;
        dirChangeInterval = Random.Range(minDirChangeInterval, maxDirChangeInterval);
        speedVariationTarget = Random.Range(-speedVariation, speedVariation);
    }

    // -------------------------------------------------------------------------

    private void Update()
    {
        if (isDead) return;

        UpdateSpeedVariation();
        UpdateState();
        HandleObstacleAvoidance();
        SmoothDirection();
        CalculateSnakeOffset();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        float speed = Mathf.Max(currentSpeed + speedVariationTarget, 0.5f);
        rb.linearVelocity = moveDirection * speed + pendingSnakeOffset;
        pendingSnakeOffset = Vector2.zero;
    }

    // -------------------------------------------------------------------------
    //  ESTADOS
    // -------------------------------------------------------------------------

    private void UpdateState()
    {
        switch (state)
        {
            case AlienState.Exploring: UpdateExploring(); break;
            case AlienState.Chasing: UpdateChasing(); break;
            case AlienState.Investigating: UpdateInvestigating(); break;
        }
    }

    private void UpdateExploring()
    {
        currentSpeed = exploreSpeed;

        dirChangeTimer += Time.deltaTime;
        if (dirChangeTimer >= dirChangeInterval)
        {
            PickNewExploreDirection();
            dirChangeTimer = 0f;
            dirChangeInterval = Random.Range(minDirChangeInterval, maxDirChangeInterval);
        }

        Transform player = GetNearestPlayerInRange();
        if (player != null)
        {
            chasedPlayer = player;
            state = AlienState.Chasing;
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

        float dist = Vector2.Distance(transform.position, chasedPlayer.position);
        if (dist > detectionRadius * 1.5f)
        {
            lastSeenPosition = chasedPlayer.position;
            chasedPlayer = null;
            StartInvestigating(lastSeenPosition);
            return;
        }

        lastSeenPosition = chasedPlayer.position;
        targetDirection = ((Vector2)chasedPlayer.position - (Vector2)transform.position).normalized;
    }

    private void UpdateInvestigating()
    {
        currentSpeed = exploreSpeed * 0.8f;
        investigateTimer -= Time.deltaTime;

        Vector2 toTarget = lastSeenPosition - (Vector2)transform.position;
        if (toTarget.magnitude > 0.3f)
            targetDirection = toTarget.normalized;

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
        }
    }

    private void StartInvestigating(Vector2 lastPos)
    {
        lastSeenPosition = lastPos;
        investigateTimer = investigateDuration;
        state = AlienState.Investigating;
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
        float currentAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        float turnAngle = Random.Range(-80f, 80f);
        float newAngle = (currentAngle + turnAngle) * Mathf.Deg2Rad;
        targetDirection = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
    }

    private void SmoothDirection()
    {
        moveDirection = Vector2.Lerp(moveDirection, targetDirection, turnSmoothSpeed * Time.deltaTime).normalized;
    }

    private void CalculateSnakeOffset()
    {
        // Calcula el offset lateral del serpenteo y lo guarda para aplicarlo en FixedUpdate
        snakeTimer += Time.deltaTime;
        float sineValue = Mathf.Sin(snakeTimer * snakeFrequency) * snakeAmplitude;
        Vector2 perp = new Vector2(-moveDirection.y, moveDirection.x);
        pendingSnakeOffset = perp * sineValue * Time.fixedDeltaTime;
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
        // Chequeo de bordes por tag (mas confiable que raycast para bordes irregulares)
        Vector2 borderPush = GetBorderAvoidanceDirection();
        bool borderNear = borderPush != Vector2.zero;

        if (borderNear)
        {
            // Forzar giro inmediato alejandose del borde
            targetDirection = borderPush;
            // Acelerar la suavidad del giro cuando hay borde cercano
            moveDirection = Vector2.Lerp(moveDirection, targetDirection, turnSmoothSpeed * 3f * Time.deltaTime).normalized;
            return;
        }

        // Raycast para obstaculos internos (asteroides, etc)
        bool centerHit = Physics2D.Raycast(transform.position, moveDirection, obstacleDetectDist, obstacleMask);
        Vector2 leftDir = Rotate(moveDirection, obstacleRayAngle);
        Vector2 rightDir = Rotate(moveDirection, -obstacleRayAngle);
        bool leftHit = Physics2D.Raycast(transform.position, leftDir, obstacleDetectDist * 0.8f, obstacleMask);
        bool rightHit = Physics2D.Raycast(transform.position, rightDir, obstacleDetectDist * 0.8f, obstacleMask);

        if (!centerHit && !leftHit && !rightHit) return;

        Vector2 evadeDir;
        if (centerHit && leftHit && rightHit)
            evadeDir = -moveDirection;
        else if (centerHit)
            evadeDir = rightHit ? leftDir : rightDir;
        else if (leftHit)
            evadeDir = rightDir;
        else
            evadeDir = leftDir;

        targetDirection = evadeDir.normalized;
    }

    /// Devuelve una direccion de alejamiento si hay bordes cerca, o Vector2.zero si no.
    private Vector2 GetBorderAvoidanceDirection()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, borderCheckRadius);
        Vector2 pushDir = Vector2.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.CompareTag(borderTag)) continue;

            // Direccion que aleja al alien del borde
            Vector2 awayFromBorder = ((Vector2)transform.position - (Vector2)hit.ClosestPoint(transform.position)).normalized;
            pushDir += awayFromBorder;
            count++;
        }

        if (count == 0) return Vector2.zero;
        return (pushDir / count).normalized;
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
            if (d < nearestDist) { nearestDist = d; nearest = p1.transform; }
        }

        if (p2 != null)
        {
            float d = Vector2.Distance(transform.position, p2.transform.position);
            if (d < nearestDist) { nearest = p2.transform; }
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

        // El que mata al alien es el rival del jugador golpeado
        int killerPlayer = isP1 ? 2 : 1;
        Die(killerPlayer);

        if (eventManager != null)
            ship.ApplySlow(eventManager.GetSlowDuration(), eventManager.GetSlowMultiplier());
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
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, borderCheckRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + moveDirection * obstacleDetectDist);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + Rotate(moveDirection, obstacleRayAngle) * obstacleDetectDist * 0.8f);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + Rotate(moveDirection, -obstacleRayAngle) * obstacleDetectDist * 0.8f);
        }
    }
#endif
}