using UnityEngine;

    [CreateAssetMenu(fileName = "HUDTheme", menuName ="UI/HUD Theme")]
    public class HUDTheme :ScriptableObject
    {
        [Header("Color principal")]
        public Color hudColor = Color.white; //el hud de forma predeterminada es blanco

        [Header("Colores de texto")]
        public Color player1TextColor = Color.white;
        public Color player2TextColor = Color.white;
        public Color timerTextColor = Color.white;
    }

