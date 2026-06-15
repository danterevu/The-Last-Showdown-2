using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class SpaceBattleRoyaleZone : MonoBehaviour
{
    [Header("Activacion")]
    [Tooltip("Segundos sin kill para que aparezca y empiece a achicar")]
    [SerializeField] private float timeBeforeActivation = 30f;

    [Header("Achique")]
    [Tooltip("Radio inicial del circulo cuando aparece")]
    [SerializeField] private float initialRadius = 12f;
    [Tooltip("Velocidad de achique en unidades por segundo")]
    [SerializeField] private float shrinkSpeed = 0.5f;
    [Tooltip("Radio minimo (no achica mas alla de esto)")]
    [SerializeField] private float minRadius = 1.5f;

    [Header("Daño")]
    [Tooltip("Cada cuantos segundos mata a quien este afuera")]
    [SerializeField] private float damageTickRate = 1.5f;

    [Header("Visuales")]
    [Tooltip("SpriteRenderer del circulo")]
    [SerializeField] private SpriteRenderer zoneRenderer;
    [SerializeField] private Color safeColor = new Color(0.3f, 0.8f, 1f, 0.15f);

    private CircleCollider2D circleCollider;
    private Transform player1;
    private Transform player2;

    private float currentRadius;
    private float killTimer;
    private bool isActive = false;

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
        // Buscar jugadores por tag automaticamente
        GameObject p1Obj = GameObject.FindWithTag("Player1");
        GameObject p2Obj = GameObject.FindWithTag("Player2");

        if (p1Obj != null) player1 = p1Obj.transform;
        else Debug.LogWarning("[BattleRoyale] No se encontro GameObject con tag 'Player1'.");

        if (p2Obj != null) player2 = p2Obj.transform;
        else Debug.LogWarning("[BattleRoyale] No se encontro GameObject con tag 'Player2'.");

        SetZoneVisible(false);
    }

    // ── API publica ───────────────────────────────────────────────────────────

    /// Mueve el circulo al centro de la zona activa. Llamado por SpaceEventManager.
    public void SetZoneCenter(Vector3 center)
    {
        transform.position = new Vector3(center.x, center.y, transform.position.z);
        Debug.Log($"[BattleRoyale] Centro de zona establecido en {center}");
    }

    /// Arranca el countdown. Llamado por SpaceEventManager.SetActiveZone().
    public void StartCountdown()
    {
        ForceStop();
        activationCoroutine = StartCoroutine(ActivationCountdown());
        Debug.Log($"[BattleRoyale] Countdown iniciado: {timeBeforeActivation}s sin kill para activar.");
    }

    /// Resetea el contador tras una kill. Llamado por SpaceEventManager.NotifyKill().
    public void ResetTimer()
    {
        if (activationCoroutine == null && !isActive) return;

        Debug.Log("[BattleRoyale] Kill detectada -> Resetando contador y limpiando zona.");
        ForceStop();
        activationCoroutine = StartCoroutine(ActivationCountdown());
    }

    /// Detiene y oculta todo. Llamado al cambiar de zona o fin de ronda.
    public void ForceStop()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        StopAllCoroutines();

        isActive = false;
        killTimer = 0f;
        p1DamageTimer = 0f;
        p2DamageTimer = 0f;

        SetZoneVisible(false);
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator ActivationCountdown()
    {
        killTimer = 0f;

        while (killTimer < timeBeforeActivation)
        {
            killTimer += Time.deltaTime;
            yield return null;
        }

        activationCoroutine = null;
        Debug.Log("[BattleRoyale] Zona Battle Royale ACTIVADA!");
        StartCoroutine(ShrinkLoop());
    }

    private IEnumerator ShrinkLoop()
    {
        isActive = true;
        currentRadius = initialRadius;
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

        // Llego al minimo: seguir chequeando sin achicar mas
        while (isActive)
        {
            CheckPlayersOutside();
            yield return null;
        }
    }

    // ── Logica de daño ────────────────────────────────────────────────────────

    private void CheckPlayersOutside()
    {
        if (player1 != null) CheckPlayer(player1, 1, ref p1DamageTimer);
        if (player2 != null) CheckPlayer(player2, 2, ref p2DamageTimer);
    }

    private void CheckPlayer(Transform player, int playerIndex, ref float damageTimer)
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > currentRadius)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageTickRate)
            {
                damageTimer = 0f;
                KillPlayer(playerIndex);
            }
        }
        else
        {
            damageTimer = 0f;
        }
    }

    private void KillPlayer(int victim)
    {
        int killer = victim == 1 ? 2 : 1;
        Debug.Log($"[BattleRoyale] Jugador {victim} eliminado por la zona. Punto para Jugador {killer}.");
        SpaceMinigame.Instance?.RegisterKill(killer, victim);
        // RegisterKill -> NotifyKill -> ResetTimer: la zona se reinicia sola tras la kill
    }

    // ── Helpers visuales ─────────────────────────────────────────────────────

    private void ApplyRadius(float radius)
    {
        circleCollider.radius = radius;
        float diameter = radius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void SetZoneVisible(bool visible)
    {
        if (zoneRenderer != null)
            zoneRenderer.enabled = visible;
        circleCollider.enabled = visible;
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