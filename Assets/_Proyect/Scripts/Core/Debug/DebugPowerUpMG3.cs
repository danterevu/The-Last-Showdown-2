using UnityEngine;



public class DebugPowerUpMG3 : MonoBehaviour
{
    private PlayerControllerDNA p1;
    private PlayerControllerDNA p2;

    private void Start()
    {
        var all = FindObjectsByType<PlayerControllerDNA>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c.CompareTag("Player1")) p1 = c;
            else if (c.CompareTag("Player2")) p2 = c;
        }
    }

    private void Update()
    {
        if (!DebugManager.IsDebugMode) return;

        // Player 1  (teclas numericas) 
        if (Input.GetKeyDown(KeyCode.Alpha1)) Give(p1, DNAPowerUpPickup.DNAPowerUpType.Shrink);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Give(p1, DNAPowerUpPickup.DNAPowerUpType.Mine);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Give(p1, DNAPowerUpPickup.DNAPowerUpType.RemoteControl);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Give(p1, DNAPowerUpPickup.DNAPowerUpType.Berserk);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Give(p1, DNAPowerUpPickup.DNAPowerUpType.SlimeShot);
        if (Input.GetKeyDown(KeyCode.Space)) Give(p1, DNAPowerUpPickup.DNAPowerUpType.Shield);

        //  Player 2  (teclas 6-7) 
        if (Input.GetKeyDown(KeyCode.Alpha6)) Give(p2, DNAPowerUpPickup.DNAPowerUpType.Shrink);
        if (Input.GetKeyDown(KeyCode.Alpha7)) Give(p2, DNAPowerUpPickup.DNAPowerUpType.Mine);
        if (Input.GetKeyDown(KeyCode.Alpha8)) Give(p2, DNAPowerUpPickup.DNAPowerUpType.RemoteControl);
        if (Input.GetKeyDown(KeyCode.Alpha9)) Give(p2, DNAPowerUpPickup.DNAPowerUpType.Berserk);
        if (Input.GetKeyDown(KeyCode.Alpha0)) Give(p2, DNAPowerUpPickup.DNAPowerUpType.SlimeShot);
        if (Input.GetKeyDown(KeyCode.Return)) Give(p2, DNAPowerUpPickup.DNAPowerUpType.Shield);
    }

    private void Give(PlayerControllerDNA controller, DNAPowerUpPickup.DNAPowerUpType type)
    {
        if (controller == null) return;
        if (controller.HasPowerUp()) return;   // no sobreescribir si ya tiene uno
        controller.ReceiveDNAPowerUp(type);
        Debug.Log($"[Debug] {controller.tag} recibió: {type}");
    }
}