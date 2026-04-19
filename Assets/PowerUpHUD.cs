using TMPro;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class PowerUpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI powerUpNameText;

    [Header("Sprites de cada Power Up")]
    [SerializeField] private Sprite spriteCage;
    [SerializeField] private Sprite spriteShield;
    [SerializeField] private Sprite spriteHook;
    [SerializeField] private Sprite spriteDoubleJump;
    [SerializeField] private Sprite spriteHeavyGravity;
    [SerializeField] private Sprite spriteMirrorControl;
    [SerializeField] private Sprite spriteInvertControls;
    [SerializeField] private Sprite spriteJetpack;
    [SerializeField] private Sprite spriteEmpty;

    private PlatformPlayerController trackedPlayer;
    private void Awake()
    {
        ShowEmpty();
    }

    // Update is called once per frame
    void Update()
    {
     if(trackedPlayer!=null)
        {
            Refresh();
        }
    }
    //Se llama en el minijuego dos, en KingOfHill.StartMinigame()
    public void TrackPlayer(PlatformPlayerController player)
    {
        trackedPlayer = player;
        Debug.Log($"HUD {gameObject.name} trackeando a {player.gameObject.name}");
    }
    //se llama desde el powerUp manager cuando cambia el estado del power up
    public void ManualUpdate(bool hasPowerUp,PowerUpPickup.PowerUpType type)
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
        if (powerUpNameText != null) powerUpNameText.text = GetGame(type);
    }
    private void ShowEmpty()
    {
        if (iconImage != null) iconImage.sprite = spriteEmpty;
        if(powerUpNameText!= null) powerUpNameText.text="Without Power Up";
    }
    private Sprite GetSprite(PowerUpPickup.PowerUpType type)
    {
        switch(type)
        {
            case PowerUpPickup.PowerUpType.Cage: return spriteCage;
            case PowerUpPickup.PowerUpType.Shield: return spriteShield;
            case PowerUpPickup.PowerUpType.Hook: return spriteHook;
            case PowerUpPickup.PowerUpType.DoubleJump: return spriteDoubleJump;
            case PowerUpPickup.PowerUpType.HeavyGravity: return spriteHeavyGravity;
            case PowerUpPickup.PowerUpType.MirrorControl: return spriteMirrorControl;
            case PowerUpPickup.PowerUpType.InvertControls: return spriteInvertControls;
            case PowerUpPickup.PowerUpType.Jetpack: return spriteJetpack;
            default: return spriteEmpty;
        }
    }
    private string GetGame(PowerUpPickup.PowerUpType type)
    {
        switch (type)
        {
            case PowerUpPickup.PowerUpType.Cage: return "Cage";
            case PowerUpPickup.PowerUpType.Shield: return "Shield";
            case PowerUpPickup.PowerUpType.Hook: return "Hook";
            case PowerUpPickup.PowerUpType.DoubleJump: return "Double Jump";
            case PowerUpPickup.PowerUpType.HeavyGravity: return "Heavy Gravity";
            case PowerUpPickup.PowerUpType.MirrorControl: return "Mirror Control";
            case PowerUpPickup.PowerUpType.InvertControls: return "Invert Controls";
            case PowerUpPickup.PowerUpType.Jetpack: return "Jetpack";
            default: return "Without Power Up";
        }
    }

}

