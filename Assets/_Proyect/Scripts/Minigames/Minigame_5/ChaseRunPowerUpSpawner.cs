using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// Fase Y: spawnea fuera del borde superior de la camara.
///         Los power ups tienen Rigidbody2D con gravedad  caen solos.
///         Velocidad de caída configurable.
/// 
/// Fase X: spawnea fuera del borde derecho de la camara.
///         Los power ups se mueven en -X hacia los jugadores.
///         Velocidad horizontal configurable.

/// Coloca este script en un GO vacio en la escena.
/// El prefab de power up debe tener: Rigidbody2D + ChaseRunPowerUpPickup.

public class ChaseRunPowerUpSpawner : MonoBehaviour
{
    //  Configuracion 
    [Header("Prefab")]
    [SerializeField] private GameObject powerUpPrefab;

    [Header("Intervalo de spawn")]
    [SerializeField] private float spawnInterval = 8f;

    [Header("Fase Y — caída")]
    [Tooltip("Velocidad inicial de caida del power up (se suma a la gravedad del Rigidbody2D)")]
    [SerializeField] private float fallSpeed = 3f;

    [Header("Fase X — movimiento horizontal")]
    [Tooltip("Velocidad a la que el power up se mueve hacia la izquierda (valor positivo)")]
    [SerializeField] private float horizontalSpeed = 5f;

    [Tooltip("Cuantos power ups spawnear por oleada en fase X")]
    [SerializeField] private int spawnCountPhaseX = 2;

    // Referencias 
    private ChaseRunCamera chaseCamera;
    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;

    private Coroutine spawnCoroutine;
    private List<GameObject> activePickups = new List<GameObject>();

    

    private void Start()
    {
        chaseCamera = Object.FindFirstObjectByType<ChaseRunCamera>();
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void SetPhase(ChaseRunManager.RunPhase phase)
    {
        currentPhase = phase;

        // Limpiar power ups de la fase anterior
        foreach (var obj in activePickups)
            if (obj != null) Destroy(obj);
        activePickups.Clear();
    }

    //  Loop principal 

    private IEnumerator SpawnLoop()
    {
        // Pequeña espera inicial para que la camara este lista
        yield return new WaitForSeconds(2f);

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!ChaseRunManager.Instance.IsGameRunning()) continue;

            if (currentPhase == ChaseRunManager.RunPhase.PhaseY)
                SpawnPhaseY();
            else
                SpawnPhaseX();
        }
    }

    //  Spawn Fase Y (caen desde arriba)

    private void SpawnPhaseY()
    {
        if (chaseCamera == null || powerUpPrefab == null) return;

        var (xMin, xMax) = chaseCamera.GetHorizontalRange();
        float spawnX = Random.Range(xMin, xMax);
        float spawnY = chaseCamera.GetTopBound();

        Vector3 pos = new Vector3(spawnX, spawnY, 0f);
        GameObject obj = Instantiate(powerUpPrefab, pos, Quaternion.identity);

        // Configurar el pickup para que caiga
        ChaseRunPowerUpPickup pickup = obj.GetComponent<ChaseRunPowerUpPickup>();
        if (pickup != null)
            pickup.SetupFalling(fallSpeed);

        activePickups.Add(obj);
    }

    //  Spawn Fase X (entran por la derecha) 

    private void SpawnPhaseX()
    {
        if (chaseCamera == null || powerUpPrefab == null) return;

        for (int i = 0; i < spawnCountPhaseX; i++)
        {
            var (yMin, yMax) = chaseCamera.GetVerticalRange();
            float spawnY = Random.Range(yMin, yMax);
            float spawnX = chaseCamera.GetRightBound();

            Vector3 pos = new Vector3(spawnX, spawnY, 0f);
            GameObject obj = Instantiate(powerUpPrefab, pos, Quaternion.identity);

            // Configurar el pickup para que se mueva hacia la izquierda
            ChaseRunPowerUpPickup pickup = obj.GetComponent<ChaseRunPowerUpPickup>();
            if (pickup != null)
                pickup.SetupMovingLeft(horizontalSpeed);

            activePickups.Add(obj);
        }
    }

    //  Notificacion cuando un pickup es recogido 

    public void OnPickupCollected(GameObject pickup)
    {
        activePickups.Remove(pickup);
    }
}
