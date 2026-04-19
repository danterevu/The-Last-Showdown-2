    using UnityEngine;
    using System.Collections.Generic;

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Puntos Globales (persisten entre minijuegos)")]
        public int player1Score;
        public int player2Score;

        [Header("Puntos de la ronda actual")]
        public int player1RoundPoints;
        public int player2RoundPoints;

        [Header("Estado del juego")]
        public int currentRound = 1;
        public const int TOTAL_ROUNDS = 2;

        // modificador activo para esta ronda
        [HideInInspector] public float player1Multiplier = 1f;
        [HideInInspector] public float player2Multiplier = 1f;

        private List<int> availableMinigames = new List<int>();
        private List<int> playedMinigames = new List<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMinigames();
        }

    public bool IsMinigameAvailable(int id)
    {
        return availableMinigames.Contains(id);
    }

    private void InitializeMinigames()
        {
            availableMinigames.Clear();
            playedMinigames.Clear();
            for (int i = 1; i <= TOTAL_ROUNDS; i++)
                availableMinigames.Add(i);
        }

        // llamado desde cada minijuego para sumar puntos de la ronda
        // aplica el multiplicador activo del jugador
        public void AddPoints(int player, int amount)
        {
            int final = Mathf.RoundToInt(amount * (player == 1 ? player1Multiplier : player2Multiplier));
            if (player == 1) player1RoundPoints += final;
            else player2RoundPoints += final;
        }

        // para restar puntos (sin aplicar multiplicador, la resta es directa)
        public void RemovePoints(int player, int amount)
        {
            if (player == 1) player1RoundPoints = Mathf.Max(0, player1RoundPoints - amount);
            else player2RoundPoints = Mathf.Max(0, player2RoundPoints - amount);
        }

        // llamado al terminar el minijuego - transfiere puntos de ronda al global
        // devuelve los puntos ganados en la ronda para mostrarlos en Results
        public (int p1Round, int p2Round) FinishMinigame()
        {
            int p1 = player1RoundPoints;
            int p2 = player2RoundPoints;

            player1Score += p1;
            player2Score += p2;

            return (p1, p2);
        }

        // llamado despues de FinishMinigame para registrar la ronda y limpiar
        public void EndRound(int minigameId)
        {
            playedMinigames.Add(minigameId);
            availableMinigames.Remove(minigameId);
            currentRound++;

            // resetear puntos de ronda y multiplicadores para la proxima
            player1RoundPoints = 0;
            player2RoundPoints = 0;
            player1Multiplier = 1f;
            player2Multiplier = 1f;
        }

        // usado por DodgeDisk - AddResult original simplificado
        public void AddResult(int player, bool won)
        {
            int points = won ? 10 : -10;
            AddPoints(player, Mathf.Abs(points));
            if (!won) RemovePoints(player, Mathf.Abs(points) * 2); // resta neta
        }

        public bool IsGameOver() => currentRound > TOTAL_ROUNDS;
        public List<int> GetAvailableMinigames() => new List<int>(availableMinigames);

        public void ResetGame()
        {
            player1Score = 0;
            player2Score = 0;
            player1RoundPoints = 0;
            player2RoundPoints = 0;
            player1Multiplier = 1f;
            player2Multiplier = 1f;
            currentRound = 1;
            InitializeMinigames();
        }
    }