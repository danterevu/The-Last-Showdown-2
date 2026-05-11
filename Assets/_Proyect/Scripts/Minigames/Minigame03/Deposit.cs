using UnityEngine;

public class Deposit : MonoBehaviour
{
    [SerializeField] private int allowedPlayer; // 1 o 2, se setea en el Inspector
    [SerializeField] private DNA dna; //GameObject del DNA acá

    public static event System.Action OnAnyDeposit;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerControllerDNA controller = collision.GetComponent<PlayerControllerDNA>();
        if (controller == null || !controller.HasDNA()) return;

        // Verificar que sea el jugador correcto
        int playerTag = collision.CompareTag("Player1") ? 1 : collision.CompareTag("Player2") ? 2 : 0;
        if (playerTag != allowedPlayer) return;

        // Sumar puntos usando AddPoints para que respete multiplicadores
        GameManager.Instance.AddPoints(allowedPlayer, 50);
        OnAnyDeposit?.Invoke(); // avisar a todos los botones 
        controller.DropDNA();

        Debug.Log($"Jugador {allowedPlayer} depositó DNA");
        dna.RespawnAfterDelay(); //llama a la funcion de dna
    }
}