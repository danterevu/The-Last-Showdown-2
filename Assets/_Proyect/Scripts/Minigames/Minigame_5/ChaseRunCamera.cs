using UnityEngine;



// Sigue un GO "runner" que avanza automaticamente.
// Fase Y  el runner sube (+Y). Fase X  el runner va a la derecha (+X).
// La kill zone es el borde trasero de la camara (abajo en Y, izquierda en X).

[RequireComponent(typeof(Camera))]
public class ChaseRunCamera : MonoBehaviour
{
    // Runner (GO vacío que avanza solo) 
    [Header("Runner")]
    [Tooltip("GO vacio hijo de la cámara o independiente que marca el centro objetivo")]
    [SerializeField] private Transform runner;

    [Header("Velocidades del runner")]
    [SerializeField] private float runnerSpeedY = 3f;   // velocidad al subir
    [SerializeField] private float runnerSpeedX = 4f;   // velocidad al ir derecha

    // Seguimiento suavizado 
    [Header("Suavizado de la cámara")]
    [SerializeField] private float lerpSpeed = 5f;

    //  Kill Zone 
    [Header("Kill Zone")]
    [Tooltip("Qué tan atrás del borde de la cámara está la kill zone (unidades world)")]
    [SerializeField] private float killZoneOffset = 1f;

    //  Estado 
    private ChaseRunManager.RunPhase currentPhase = ChaseRunManager.RunPhase.PhaseY;
    private Camera cam;

    // posicion inicial del runner para resetear si es necesario
    private Vector3 runnerStartPos;

   

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (runner != null) runnerStartPos = runner.position;
    }

    public void SetPhase(ChaseRunManager.RunPhase phase)
    {
        currentPhase = phase;
    }

    private void LateUpdate()
    {
        if (runner == null) return;

        // 1. Mover el runner 
        float speed = currentPhase == ChaseRunManager.RunPhase.PhaseY ? runnerSpeedY : runnerSpeedX;
        Vector3 dir = currentPhase == ChaseRunManager.RunPhase.PhaseY ? Vector3.up : Vector3.right;
        runner.position += dir * speed * Time.deltaTime;

        // 2. La camara sigue el runner (solo en el eje activo) 
        Vector3 targetPos = transform.position;
        if (currentPhase == ChaseRunManager.RunPhase.PhaseY)
            targetPos.y = Mathf.Lerp(transform.position.y, runner.position.y, lerpSpeed * Time.deltaTime);
        else
            targetPos.x = Mathf.Lerp(transform.position.x, runner.position.x, lerpSpeed * Time.deltaTime);

        transform.position = targetPos;
    }

    // Kill Zone: posicion del borde trasero de la camara 
   
    // Retorna la posicion Y del borde inferior de la camara (fase Y)
    // o la posicion X del borde izquierdo (fase X), con el offset aplicado.
    
    public float GetKillZoneBound()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        if (currentPhase == ChaseRunManager.RunPhase.PhaseY)
            return transform.position.y - halfHeight - killZoneOffset;
        else
            return transform.position.x - halfWidth - killZoneOffset;
    }

    public ChaseRunManager.RunPhase CurrentPhase => currentPhase;

    // Helpers para el spawner de power ups 
   
    // Posicion justo FUERA del borde superior de la camara (fase Y).
    
    public float GetTopBound() => transform.position.y + cam.orthographicSize + 1f;

    
    // Posicion justo FUERA del borde derecho de la camara (fase X).
    
    public float GetRightBound() => transform.position.x + cam.orthographicSize * cam.aspect + 1f;

    
    // Rango horizontal visible (para spawnear power ups en X aleatorio dentro de pantalla).
   
    public (float min, float max) GetHorizontalRange()
    {
        float halfWidth = cam.orthographicSize * cam.aspect;
        return (transform.position.x - halfWidth + 1f, transform.position.x + halfWidth - 1f);
    }

   
    // Rango vertical visible (para spawnear power ups en Y aleatorio dentro de pantalla, fase X).
    
    public (float min, float max) GetVerticalRange()
    {
        float halfHeight = cam.orthographicSize;
        return (transform.position.y - halfHeight + 1f, transform.position.y + halfHeight - 1f);
    }
}
