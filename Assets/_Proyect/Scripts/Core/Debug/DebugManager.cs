using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;

    public static bool IsDebugMode { get; private set; }

    // guarda qué minijuego eligió el profe en el panel debug
    public static int SelectedMinigame { get; private set; } = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Toggle()
    {
        IsDebugMode = !IsDebugMode;
        Debug.Log($"[DebugManager] Modo debug: {(IsDebugMode ? "ON" : "OFF")}");
    }

    public static void SetMinigame(int id)
    {
        SelectedMinigame = id;
    }
}