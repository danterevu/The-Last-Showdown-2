using System.Collections;
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

    private bool hasDeposited = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasDeposited) return; // evitar doble disparo

        // Caso 1: Jugador con DNA toca el depósito manualmente
        PlayerControllerDNA controller = collision.GetComponent<PlayerControllerDNA>();
        if (controller != null && controller.HasDNA())
        {
            int playerTag = collision.CompareTag("Player1") ? 1 : collision.CompareTag("Player2") ? 2 : 0;
            if (playerTag != allowedPlayer) return;

            DNA dnaInHand = controller.GetCarriedDNA();
            if (dnaInHand == null) return;

            // Verificar que el DNA no esté siendo lanzado
            if (dnaInHand.IsThrown()) return;

            hasDeposited = true;
            int points = 30;
            GameManager.Instance.AddPoints(allowedPlayer, points);
            OnAnyDeposit?.Invoke();
            SpawnDepositParticles();
            controller.DropDNA();
            dnaInHand.RespawnAfterDelay();
            Debug.Log($"Jugador {allowedPlayer} depositó DNA manualmente — {points} pts");
            StartCoroutine(ResetDepositFlag());
            return;
        }

        // Caso 2: DNA lanzado entra al depósito
        DNA dna = collision.GetComponent<DNA>();
        if (dna != null && dna.IsThrown() && dna.GetHolder() == null)
        {
            int thrower = dna.GetLastThrower();
            if (thrower != allowedPlayer)
            {
                Debug.Log($"DNA lanzado por jugador {thrower} rechazado en depósito de jugador {allowedPlayer}");
                return;
            }

            hasDeposited = true;
            bool throwBonus = ModifierManager.Instance != null &&
                              ModifierManager.Instance.activeDNAModifier == ModifierManager.MutantDNAModifier.ThrowBonus;

            int points = throwBonus ? 50 : 30;
            GameManager.Instance.AddPoints(allowedPlayer, points);
            OnAnyDeposit?.Invoke();
            SpawnDepositParticles();
            dna.RespawnAfterDelay();
            Debug.Log($"DNA lanzado por jugador {thrower} — {points} pts {(throwBonus ? "[ThrowBonus]" : "")}");
            StartCoroutine(ResetDepositFlag());
            return;
        }
    }

    private IEnumerator ResetDepositFlag()
    {
        yield return new WaitForSeconds(0.5f);
        hasDeposited = false;
    }

    private void SpawnDepositParticles()
    {
        if (depositParticlesPrefab == null) return;
        Vector3 spawnPos = transform.position + particlesOffset;
        Instantiate(depositParticlesPrefab, spawnPos, Quaternion.identity);
    }
}