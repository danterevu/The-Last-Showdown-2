using UnityEngine;
using TMPro;

public class Results : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI player1RoundText;
    [SerializeField] private TextMeshProUGUI player2RoundText;
    [SerializeField] private TextMeshProUGUI player1TotalText;
    [SerializeField] private TextMeshProUGUI player2TotalText;

    private void Start()
    {
        // puntos de esta ronda (guardados antes de transferir al global)
        int p1Round = PlayerPrefs.GetInt("LastRoundP1", 0);
        int p2Round = PlayerPrefs.GetInt("LastRoundP2", 0);

        player1RoundText.text = "Jugador 1 esta ronda: +" + p1Round + " pts";
        player2RoundText.text = "Jugador 2 esta ronda: +" + p2Round + " pts";

        // total global (ya incluye esta ronda porque FinishMinigame los sumó)
        player1TotalText.text = "Total J1: " + GameManager.Instance.player1Score + " pts";
        player2TotalText.text = "Total J2: " + GameManager.Instance.player2Score + " pts";
    }

    public void OnContinuarButton()
    {
        if (GameManager.Instance.IsGameOver())
            SceneLoader.Instance.LoadFinalScreen();
        else
            SceneLoader.Instance.LoadRuleta();
    }

    public void OnSalirButton()
    {
        Application.Quit();
    }
}