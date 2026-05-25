using UnityEngine;

public class Menu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance?.PlayMusic(SoundID.MenuMusic);
    }
    
    public void MenuButton()
    {
        AudioManager.Instance?.StopMusic();
        SceneLoader.Instance.LoadCharacterSelection();
    }

    public void Quit()
    {
        Application.Quit();
    }
}
