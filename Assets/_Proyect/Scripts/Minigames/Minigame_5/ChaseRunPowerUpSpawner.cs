using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// ChaseRunPowerUpSpawner
//
// PhaseY: spawna fuera del borde SUPERIOR de la cámara. Los power ups caen
//         por gravedad hacia los jugadores.
//
// PhaseX: spawna fuera del borde DERECHO de la cámara. Los power ups se
//         mueven en -X hacia los jugadores.
//
// Al cambiar de fase limpia todos los power ups activos de la fase anterior.
// ─────────────────────────────────────────────────────────────────────────────

public class ChaseRunPowerUpSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject powerUpPrefab;

    [Header("Intervalo de spawn")]
    [SerializeField] private float spawnInterval = 8f;
    [Tooltip("Espera inicial antes del primer spawn.")]
    [SerializeField] private float initialDelay = 3f;

    [Header("Fase Y — caída")]
    [Tooltip("Velocidad inicial de caída (se suma a la gravedad del Rigidbody2D).")]
    [SerializeField] private float fallSpeed = 3f;

    [Header("Fase X — movimiento horizontal")]
    [Tooltip("Velocidad a la que el power up se mueve hacia la izquierda.")]
    [SerializeField] private float horizontalSpeed = 5f;
    [Tooltip("Cuántos power ups spawnear por oleada en fase X.")]
    [SerializeField] private int spawnCountPhaseX = 2;

    // ── Estado ────────────────────────────────────────────────────────────────

    private ChaseRunCamera chaseCamera;
    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;
    private Coroutine spawnCoroutine;
    private readonly List<GameObject> activePickups = new List<GameObject>();

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Start()
    {
        chaseCamera    = Object.FindFirstObjectByType<ChaseRunCamera>();
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>Llamado por ChaseRunManager al cambiar de fase.</summary>
    public void SetPhase(ChaseRunManager.RunPhase phase)
    {
        currentPhase = phase;

        // Destruir todos los power ups de la fase anterior
        foreach (var obj in activePickups)
            if (obj != null) Destroy(obj);
        activePickups.Clear();
    }

    // ── Loop principal ────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (ChaseRunManager.Instance == null || !ChaseRunManager.Instance.IsGameRunning())
                continue;

            if (powerUpPrefab == null || chaseCamera == null)
                continue;

            if (currentPhase == ChaseRunManager.RunPhase.PhaseY)
                SpawnPhaseY();
            else
                SpawnPhaseX();
        }
    }

    // ── Spawn fase Y (caen desde arriba) ─────────────────────────────────────

    private void SpawnPhaseY()
    {
        var (xMin, xMax) = chaseCamera.GetHorizontalRange();
        float spawnX     = Random.Range(xMin, xMax);
        float spawnY     = chaseCamera.GetTopBound();

        SpawnPickup(new Vector3(spawnX, spawnY, 0f), falling: true);
    }

    // ── Spawn fase X (entran por la derecha moviéndose a la izquierda) ────────

    private void SpawnPhaseX()
    {
        for (int i = 0; i < spawnCountPhaseX; i++)
        {
            var (yMin, yMax) = chaseCamera.GetVerticalRange();
            float spawnY     = Random.Range(yMin, yMax);
            float spawnX     = chaseCamera.GetRightBound();

            SpawnPickup(new Vector3(spawnX, spawnY, 0f), falling: false);
        }
    }

    // ── Helper de instanciación ───────────────────────────────────────────────

    private void SpawnPickup(Vector3 position, bool falling)
    {
        GameObject obj = Instantiate(powerUpPrefab, position, Quaternion.identity);
        ChaseRunPowerUpPickup pickup = obj.GetComponent<ChaseRunPowerUpPickup>();

        if (pickup != null)
        {
            if (falling)
                pickup.SetupFalling(fallSpeed);
            else
                pickup.SetupMovingLeft(horizontalSpeed);
        }

        activePickups.Add(obj);
    }

    // ── Notificación desde pickup ─────────────────────────────────────────────

    /// <summary>Llamado por el pickup al ser recogido o destruido.</summary>
    public void OnPickupCollected(GameObject pickup)
    {
        activePickups.Remove(pickup);
    }
}
