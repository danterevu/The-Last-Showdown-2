using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Sprites de cada Power Up")]
    [SerializeField] private Sprite spriteCage;
    [SerializeField] private Sprite spriteShield;
    [SerializeField] private Sprite spriteHook;
    [SerializeField] private Sprite spriteHeavyGravity;
    [SerializeField] private Sprite spriteMirrorControl;
    [SerializeField] private Sprite spriteJetpack;
    [SerializeField] private Sprite spriteEmpty;

    private PlatformPlayerController trackedPlayer;

    private void Awake()
    {
        ShowEmpty();
    }

    void Update()
    {
        if (trackedPlayer != null)
            Refresh();
    }

    public void TrackPlayer(PlatformPlayerController player)
    {
        trackedPlayer = player;
        Debug.Log($"HUD {gameObject.name} trackeando a {player.gameObject.name}");
    }

    public void ManualUpdate(bool hasPowerUp, PowerUpPickup.PowerUpType type)
    {
        if (hasPowerUp) Show(type);
        else ShowEmpty();
    }

    private void Refresh()
    {
        if (trackedPlayer.HasPowerUp())
            Show(trackedPlayer.GetCurrentPowerUp());
        else
            ShowEmpty();
    }

    private void Show(PowerUpPickup.PowerUpType type)
    {
        if (iconImage != null) iconImage.sprite = GetSprite(type);
    }

    private void ShowEmpty()
    {
        if (iconImage != null) iconImage.sprite = spriteEmpty;
    }

    private Sprite GetSprite(PowerUpPickup.PowerUpType type)
    {
        switch (type)
        {
            
            case PowerUpPickup.PowerUpType.Hook: return spriteHook;
            case PowerUpPickup.PowerUpType.HeavyGravity: return spriteHeavyGravity;
            case PowerUpPickup.PowerUpType.MirrorControl: return spriteMirrorControl;
            case PowerUpPickup.PowerUpType.Jetpack: return spriteJetpack;
            default: return spriteEmpty;
        }
    }
}