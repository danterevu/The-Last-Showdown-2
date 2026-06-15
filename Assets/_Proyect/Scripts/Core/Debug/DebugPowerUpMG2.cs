using UnityEngine;

public class DebugPowerUpMG2 : MonoBehaviour
{
    // se autocompletan buscando los controllers en Start
    private PlatformPlayerController p1;
    private PlatformPlayerController p2;

    private void Start()
    {
        var all = FindObjectsByType<PlatformPlayerController>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c.CompareTag("Player1")) p1 = c;
            else if (c.CompareTag("Player2")) p2 = c;
        }
    }

    private void Update()
    {
        if (!DebugManager.IsDebugMode) return;

        // player 1 — teclas numéricas
        // if (Input.GetKeyDown(KeyCode.Alpha1)) Give(p1, PowerUpPickup.PowerUpType.Cage);
        // if (Input.GetKeyDown(KeyCode.Alpha2)) Give(p1, PowerUpPickup.PowerUpType.Shield);
        if (Input.GetKeyDown(KeyCode.Alpha1)) Give(p1, PowerUpPickup.PowerUpType.Crusher);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Give(p1, PowerUpPickup.PowerUpType.Hook);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Give(p1, PowerUpPickup.PowerUpType.HeavyGravity);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Give(p1, PowerUpPickup.PowerUpType.MirrorControl);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Give(p1, PowerUpPickup.PowerUpType.Jetpack);

        // player 2 — numpad
        // if (Input.GetKeyDown(KeyCode.Alpha7)) Give(p2, PowerUpPickup.PowerUpType.Cage);
        // if (Input.GetKeyDown(KeyCode.Alpha8)) Give(p2, PowerUpPickup.PowerUpType.Shield);
        if (Input.GetKeyDown(KeyCode.Alpha6)) Give(p2, PowerUpPickup.PowerUpType.Crusher);
        if (Input.GetKeyDown(KeyCode.Alpha7)) Give(p2, PowerUpPickup.PowerUpType.Hook);
        if (Input.GetKeyDown(KeyCode.Alpha8)) Give(p2, PowerUpPickup.PowerUpType.HeavyGravity);
        if (Input.GetKeyDown(KeyCode.Keypad9)) Give(p2, PowerUpPickup.PowerUpType.MirrorControl);
        if (Input.GetKeyDown(KeyCode.Keypad0)) Give(p2, PowerUpPickup.PowerUpType.Jetpack);
    }

    private void Give(PlatformPlayerController controller, PowerUpPickup.PowerUpType type)
    {
        if (controller == null) return;
        if (controller.HasPowerUp()) return; // no sobreescribir si ya tiene uno
        controller.ReceivePowerUp(type);
        Debug.Log($"[Debug] {controller.tag} recibió: {type}");
    }
}