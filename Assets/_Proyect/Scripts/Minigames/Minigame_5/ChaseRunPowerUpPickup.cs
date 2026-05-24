using UnityEngine;


/// Power up del Chase Run.
/// Se configura en runtime desde ChaseRunPowerUpSpawner:
///   - SetupFalling:     cae desde arriba con gravedad (fase Y)
///   - SetupMovingLeft:  se mueve en -X (fase X)
/// 
/// Al ser recogido dispara el efecto correspondiente a traves de PowerUpEffects
/// (mismo sistema que KOH) si está disponible, o aplica un efecto simple propio.
/// 
/// Requiere: Rigidbody2D en el prefab.

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ChaseRunPowerUpPickup : MonoBehaviour
{
    // Tipos de power up disponibles 
    public enum PowerUpType
    {
        SpeedBoost,       // +velocidad al jugador que lo recoge
        ScoreBonus,       // puntos extra inmediatos
        Shield,           // invencibilidad breve a la kill zone
        SlowOpponent,     // ralentiza al oponente
        ExtraJump         // +1 salto extra por un tiempo
    }

    [Header("Tipo")]
    [SerializeField] private PowerUpType type = PowerUpType.SpeedBoost;

    [Header("Duracion del efecto (si aplica)")]
    [SerializeField] private float effectDuration = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] typeSprites; // uno por cada PowerUpType, mismo orden que el enum

    // Movimiento
    private enum MoveMode { Falling, MovingLeft, None }
    private MoveMode moveMode = MoveMode.None;
    private float speed;

    private Rigidbody2D rb;
    private ChaseRunPowerUpSpawner spawner;
    private bool collected = false;



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // En el Start, buscar el spawner si no se asigno
        spawner = Object.FindFirstObjectByType<ChaseRunPowerUpSpawner>();

        // Aleatorizar tipo visualmente
        type = (PowerUpType)Random.Range(0, System.Enum.GetValues(typeof(PowerUpType)).Length);
        if (spriteRenderer != null && typeSprites != null && (int)type < typeSprites.Length)
            spriteRenderer.sprite = typeSprites[(int)type];
    }

    // Configuracion desde el spawner 

    /// Fase Y: el power up cae. Usa gravedad del Rigidbody2D + velocidad inicial.
    public void SetupFalling(float fallSpeed)
    {
        moveMode = MoveMode.Falling;
        speed = fallSpeed;

        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.down * fallSpeed;
    }

    /// Fase X: el power up se mueve horizontalmente hacia la izquierda.
    public void SetupMovingLeft(float horizontalSpeed)
    {
        moveMode = MoveMode.MovingLeft;
        speed = horizontalSpeed;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.left * horizontalSpeed;
    }

    //  Destruir si sale de camara (seguridad) 

    private void Update()
    {
        if (collected) return;

        ChaseRunCamera cam = Object.FindFirstObjectByType<ChaseRunCamera>();
        if (cam == null) return;

        bool outOfBounds = false;

        if (moveMode == MoveMode.Falling)
        {
            // Destruir si cae demasiado abajo
            outOfBounds = transform.position.y < cam.GetKillZoneBound() - 3f;
        }
        else if (moveMode == MoveMode.MovingLeft)
        {
            // Destruir si sale por la izquierda de la camara
            outOfBounds = transform.position.x < cam.GetKillZoneBound() - 3f;
        }

        if (outOfBounds)
        {
            spawner?.OnPickupCollected(gameObject);
            Destroy(gameObject);
        }
    }

    // Colision con jugadores 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        ChaseRunPlayerController player = other.GetComponent<ChaseRunPlayerController>();
        if (player == null) return;

        collected = true;
        ApplyEffect(player);

        spawner?.OnPickupCollected(gameObject);
        Destroy(gameObject);
    }

    // Efectos 

    private void ApplyEffect(ChaseRunPlayerController player)
    {
        int pNum = player.PlayerNumber;
        int opNum = pNum == 1 ? 2 : 1;

        switch (type)
        {
            case PowerUpType.ScoreBonus:
                GameManager.Instance?.AddPoints(pNum, 10);
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

    // Coroutines de efectos 

    private System.Collections.IEnumerator SpeedBoostCoroutine(ChaseRunPlayerController player)
    {
        player.ApplySpeedMultiplier(1.6f);
        yield return new WaitForSeconds(effectDuration);
        player.ApplySpeedMultiplier(1f);
    }

    private System.Collections.IEnumerator ShieldCoroutine(ChaseRunPlayerController player)
    {
        player.SetKillZoneImmunity(true);
        yield return new WaitForSeconds(effectDuration);
        player.SetKillZoneImmunity(false);
    }

    private System.Collections.IEnumerator SlowCoroutine(ChaseRunPlayerController player)
    {
        player.ApplySpeedMultiplier(0.5f);
        yield return new WaitForSeconds(effectDuration);
        player.ApplySpeedMultiplier(1f);
    }

    private System.Collections.IEnumerator ExtraJumpCoroutine(ChaseRunPlayerController player)
    {
        player.AddExtraJump();
        yield return new WaitForSeconds(effectDuration);
        player.RemoveExtraJump();
    }
}
