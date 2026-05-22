
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private HolographicPlatforms gameManager;

    // llamado desde HolographicPlatforms para ignorar triggers temporalmente
    public bool ignoring = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ignoring) return; // ignorar mientras se respawnea

        if (other.CompareTag("Player1"))
            gameManager.OnPlayerFell(1);
        else if (other.CompareTag("Player2"))
            gameManager.OnPlayerFell(2);
    }
}