using UnityEngine;
using System.Collections;



[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ChaseRunPowerUpPickup : MonoBehaviour
{
    public enum PowerUpType
    {
        SpeedBoost,     // +velocidad al recolector por X segundos
        ScoreBonus,     // puntos extra inmediatos
        Shield,         // inmunidad a kill zone por X segundos
        SlowOpponent,   // ralentiza al oponente por X segundos
        ExtraJump       // +1 salto en el aire por X segundos
    }

    [Header("Tipo")]
    [SerializeField] private PowerUpType type = PowerUpType.SpeedBoost;

    [Header("Duración del efecto")]
    [SerializeField] private float effectDuration = 5f;

    [Header("Puntos (solo ScoreBonus)")]
    [SerializeField] private int scoreBonus = 10;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Un sprite por cada valor del enum PowerUpType, en el mismo orden.")]
    [SerializeField] private Sprite[] typeSprites;

    // Movimiento 

    private enum MoveMode { Falling, MovingLeft, None }
    private MoveMode moveMode = MoveMode.None;

    private Rigidbody2D rb;
    private ChaseRunCamera chaseCamera;
    private ChaseRunPowerUpSpawner spawner;
    private bool collected = false;

    // ── Inicialización ────────────────────────────────────────────────────────

    private void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();
        spawner     = Object.FindFirstObjectByType<ChaseRunPowerUpSpawner>();

        // Aleatorizar tipo al spawnear
        type = (PowerUpType)Random.Range(0, System.Enum.GetValues(typeof(PowerUpType)).Length);

        // Asignar sprite si hay uno definido
        if (spriteRenderer != null && typeSprites != null && (int)type < typeSprites.Length)
            spriteRenderer.sprite = typeSprites[(int)type];
    }

    // ── Configuración desde el spawner ────────────────────────────────────────

    /// <summary>Fase Y: el power up cae por gravedad.</summary>
    public void SetupFalling(float fallSpeed)
    {
        moveMode        = MoveMode.Falling;
        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.down * fallSpeed;
    }

    /// <summary>Fase X: el power up se mueve hacia la izquierda a velocidad constante.</summary>
    public void SetupMovingLeft(float horizontalSpeed)
    {
        moveMode          = MoveMode.MovingLeft;
        rb.gravityScale   = 0f;
        rb.linearVelocity = Vector2.left * horizontalSpeed;
    }

    // ── Culling — destruir si sale de cámara ──────────────────────────────────

    private void Update()
    {
        if (collected || chaseCamera == null) return;

        float killBound = chaseCamera.GetKillZoneBound();
        bool outOfBounds = false;

        switch (moveMode)
        {
            case MoveMode.Falling:
                // Destruir si cae por debajo de la kill zone
                outOfBounds = transform.position.y < killBound - 2f;
                break;
            case MoveMode.MovingLeft:
                // Destruir si sale por la izquierda de la kill zone
                outOfBounds = transform.position.x < killBound - 2f;
                break;
        }

        if (outOfBounds)
        {
            spawner?.OnPickupCollected(gameObject);
            Destroy(gameObject);
        }
    }

    // ── Colisión con jugadores ────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        if (!other.CompareTag("Player")) return;

        ChaseRunPlayerController player = other.GetComponent<ChaseRunPlayerController>();
        if (player == null) return;

        collected = true;
        ApplyEffect(player);

        spawner?.OnPickupCollected(gameObject);
        Destroy(gameObject);
    }

    // ── Efectos ───────────────────────────────────────────────────────────────

    private void ApplyEffect(ChaseRunPlayerController player)
    {
        int pNum  = player.PlayerNumber;
        int opNum = pNum == 1 ? 2 : 1;

        switch (type)
        {
            case PowerUpType.ScoreBonus:
                GameManager.Instance?.AddPoints(pNum, scoreBonus);
                break;

            case PowerUpType.SpeedBoost:
                player.StartCoroutine(SpeedBoostCoroutine(player));
                break;

            case PowerUpType.Shield:
                player.StartCoroutine(ShieldCoroutine(player));
                break;

            case PowerUpType.SlowOpponent:
                ChaseRunPlayerController opponent = ChaseRunManager.Instance?.GetPlayer(opNum);
                if (opponent != null)
                    opponent.StartCoroutine(SlowCoroutine(opponent));
                break;

            case PowerUpType.ExtraJump:
                player.StartCoroutine(ExtraJumpCoroutine(player));
                break;
        }
    }

    // ── Coroutines de efectos ─────────────────────────────────────────────────

    private IEnumerator SpeedBoostCoroutine(ChaseRunPlayerController player)
    {
        player.ApplySpeedMultiplier(1.6f);
        yield return new WaitForSeconds(effectDuration);
        player.ApplySpeedMultiplier(1f);
    }

    private IEnumerator ShieldCoroutine(ChaseRunPlayerController player)
    {
        player.SetKillZoneImmunity(true);
        yield return new WaitForSeconds(effectDuration);
        player.SetKillZoneImmunity(false);
    }

    private IEnumerator SlowCoroutine(ChaseRunPlayerController player)
    {
        player.ApplySpeedMultiplier(0.5f);
        yield return new WaitForSeconds(effectDuration);
        player.ApplySpeedMultiplier(1f);
    }

    private IEnumerator ExtraJumpCoroutine(ChaseRunPlayerController player)
    {
        player.AddExtraJump();
        yield return new WaitForSeconds(effectDuration);
        player.RemoveExtraJump();
    }
}
