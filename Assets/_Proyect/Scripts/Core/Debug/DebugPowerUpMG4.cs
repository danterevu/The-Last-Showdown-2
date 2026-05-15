using UnityEngine;

public class DebugPowerUpMG5 : MonoBehaviour
{
    private PowerUpHolder p1Holder;
    private PowerUpHolder p2Holder;

    private void Start()
    {
        var all = FindObjectsByType<PowerUpHolder>(FindObjectsSortMode.None);
        foreach (var h in all)
        {
            if (h.CompareTag("Player1")) p1Holder = h;
            else if (h.CompareTag("Player2")) p2Holder = h;
        }
    }

    private void Update()
    {
        if (!DebugManager.IsDebugMode) return;

        // player 1 — teclas numéricas
        if (Input.GetKeyDown(KeyCode.Alpha1)) Give(p1Holder, SpacePowerUpType.SlowGrande);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Give(p1Holder, SpacePowerUpType.RocketSabotage);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Give(p1Holder, SpacePowerUpType.MeteorStrike);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Give(p1Holder, SpacePowerUpType.HomingMissile);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Give(p1Holder, SpacePowerUpType.Repulsion);

        // player 2 — numpad
        if (Input.GetKeyDown(KeyCode.Alpha6)) Give(p2Holder, SpacePowerUpType.SlowGrande);
        if (Input.GetKeyDown(KeyCode.Alpha7)) Give(p2Holder, SpacePowerUpType.RocketSabotage);
        if (Input.GetKeyDown(KeyCode.Alpha8)) Give(p2Holder, SpacePowerUpType.MeteorStrike);
        if (Input.GetKeyDown(KeyCode.Alpha9)) Give(p2Holder, SpacePowerUpType.HomingMissile);
        if (Input.GetKeyDown(KeyCode.Alpha0)) Give(p2Holder, SpacePowerUpType.Repulsion);
    }

    private void Give(PowerUpHolder holder, SpacePowerUpType type)
    {
        if (holder == null || holder.HasPowerUp) return;
        holder.ReceivePowerUp(type);
        Debug.Log($"[Debug] {holder.tag} recibió: {type}");
    }
}