using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUDMinigame1 : MonoBehaviour
{
    [Header("Iconos con el mismo orden del Enum de PowerUpManager")]
    [SerializeField] private Sprite[] powerUpIcons;

    [Header("Referencias UI")]
    [SerializeField] private Image player1Icon;
    [SerializeField] private Image player2Icon;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        player1Icon.gameObject.SetActive(false);
        player2Icon.gameObject.SetActive(false);
    }
    
   public void ShowIcon(int player, PowerUpManager.PowerUpType type)
    {
        Image icon = player == 1 ? player1Icon : player2Icon;
        icon.sprite = powerUpIcons[(int)type];
        icon.gameObject.SetActive(true);
    }

   public void HideIcon(int player)
    {
        Image icon = player == 1 ? player1Icon : player2Icon;
        icon.gameObject.SetActive(false);
    }
}
