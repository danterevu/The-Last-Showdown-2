using UnityEngine;



[RequireComponent(typeof(Camera))]
public class ChaseRunCamera : MonoBehaviour
{
    [Header("Runner")]
    [Tooltip("GameObject vacío que avanza automáticamente. La cámara lo persigue.")]
    [SerializeField] private Transform runner;

    [Header("Velocidades del runner")]
    [SerializeField] private float runnerSpeedY = 3f;
    [SerializeField] private float runnerSpeedX = 4f;

    [Header("Suavizado de cámara")]
    [Tooltip("Qué tan rápido la cámara alcanza al runner. Mayor = más pegada.")]
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Kill Zone")]
    [Tooltip("Cuántas unidades DETRÁS del borde de cámara está la kill zone.")]
    [SerializeField] private float killZoneOffset = 1f;

    //Estado 

    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;
    private Camera cam;

    // Ciclo de vida 

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetPhase(ChaseRunManager.RunPhase phase)
    {
        currentPhase = phase;
    }

    private void LateUpdate()
    {
        if (runner == null) return;

        // 1. Mover el runner en el eje activo
        float speed = currentPhase == ChaseRunManager.RunPhase.PhaseY ? runnerSpeedY : runnerSpeedX;
        Vector3 direction = currentPhase == ChaseRunManager.RunPhase.PhaseY ? Vector3.up : Vector3.right;
        runner.position += direction * speed * Time.deltaTime;

        // 2. Cámara sigue al runner con lerp, solo en el eje activo
        Vector3 target = transform.position;

        if (currentPhase == ChaseRunManager.RunPhase.PhaseY)
            target.y = Mathf.Lerp(transform.position.y, runner.position.y, lerpSpeed * Time.deltaTime);
        else
            target.x = Mathf.Lerp(transform.position.x, runner.position.x, lerpSpeed * Time.deltaTime);

        transform.position = target;
    }

    // Kill Zone

    
    public float GetKillZoneBound()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        if (currentPhase == ChaseRunManager.RunPhase.PhaseY)
            return transform.position.y - halfHeight - killZoneOffset;
        else
            return transform.position.x - halfWidth - killZoneOffset;
    }

    // Helpers para SpawnPointUpdater 

    /// Posición X del centro de la cámara.
    public float CenterX => transform.position.x;
    /// Posición Y del centro de la cámara.
    public float CenterY => transform.position.y;

    //Helpers para el spawner de power ups 

    /// Justo fuera del borde SUPERIOR de la cámara (spawn power ups fase Y).
    public float GetTopBound() => transform.position.y + cam.orthographicSize + 1f;

    /// Justo fuera del borde DERECHO de la cámara (spawn power ups fase X).
    public float GetRightBound() => transform.position.x + cam.orthographicSize * cam.aspect + 1f;

    /// Rango horizontal visible (para elegir X aleatoria en fase Y).
    public (float min, float max) GetHorizontalRange()
    {
        float hw = cam.orthographicSize * cam.aspect;
        return (transform.position.x - hw + 1f, transform.position.x + hw - 1f);
    }

    /// Rango vertical visible (para elegir Y aleatoria en fase X).
    public (float min, float max) GetVerticalRange()
    {
        float hh = cam.orthographicSize;
        return (transform.position.y - hh + 1f, transform.position.y + hh - 1f);
    }

    // Propiedades públicas

    public ChaseRunManager.RunPhase CurrentPhase => currentPhase;

   
    public Transform Runner => runner;
}
