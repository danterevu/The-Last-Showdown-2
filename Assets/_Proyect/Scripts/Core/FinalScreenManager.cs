using TMPro;
using UnityEngine;

public class FinalScreenManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;

    private void Start()
    {
        int p1 = GameManager.Instance.player1Score;
        int p2 = GameManager.Instance.player2Score;

        if (p1 > p2)
            resultText.text = "¡Ganó Gloppy!\n{p1} - {p2}";
        else if (p2 > p1)
            resultText.text = "¡Ganó Choppy!\n{p1} - {p2}";
        else
            resultText.text = "¡Empate!\n{p1} - {p2}";
    }
    public void OnRestartButton()
    {
        GameManager.Instance.ResetGame();
        SceneLoader.Instance.LoadMenu();
    }
    
}