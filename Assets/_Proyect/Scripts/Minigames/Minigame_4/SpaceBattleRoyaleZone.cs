using System.Collections;
using UnityEngine;


/// SpaceBattleRoyaleZone
///
/// SETUP del GameObject:
///   - Un SpriteRenderer con un sprite circular (semi-transparente, borde visible)
///   - Un CircleCollider2D con Is Trigger = true
///   - Este script
///
/// SETUP en el Inspector:
///   - timeBeforeActivation: segundos sin kill para que empiece a achicar
///   - shrinkSpeed:          velocidad de achique (unidades por segundo)
///   - minRadius:            radio mínimo al que puede llegar
///   - damageTickRate:       cada cuántos segundos hace daño a quien esté afuera
///   - El GameObject empieza desactivado (ForceStop lo mantiene así)

[RequireComponent(typeof(CircleCollider2D))]
public class SpaceBattleRoyaleZone : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Segundos sin kill para que aparezca y empiece a achicar")]
    [SerializeField] private float timeBeforeActivation = 30f;

    [Header("Achique")]
    [Tooltip("Radio inicial del círculo cuando aparece")]
    [SerializeField] private float initialRadius = 12f;
    [Tooltip("Velocidad de achique en unidades por segundo")]
    [SerializeField] private float shrinkSpeed = 0.5f;
    [Tooltip("Radio mínimo (no achica más allá de esto)")]
    [SerializeField] private float minRadius = 1.5f;

    [Header("Daño")]
    [Tooltip("Cada cuántos segundos el borde mata a quien esté afuera")]
    [SerializeField] private float damageTickRate = 1.5f;

    [Header("Visuales")]
    [Tooltip("SpriteRenderer del círculo (debe ser circular)")]
    [SerializeField] private SpriteRenderer zoneRenderer;
    [Tooltip("Color del área segura (semi-transparente)")]
    [SerializeField] private Color safeColor = new Color(0.3f, 0.8f, 1f, 0.15f);
    [Tooltip("Color del borde exterior (peligro)")]
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f, 0.8f);

    // Referencias 
    private CircleCollider2D circleCollider;
    private Transform player1;
    private Transform player2;

    // Estado 
    private float currentRadius;
    private float killTimer;
    private bool isActive = false;
    private bool isShrinking = false;

    // Para el daño: timers individuales por jugador
    private float p1DamageTimer = 0f;
    private float p2DamageTimer = 0f;

    private Coroutine activationCoroutine;

    
    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();

        if (zoneRenderer != null)
            zoneRenderer.color = safeColor;
    }

    private void Start()
    {
        // Empieza invisible y desactivado
        SetZoneVisible(false);
    }

    /// Llamado por SpaceEventManager cuando se activa una nueva zona.
    /// Inicia el contador de tiempo antes de que aparezca.
    public void StartZone(Transform p1, Transform p2)
    {
        player1 = p1;
        player2 = p2;

        ForceStop();
        activationCoroutine = StartCoroutine(ActivationCountdown());
    }

    /// Resetea el contador (llamado en cada kill). Si ya está activo/achicando,
    /// detiene todo y vuelve a contar desde 0.
    public void ResetTimer()
    {
        if (!isActive && activationCoroutine == null) return;

        Debug.Log("[BattleRoyale] Kill detectada → Resetando contador y limpiando zona.");

        ForceStop();
        activationCoroutine = StartCoroutine(ActivationCountdown());
    }

    /// Detiene y oculta la zona completamente (cambio de zona, fin de ronda, etc).
    public void ForceStop()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        StopAllCoroutines();

        isActive = false;
        isShrinking = false;
        killTimer = 0f;
        p1DamageTimer = 0f;
        p2DamageTimer = 0f;

        SetZoneVisible(false);
    }

    // Coroutines 

    private IEnumerator ActivationCountdown()
    {
        killTimer = 0f;

        Debug.Log($"[BattleRoyale] Iniciando countdown: {timeBeforeActivation}s sin kill para activar.");

        while (killTimer < timeBeforeActivation)
        {
            killTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[BattleRoyale] ¡Zona Battle Royale ACTIVADA!");
        activationCoroutine = null;
        StartCoroutine(ShrinkLoop());
    }

    private IEnumerator ShrinkLoop()
    {
        isActive = true;
        isShrinking = true;
        currentRadius = initialRadius;

        // Centrar en el centro del mapa (posición del propio GameObject, configurala en Unity)
        SetZoneVisible(true);
        ApplyRadius(currentRadius);

        while (currentRadius > minRadius)
        {
            currentRadius -= shrinkSpeed * Time.deltaTime;
            currentRadius = Mathf.Max(currentRadius, minRadius);
            ApplyRadius(currentRadius);

            CheckPlayersOutside();
            yield return null;
        }

        // Llegó al mínimo: seguir chequeando pero sin achicar más
        while (isActive)
        {
            CheckPlayersOutside();
            yield return null;
        }
    }

    //  Lógica de daño 

    private void CheckPlayersOutside()
    {
        if (player1 != null)
            CheckPlayer(player1, 1, ref p1DamageTimer);

        if (player2 != null)
            CheckPlayer(player2, 2, ref p2DamageTimer);
    }

    private void CheckPlayer(Transform player, int playerIndex, ref float damageTimer)
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > currentRadius)
        {
            // Está fuera de la zona segura  acumular timer de daño
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageTickRate)
            {
                damageTimer = 0f;
                KillPlayer(playerIndex);
            }
        }
        else
        {
            // Dentro de la zona: resetear timer
            damageTimer = 0f;
        }
    }

    private void KillPlayer(int victim)
    {
        int killer = victim == 1 ? 2 : 1;
        Debug.Log($"[BattleRoyale] Jugador {victim} eliminado por la zona. Killer={killer}");
        SpaceMinigame.Instance?.RegisterKill(killer, victim);
        // Nota: RegisterKill ya llama a SpaceEventManager.NotifyKill()  ResetTimer()
        // así que la zona se resetea automáticamente tras la kill.
    }

    // Helpers visuales 

    private void ApplyRadius(float radius)
    {
        // El collider
        circleCollider.radius = radius;

        // El sprite: asumimos que el sprite base tiene 1 unidad de diámetro
        // así que escala = diámetro = radius * 2
        float diameter = radius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void SetZoneVisible(bool visible)
    {
        if (zoneRenderer != null)
            zoneRenderer.enabled = visible;

        circleCollider.enabled = visible;
    }

    //  API pública 

    /// Llama esto desde SpaceEventManager para iniciar la zona con referencias a los jugadores.
    /// Si no tenés las referencias acá, también podés buscarlas con FindObjectOfType.
    public void Initialize(Transform p1, Transform p2)
    {
        player1 = p1;
        player2 = p2;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!isActive) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, currentRadius);
    }
#endif
}
