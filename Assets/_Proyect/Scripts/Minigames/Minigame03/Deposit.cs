using UnityEngine;

public class Deposit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlatformPlayerController controller = collision.GetComponent<PlatformPlayerController>();

        if (controller != null && controller.HasDNA())
        {
            if (collision.CompareTag("Player1"))
            {
                GameManager.Instance.player1Score += 50; // problemas en el puntaje
                Debug.Log("Player 1 depositó DNA");
            }
            else if (collision.CompareTag("Player2"))
            {
                GameManager.Instance.player2Score += 50;
                Debug.Log("Player 2 depositó DNA");
            }

            controller.DropDNA(); // pierde el DNA
        }
    }
}
