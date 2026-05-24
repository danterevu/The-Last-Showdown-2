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
}
