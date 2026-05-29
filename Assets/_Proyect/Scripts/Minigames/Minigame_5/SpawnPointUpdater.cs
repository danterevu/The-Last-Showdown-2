using UnityEngine;



public class SpawnPointUpdater : MonoBehaviour
{
    [SerializeField] private ChaseRunPlayerController player1;
    [SerializeField] private ChaseRunPlayerController player2;
    [SerializeField] private ChaseRunCamera chaseCamera;

    [Tooltip("Cuántas unidades delante de la kill zone está el spawn point (en el eje activo).")]
    [SerializeField] private float offsetFromKillZone = 2.5f;

    [Tooltip("Separación lateral entre los dos jugadores al spawnear.")]
    [SerializeField] private float lateralSeparation = 0.8f;

    private void LateUpdate()
    {
        if (chaseCamera == null) return;

        Vector3 p1Spawn, p2Spawn;

        if (chaseCamera.CurrentPhase == ChaseRunManager.RunPhase.PhaseY)
        {
            // Kill zone es el borde inferior en Y → spawn un poco arriba de ese borde
            float safeY = chaseCamera.GetKillZoneBound() + offsetFromKillZone;
            float camX = chaseCamera.CenterX;

            p1Spawn = new Vector3(camX - lateralSeparation, safeY, 0f);
            p2Spawn = new Vector3(camX + lateralSeparation, safeY, 0f);
        }
        else
        {
            // Kill zone es el borde izquierdo en X → spawn un poco a la derecha de ese borde
            float safeX = chaseCamera.GetKillZoneBound() + offsetFromKillZone;
            float camY = chaseCamera.CenterY;

            p1Spawn = new Vector3(safeX, camY + lateralSeparation, 0f);
            p2Spawn = new Vector3(safeX, camY - lateralSeparation, 0f);
        }

        player1?.SetSpawnPoint(p1Spawn);
        player2?.SetSpawnPoint(p2Spawn);
    }
}
