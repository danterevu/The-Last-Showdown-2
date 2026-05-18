using UnityEngine;
using UnityEngine.UI;

public class SpacePowerUpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Sprites de cada Power Up")]
    [SerializeField] private Sprite spriteSlowGrande;
    [SerializeField] private Sprite spriteRocketSabotage;
    [SerializeField] private Sprite spriteMeteorStrike;
    [SerializeField] private Sprite spriteHomingMissile;
    [SerializeField] private Sprite spriteRepulsion;
    [SerializeField] private Sprite spriteEmpty;

    [Header("Setup (opcional)")]
    [Tooltip("Si lo dejas vacío, SpacePowerUpManager lo inicializará automáticamente")]
    [SerializeField] private PowerUpHolder targetHolder;

    private PowerUpHolder trackedHolder;

    private void Awake()
    {
        ShowEmpty();
    }

    private void Start()
    {
        if (targetHolder != null)
        {
            TrackHolder(targetHolder);
        }
    }

    public void TrackHolder(PowerUpHolder holder)
    {
        if (trackedHolder != null)
            trackedHolder.OnPowerUpChanged -= OnPowerUpChanged;

        trackedHolder = holder;
        if (trackedHolder != null)
        {
            trackedHolder.OnPowerUpChanged += OnPowerUpChanged;
            OnPowerUpChanged(trackedHolder.HasPowerUp ? trackedHolder.heldPowerUp : null);
        }
    }

    private void OnPowerUpChanged(SpacePowerUpType? type)
    {
        if (type.HasValue)
            Show(type.Value);
        else
            ShowEmpty();
    }

    private void Show(SpacePowerUpType type)
    {
        if (iconImage != null)
            iconImage.sprite = GetSprite(type);
    }

    private void ShowEmpty()
    {
        if (iconImage != null)
            iconImage.sprite = spriteEmpty;
    }

    private Sprite GetSprite(SpacePowerUpType type)
    {
        switch (type)
        {
            case SpacePowerUpType.SlowGrande: return spriteSlowGrande;
            case SpacePowerUpType.RocketSabotage: return spriteRocketSabotage;
            case SpacePowerUpType.MeteorStrike: return spriteMeteorStrike;
            case SpacePowerUpType.HomingMissile: return spriteHomingMissile;
            case SpacePowerUpType.Repulsion: return spriteRepulsion;
            default: return spriteEmpty;
        }
    }

    private void OnDestroy()
    {
        if (trackedHolder != null)
            trackedHolder.OnPowerUpChanged -= OnPowerUpChanged;
    }
}
