using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputAssigner : MonoBehaviour
{
    // Guarda los gamepads asignados, índice 0 = P1, índice 1 = P2
    private static List<Gamepad> assignedGamepads = new List<Gamepad>();

    public static Gamepad GetGamepadForPlayer(int playerIndex)
    {
        //Debug.Log($"GetGamepadForPlayer({playerIndex}) | Total asignados: {assignedGamepads.Count} | " +
          //        $"P0: {(assignedGamepads.Count > 0 ? assignedGamepads[0].displayName : "null")} | " +
            //      $"P1: {(assignedGamepads.Count > 1 ? assignedGamepads[1].displayName : "null")}");

        if (assignedGamepads.Count > playerIndex)
            return assignedGamepads[playerIndex];
        return null;
    }

    public static void Clear() => assignedGamepads.Clear();

    [Header("UI")]
    [SerializeField] private GameObject player1Panel;
    [SerializeField] private GameObject player2Panel;
    [SerializeField] private GameObject startPrompt;
    private float lastAssignTime = 0f;
    public static int AssignedCount => assignedGamepads.Count;

    private void OnEnable()
    {
        assignedGamepads.Clear();
        Debug.Log("InputAssigner: lista limpiada");
        UpdateUI();
    }

    private void Update()
    {
        if (Time.time - lastAssignTime < 0.5f) return;

        foreach (var gamepad in Gamepad.all)
        {
            if (AnyButtonPressed(gamepad))
            {
                if (assignedGamepads.Exists(g => g.deviceId == gamepad.deviceId))
                {
                    Debug.LogWarning($"Control {gamepad.deviceId} ya está asignado");
                    continue;
                }

                assignedGamepads.Add(gamepad);
                lastAssignTime = Time.time;
                Debug.Log($"Jugador {assignedGamepads.Count} asignado a: {gamepad.displayName} | ID: {gamepad.deviceId}");
                UpdateUI();

                if (assignedGamepads.Count >= 2)
                    Invoke(nameof(LoadNextScene), 1f);
            }
        }
    }
    private bool AnyButtonPressed(Gamepad gamepad)
    {
        return gamepad.buttonSouth.wasPressedThisFrame ||
               gamepad.buttonNorth.wasPressedThisFrame ||
               gamepad.buttonEast.wasPressedThisFrame ||
               gamepad.buttonWest.wasPressedThisFrame ||
               gamepad.startButton.wasPressedThisFrame ||
               gamepad.selectButton.wasPressedThisFrame;
    }

    private void UpdateUI()
    {
        if (player1Panel != null)
            player1Panel.SetActive(assignedGamepads.Count >= 1);

        if (player2Panel != null)
            player2Panel.SetActive(assignedGamepads.Count >= 2);

        if (startPrompt != null)
            startPrompt.SetActive(assignedGamepads.Count >= 2);
    }

    public void LoadNextScene()
    {
        SceneLoader.Instance.LoadRuleta();
    }
}