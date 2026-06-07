using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton que mantiene una pila de UINavigators activos.
/// Solo el que está en el tope de la pila recibe input.
/// 
/// No necesitás asignarlo en ningún lado — se crea solo.
/// UINavigator lo usa automáticamente.
/// </summary>
public class UINavigatorStack : MonoBehaviour
{
    public static UINavigatorStack Instance { get; private set; }

    private Stack<UINavigator> stack = new Stack<UINavigator>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Push(UINavigator nav)
    {
        EnsureExists();
        // Pausa el que estaba activo
        if (Instance.stack.Count > 0)
            Instance.stack.Peek().SetInputEnabled(false);

        Instance.stack.Push(nav);
        nav.SetInputEnabled(true);
    }

    public static void Pop(UINavigator nav)
    {
        EnsureExists();
        if (Instance.stack.Count == 0) return;

        // Si el que se cierra es el del tope, lo saca
        if (Instance.stack.Peek() == nav)
        {
            Instance.stack.Pop();
            // Reactiva el anterior
            if (Instance.stack.Count > 0)
                Instance.stack.Peek().SetInputEnabled(true);
        }
        else
        {
            // Por si acaso se cerró uno que no era el tope (raro pero seguro)
            var temp = new Stack<UINavigator>();
            while (Instance.stack.Count > 0)
            {
                var top = Instance.stack.Pop();
                if (top != nav) temp.Push(top);
            }
            while (temp.Count > 0)
                Instance.stack.Push(temp.Pop());

            if (Instance.stack.Count > 0)
                Instance.stack.Peek().SetInputEnabled(true);
        }
    }

    private static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("UINavigatorStack");
        go.AddComponent<UINavigatorStack>();
    }
}