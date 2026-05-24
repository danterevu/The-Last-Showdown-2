using UnityEngine;

public class SpawnPointUpdater : MonoBehaviour
{
    [SerializeField] private ChaseRunPlayerController player1;
    [SerializeField] private ChaseRunPlayerController player2;
    [SerializeField] private ChaseRunCamera chaseCamera;

    [Tooltip("Qué tan lejos del borde trasero de la cámara está el spawn")]
    [SerializeField] private float offsetFromKillZone = 2f;

    [SerializeField] private float spawnHeightOffset = -1f;

    private void LateUpdate()
    {
        if (chaseCamera == null) return;

        Vector3 camPos = chaseCamera.transform.position;
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
