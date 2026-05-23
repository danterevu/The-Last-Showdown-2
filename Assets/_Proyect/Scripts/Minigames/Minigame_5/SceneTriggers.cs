using UnityEngine;


// TriggerZone
// Colocalo en un GO con Collider2D (IsTrigger = true) en el punto del nivel
// donde la camara/runner debe cambiar de Fase Y => Fase X.
// El tag del GO debe ser "PhaseChangeTrigger" o simplemente asignar el layer
// correcto, lo que sea mas cómodo; aca lo hacemos por tag.

public class TriggerZone : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        // El runner de la camara tiene el tag "CameraRunner"
        if (!other.CompareTag("CameraRunner")) return;

        triggered = true;
        ChaseRunManager.Instance?.TriggerPhaseChange();
    }
}



// GoalTrigger
// Colocalo en un GO con Collider2D (IsTrigger = true) al final del recorrido.
// Tag del GO: "Goal" (los jugadores lo detectan en OnTriggerEnter2D propio,
// pero este componente tambien lo maneja como fallback).

public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        ChaseRunPlayerController player = other.GetComponent<ChaseRunPlayerController>();
        if (player != null)
            ChaseRunManager.Instance?.PlayerReachedGoal(player.PlayerNumber);
    }
}



// SpawnPointUpdater
// Actualiza el spawn point de los jugadores conforme avanza la camara,
// para que siempre respawneen en un lugar seguro (dentro de la vista).
// Colocalo en un GO vacio, hijo de la camara o independiente.
// Asignale los dos controladores de jugador.

public class SpawnPointUpdater : MonoBehaviour
{
    [SerializeField] private ChaseRunPlayerController player1;
    [SerializeField] private ChaseRunPlayerController player2;
    [SerializeField] private ChaseRunCamera chaseCamera;

    [Tooltip("Offset desde el borde trasero de la cámara donde se pone el spawn (unidades world)")]
    [SerializeField] private float offsetFromKillZone = 2f;

    [Tooltip("Altura/X fija del spawn respecto al centro de la camara")]
    [SerializeField] private float spawnHeightOffset = -1f; // un poco abajo del centro

    private void LateUpdate()
    {
        if (chaseCamera == null) return;

        Vector3 camPos = chaseCamera.transform.GetComponent<Camera>() != null
            ? chaseCamera.transform.position
            : Vector3.zero;

        Vector3 p1Spawn, p2Spawn;

        if (chaseCamera.CurrentPhase == ChaseRunManager.RunPhase.PhaseY)
        {
            float safeY = chaseCamera.GetKillZoneBound() + offsetFromKillZone;
            p1Spawn = new Vector3(camPos.x - 1f, safeY, 0f);
            p2Spawn = new Vector3(camPos.x + 1f, safeY, 0f);
        }
        else
        {
            float safeX = chaseCamera.GetKillZoneBound() + offsetFromKillZone;
            p1Spawn = new Vector3(safeX, camPos.y + spawnHeightOffset - 0.5f, 0f);
            p2Spawn = new Vector3(safeX, camPos.y + spawnHeightOffset + 0.5f, 0f);
        }

        player1?.SetSpawnPoint(p1Spawn);
        player2?.SetSpawnPoint(p2Spawn);
    }
}
