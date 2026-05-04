using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; // panel de UI con el menú de pausa

    private bool isPaused = false;

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        // También podés pausar con Start del gamepad
        Gamepad gp1 = InputAssigner.GetGamepadForPlayer(0);
        Gamepad gp2 = InputAssigner.GetGamepadForPlayer(1);
        if (gp1 != null && gp1.startButton.wasPressedThisFrame) TogglePause();
        if (gp2 != null && gp2.startButton.wasPressedThisFrame) TogglePause();
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
    }

    public void Resume() => TogglePause();

    public void QuitToMenu()
    {
        Time.timeScale = 1f; // importante resetear antes de cambiar de escena
        GameManager.Instance.ResetGame();
        SceneLoader.Instance.LoadMenu();
    }

    public void QuitInGame()
    {
        Application.Quit();
    }
}