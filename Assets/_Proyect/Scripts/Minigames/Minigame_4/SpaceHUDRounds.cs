using UnityEngine;
using TMPro;

public class SpaceHUDRounds : MonoBehaviour
{
    [Header("Kills")]
    [SerializeField] private TextMeshProUGUI killsP1Text;
    [SerializeField] private TextMeshProUGUI killsP2Text;

    [Header("Rondas")]
    [SerializeField] private TextMeshProUGUI roundsP1Text;
    [SerializeField] private TextMeshProUGUI roundsP2Text;

    [Header("Ronda actual")]
    [SerializeField] private TextMeshProUGUI currentRoundText;

    public void UpdateKills(int k1, int k2)
    {
        if (killsP1Text) killsP1Text.text = " " + k1;
        if (killsP2Text) killsP2Text.text = "" + k2;
    }

    public void UpdateRounds(int r1, int r2)
    {
        Debug.Log($"UpdateRounds: {r1} {r2}");
        if (roundsP1Text) roundsP1Text.text = " " + r1;
        if (roundsP2Text) roundsP2Text.text = " " + r2;
    }
    public void UpdateCurrentRound(int round)
    {
        if (currentRoundText) currentRoundText.text = " " + round;
    }
}