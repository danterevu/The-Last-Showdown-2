using UnityEngine;

public class Menu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void MenuButton()
    {
        SceneLoader.Instance.LoadRuleta();
    }
}
