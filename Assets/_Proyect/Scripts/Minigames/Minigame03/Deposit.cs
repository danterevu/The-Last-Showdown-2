using UnityEngine;

public class Deposit : MonoBehaviour
{
    [SerializeField] private int allowedPlayer; // 1 o 2
    public static event System.Action OnAnyDeposit;

    [Header("Partículas")]
    [Tooltip("Prefab de partículas que se instancia al depositar el DNA (arrastrá tu prefab acá)")]
    [SerializeField] private GameObject depositParticlesPrefab;
    [Tooltip("Offset de posición donde aparecen las partículas respecto al depósito")]
    [SerializeField] private Vector3 particlesOffset = Vector3.zero;

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
                SpawnDepositParticles();
                controller.DropDNA();
                dnaInHand.RespawnAfterDelay();
                Debug.Log($"Jugador {allowedPlayer} depositó DNA. Velocidad restaurada.");
            }
            return;
        }

        // Caso 2: DNA lanzado entra al depósito
        DNA dna = collision.GetComponent<DNA>();
        if (dna != null && dna.IsThrown() && dna.GetHolder() == null)
        {
            int thrower = dna.GetLastThrower();
            if (thrower == allowedPlayer)
            {
                GameManager.Instance.AddPoints(allowedPlayer, 25);
                OnAnyDeposit?.Invoke();
                SpawnDepositParticles();
                dna.RespawnAfterDelay();
                Debug.Log($"DNA lanzado depositado correctamente por el jugador {thrower} en su depósito.");
            }
            else
            {
                Debug.Log($"DNA lanzado por jugador {thrower} intentó depositar en depósito del jugador {allowedPlayer} -> rechazado.");
            }
            return;
        }
    }

    private void SpawnDepositParticles()
    {
        if (depositParticlesPrefab == null) return;
        Vector3 spawnPos = transform.position + particlesOffset;
        Instantiate(depositParticlesPrefab, spawnPos, Quaternion.identity);
    }
}