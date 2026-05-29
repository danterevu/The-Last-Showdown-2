using UnityEngine;



public class TriggerZone : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("CameraRunner")) return;

        triggered = true;
        ChaseRunManager.Instance?.TriggerPhaseChange();
    }

#if UNITY_EDITOR
    // Visualización en editor para facilitar el posicionamiento
    private void OnDrawGizmos()
    {
        var col = GetComponent<UnityEngine.Collider2D>();
        if (col == null) return;

        Gizmos.color = triggered
            ? new Color(1f, 0f, 0f, 0.3f)
            : new Color(1f, 1f, 0f, 0.3f);

        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = triggered ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif
}
