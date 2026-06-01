using UnityEngine;

public class Deposit : MonoBehaviour
{
    [SerializeField] private int allowedPlayer; // 1 o 2
    public static event System.Action OnAnyDeposit;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Caso 1: Jugador con DNA toca el depósito
        PlayerControllerDNA controller = collision.GetComponent<PlayerControllerDNA>();
        if (controller != null && controller.HasDNA())
        {
            int playerTag = collision.CompareTag("Player1") ? 1 : collision.CompareTag("Player2") ? 2 : 0;
            if (playerTag != allowedPlayer) return;

            DNA dnaInHand = controller.GetCarriedDNA();
            if (dnaInHand != null)
            {
                GameManager.Instance.AddPoints(allowedPlayer, 50);
                OnAnyDeposit?.Invoke();

                // Primero soltar el DNA (limpia estado del jugador)
                controller.DropDNA();

                // Luego respawnear el DNA
                dnaInHand.RespawnAfterDelay();

                Debug.Log($"Jugador {allowedPlayer} depositó DNA. Velocidad restaurada.");
            }
            return;
        }

        // Caso 2: DNA lanzado entra al depósito
        DNA dna = collision.GetComponent<DNA>();
        if (dna != null && dna.IsThrown() && dna.GetHolder() == null)
        {
            GameManager.Instance.AddPoints(allowedPlayer, 50);
            OnAnyDeposit?.Invoke();
            dna.RespawnAfterDelay();
            Debug.Log($"DNA lanzado depositado en depósito {allowedPlayer}");
        }
    }
}