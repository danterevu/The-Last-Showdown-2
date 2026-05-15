using UnityEngine;

public class DebugPowerUpMG1 : MonoBehaviour
{
    [SerializeField] private PowerUpManager powerUpManager;

    private void Update()
    {
        if (!DebugManager.IsDebugMode) return;

        // teclas numéricas  player 1
        if (Input.GetKeyDown(KeyCode.Alpha1)) Give(1, PowerUpManager.PowerUpType.Swap);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Give(1, PowerUpManager.PowerUpType.Freeze);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Give(1, PowerUpManager.PowerUpType.Wall);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Give(1, PowerUpManager.PowerUpType.Magnet);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Give(1, PowerUpManager.PowerUpType.InvertControls);

        // numpad  player 2
        if (Input.GetKeyDown(KeyCode.Alpha6)) Give(2, PowerUpManager.PowerUpType.Swap);
        if (Input.GetKeyDown(KeyCode.Alpha7)) Give(2, PowerUpManager.PowerUpType.Freeze);
        if (Input.GetKeyDown(KeyCode.Alpha8)) Give(2, PowerUpManager.PowerUpType.Wall);
        if (Input.GetKeyDown(KeyCode.Alpha9)) Give(2, PowerUpManager.PowerUpType.Magnet);
        if (Input.GetKeyDown(KeyCode.Alpha0)) Give(2, PowerUpManager.PowerUpType.InvertControls);
    }

    private void Give(int player, PowerUpManager.PowerUpType type)
    {
        powerUpManager.DebugGivePowerUp(player, type);
        Debug.Log($"[Debug] P{player} recibió: {type}");
    }
}