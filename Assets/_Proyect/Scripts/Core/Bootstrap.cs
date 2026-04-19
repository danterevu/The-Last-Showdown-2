using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string firstScene;

    private void Start()
    {
        // usando Start en vez de Awake para darle tiempo al GameManager
        if (GameManager.Instance == null) // se asegura que exista el GameManager
        {
            Debug.LogError("GameManager not found.");
            return;
        }

        SceneManager.LoadScene(firstScene); //arranca en la primera escena real
    }
}