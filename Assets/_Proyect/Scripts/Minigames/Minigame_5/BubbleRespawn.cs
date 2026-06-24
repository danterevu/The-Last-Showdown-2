using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// BubbleRespawn  v3
//
//  Fix spawn-dentro-de-plataforma:
//    Después de teletransportar al jugador, usamos Collider2D.Cast() para
//    detectar si el collider del jugador está solapando con el tilemap y
//    calculamos el vector mínimo de separación para empujarlo afuera.
//    Se repite hasta N iteraciones hasta que no haya penetración.
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(ChaseRunPlayerController))]
public class BubbleRespawn : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float respawnDelay = 0.5f;
    [SerializeField] private float bubbleDuration = 4f;
    [SerializeField] private float blinkTime = 1.2f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Movimiento en burbuja")]
    [SerializeField] private float bubbleFlySpeed = 4f;

    [Header("Visual")]
    [SerializeField] private Sprite bubbleSprite;
    [SerializeField] private Vector3 bubbleScale = new Vector3(1.6f, 1.6f, 1f);
    [SerializeField] private GameObject bubbleObject;

    [Header("Spawn")]
    [SerializeField] private float spawnAboveCamera = 1.5f;
    [SerializeField] private float playerXOffset = 1f;
    [Tooltip("Layer de suelo/plataformas — mismo que el PlayerController.")]
    [SerializeField] private LayerMask groundLayer;



    // ── Referencias ───────────────────────────────────────────────────────────

    private ChaseRunPlayerController playerController;
    private ChaseRunCamera chaseCamera;
    private Rigidbody2D rb;
    private Collider2D bodyCol;
    private SpriteRenderer playerSr;
    private SpriteRenderer bubbleSr;

    // ── Estado ────────────────────────────────────────────────────────────────

    private bool inBubble = false;
    private Vector2 colliderSize = Vector2.one; // cached en Awake, nunca es (0,0)
    public bool IsInBubble => inBubble;
    public Vector2 BubbleVelocity { get; private set; }

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Awake()
    {
        playerController = GetComponent<ChaseRunPlayerController>();
        rb = GetComponent<Rigidbody2D>();
        playerSr = GetComponent<SpriteRenderer>();

        // Tomar el collider body (no-trigger)
        foreach (var c in GetComponents<Collider2D>())
            if (!c.isTrigger) { bodyCol = c; break; }

        // Cachear el tamaño AHORA que el GO está activo y bounds es válido.
        // bounds puede ser (0,0,0) si el GO está desactivado o sin render.
        if (bodyCol != null)
        {
            // Forzar que Unity calcule los bounds activando el collider
            colliderSize = bodyCol.bounds.size;

            // Si por alguna razón sigue siendo cero, leer desde el tipo concreto
            if (colliderSize.sqrMagnitude < 0.001f)
            {
                if (bodyCol is BoxCollider2D box)
                    colliderSize = box.size;
                else if (bodyCol is CapsuleCollider2D cap)
                    colliderSize = cap.size;
                else
                    colliderSize = Vector2.one * 0.8f; // fallback generoso
            }
        }

        SetupBubbleVisual();
    }

    private void Start()
    {
        chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();
        SetBubbleVisible(false);
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    private void SetupBubbleVisual()
    {
        if (bubbleObject == null)
        {
            bubbleObject = new GameObject("Bubble");
            bubbleObject.transform.SetParent(transform);
            bubbleObject.transform.localPosition = Vector3.zero;
            bubbleObject.transform.localScale = bubbleScale;
            bubbleSr = bubbleObject.AddComponent<SpriteRenderer>();
            bubbleSr.sprite = bubbleSprite;
            bubbleSr.sortingOrder = playerSr != null ? playerSr.sortingOrder - 1 : 0;
        }
        else
        {
            bubbleSr = bubbleObject.GetComponent<SpriteRenderer>()
                    ?? bubbleObject.AddComponent<SpriteRenderer>();
            bubbleSr.sprite = bubbleSprite;
        }
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void TriggerRespawn()
    {
        if (inBubble) return;
        StartCoroutine(RespawnSequence());
    }

    // ── Secuencia principal ───────────────────────────────────────────────────

    private IEnumerator RespawnSequence()
    {
        // FASE 1: Congelar (physics off, input sigue leyendo)
        playerController.SetFrozen(true);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        if (playerSr != null) playerSr.enabled = false;
        SetBubbleVisible(false);

        yield return new WaitForSeconds(respawnDelay);

        // FASE 2: Teletransportar arriba de cámara y empujar fuera de plataformas
        if (chaseCamera == null)
            chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();

        Vector3 spawnPos = FindClearPosition();
        transform.position = spawnPos;

        if (playerSr != null) playerSr.enabled = true;
        SetBubbleVisible(true);

        inBubble = true;
        BubbleVelocity = Vector2.zero;
        playerController.SetImmune(true);
        playerController.SetFrozen(false);

        // FASE 3: Vuelo libre
        float elapsed = 0f;
        float normalTime = bubbleDuration - blinkTime;

        while (elapsed < normalTime)
        {
            UpdateBubbleVelocity();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // FASE 4: Parpadeo
        float blinkElapsed = 0f;
        bool visible = true;

        while (blinkElapsed < blinkTime)
        {
            UpdateBubbleVelocity();
            blinkElapsed += Time.deltaTime;

            bool shouldBeVisible = (blinkElapsed % (blinkInterval * 2f)) < blinkInterval;
            if (shouldBeVisible != visible)
            {
                visible = shouldBeVisible;
                SetBubbleVisible(visible);
            }

            yield return null;
        }

        // FASE 5: Explosión
        SetBubbleVisible(false);
        BubbleVelocity = Vector2.zero;
        inBubble = false;
        playerController.SetImmune(false);
    }

    // ── Posición base de spawn (arriba de cámara) ─────────────────────────────

    // ── Encuentra la primera posición libre de plataformas ───────────────────
    //
    //  Empieza arriba de la cámara y sube de a pasos hasta que
    //  OverlapBox con el tamaño del collider del jugador no toque Ground.

    private Vector3 FindClearPosition()
    {
        if (chaseCamera == null) return transform.position;

        int pIdx = playerController.PlayerNumber - 1;
        float offsetX = (pIdx == 0) ? -playerXOffset : playerXOffset;
        float x = chaseCamera.CenterX + offsetX;
        float y = chaseCamera.GetTopBound() + spawnAboveCamera;

        // Usar el tamaño cacheado en Awake (nunca es cero)
        Vector2 size = colliderSize * 0.9f;

        for (int i = 0; i < 60; i++)
        {
            if (Physics2D.OverlapBox(new Vector2(x, y), size, 0f, groundLayer) == null)
                return new Vector3(x, y, 0f);
            y += size.y * 0.5f; // pasos más chicos para no saltear huecos
        }

        return new Vector3(x, y, 0f);
    }

    // ── Velocidad de burbuja ──────────────────────────────────────────────────

    private void UpdateBubbleVelocity()
    {
        BubbleVelocity = playerController.GetMoveInput() * bubbleFlySpeed;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetBubbleVisible(bool visible)
    {
        if (bubbleObject != null) bubbleObject.SetActive(visible);
    }
}