using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        ChaseRunPlayerController player = other.GetComponent<ChaseRunPlayerController>();
        if (player != null)
            ChaseRunManager.Instance?.PlayerReachedGoal(player.PlayerNumber);
    }
}
