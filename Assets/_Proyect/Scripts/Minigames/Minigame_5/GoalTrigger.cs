using UnityEngine;



public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        ChaseRunPlayerController player = other.GetComponent<ChaseRunPlayerController>();
        if (player != null)
            ChaseRunManager.Instance?.PlayerReachedGoal(player.PlayerNumber);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var col = GetComponent<UnityEngine.Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif
}
