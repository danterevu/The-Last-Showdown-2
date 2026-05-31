using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
  
    public static SceneLoader Instance; //es un singletone, todos pueden acceder

    private void Awake()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // LOAD SCENES 
    public void LoadMinigame(int minigameId)
    {
        string sceneName = "Minigame_" + minigameId;

        SceneManager.LoadScene(sceneName);
    }
    public void LoadCharacterSelection()
    {
        SceneManager.LoadScene("CharacterSelection");
    }
    public void LoadRuleta()
    {
        SceneManager.LoadScene("Select_Minigame");
    }
    public void LoadResults()
    {
        SceneManager.LoadScene("Results");
    }

    public void LoadFinalScreen()
    {
        SceneManager.LoadScene("FinalScreen");
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }


}