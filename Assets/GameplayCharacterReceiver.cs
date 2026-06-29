using UnityEngine;

[DefaultExecutionOrder(-90)]
public class GameplayCharacterReceiver : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("1 = Jugador 1, 2 = Jugador 2")]
    [SerializeField] private int playerNumber = 1;

    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [System.Serializable]
    private class CharacterVisual
    {
        public InputAssigner.Character character;
        public RuntimeAnimatorController animatorController;
        public Sprite sprite;
    }

    [Header("Visuales")]
    [SerializeField] private CharacterVisual gloppk;
    [SerializeField] private CharacterVisual chopi;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        var player = InputAssigner.GetInternalPlayer(playerNumber);

        if (player == null)
        {
            Debug.LogWarning($"GameplayCharacterReceiver: No existe el Player {playerNumber}.");
            return;
        }

        CharacterVisual visual = null;

        switch (player.character)
        {
            case InputAssigner.Character.Gloppk:
                visual = gloppk;
                break;

            case InputAssigner.Character.Chopi:
                visual = chopi;
                break;
        }

        if (visual == null)
            return;

        if (animator != null && visual.animatorController != null)
            animator.runtimeAnimatorController = visual.animatorController;

        if (spriteRenderer != null && visual.sprite != null)
            spriteRenderer.sprite = visual.sprite;
    }
}