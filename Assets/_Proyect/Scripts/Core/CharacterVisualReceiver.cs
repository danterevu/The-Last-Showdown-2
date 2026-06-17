using UnityEngine;

/// <summary>
/// Aplica el Animator Controller y el sprite idle correctos al jugador,
/// según el personaje elegido en la pantalla de selección (InputAssigner).
///
/// SETUP:
///   - Poner este componente en el mismo GameObject que tiene el tag
///     "Player1" o "Player2" (el jugador real dentro de cada minijuego).
///   - Si el Animator o el SpriteRenderer están en un hijo (por ejemplo,
///     en las naves del Minigame_4 donde WeaponController usa
///     GetComponentInChildren para el Animator), el componente los busca
///     solo en hijos automáticamente. Si hay más de un SpriteRenderer en
///     la jerarquía, asigná el correcto a mano en el Inspector.
///   - fallbackController / fallbackIdleSprite son opcionales: se usan
///     únicamente si todavía no hay datos guardados (por ejemplo, al
///     probar el minijuego suelto sin pasar por la pantalla de selección).
///
/// No requiere modificar PlatformPlayerController, PlayerControllerDNA,
/// SpaceShipController ni ChaseRunPlayerController.
/// </summary>
[DefaultExecutionOrder(-100)] // se ejecuta antes que el resto de los scripts del jugador
public class CharacterVisualReceiver : MonoBehaviour
{
    [Header("Tipo de minijuego")]
    [Tooltip("Platform = lateral con salto (King of Hill, Chase Run). TopDown = vista superior, movimiento libre (Dodge Disk, DNA). Ship = nave espacial (Space).")]
    [SerializeField] private InputAssigner.VisualType visualType;

    [Header("Referencias (se autocompletan si se dejan vacías)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Fallback (solo si no hay datos de InputAssigner)")]
    [SerializeField] private RuntimeAnimatorController fallbackController;
    [SerializeField] private Sprite fallbackIdleSprite;

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
        int internalIndex = GetInternalIndexFromTag();
        if (internalIndex == 0)
        {
            Debug.LogWarning($"[CharacterVisualReceiver] {gameObject.name}: no tiene tag Player1/Player2, no se puede aplicar el personaje.");
            return;
        }

        RuntimeAnimatorController controller = InputAssigner.GetAnimatorController(internalIndex, visualType);
        Sprite idleSprite = InputAssigner.GetIdleSprite(internalIndex, visualType);

        if (controller == null) controller = fallbackController;
        if (idleSprite == null) idleSprite = fallbackIdleSprite;

        if (animator != null && controller != null)
            animator.runtimeAnimatorController = controller;
        else if (controller == null)
            Debug.LogWarning($"[CharacterVisualReceiver] {gameObject.name}: no se encontró Animator Controller para internalIndex={internalIndex}, tipo={visualType} (ni en InputAssigner ni en fallback).");

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    private int GetInternalIndexFromTag()
    {
        if (CompareTag("Player1")) return 1;
        if (CompareTag("Player2")) return 2;
        return 0;
    }
}