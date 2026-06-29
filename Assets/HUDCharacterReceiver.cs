using UnityEngine;
using UnityEngine.UI;

/// Aplica el Animator Controller y el sprite del personaje elegido
/// al elemento de personaje del HUD (Animator + Image de UI).
///
/// SETUP:
///   - Poner este componente en el GameObject del HUD que representa
///     al personaje de un jugador (el que tiene Animator e Image).
///   - Asignar playerNumber (1 o 2) y el visualType del minijuego.
///   - Si Animator o Image están en un hijo, se buscan automáticamente.
///   - hudAnimatorController y hudIdleSprite en GameplayVisualSet
///     deben estar asignados en el Inspector de InputAssigner.

[DefaultExecutionOrder(-90)]
public class HUDCharacterReceiver : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("1 = Jugador 1, 2 = Jugador 2")]
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private InputAssigner.VisualType visualType;

    [Header("Referencias (se autocompletan si se dejan vacías)")]
    [SerializeField] private Animator animator;
    [SerializeField] private Image characterImage;

    [Header("Fallback (solo si no hay datos de InputAssigner)")]
    [SerializeField] private RuntimeAnimatorController fallbackController;
    [SerializeField] private Sprite fallbackSprite;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (characterImage == null)
            characterImage = GetComponentInChildren<Image>(true);

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        RuntimeAnimatorController controller =
            InputAssigner.GetHUDAnimatorController(playerNumber, visualType);
        Sprite sprite =
            InputAssigner.GetHUDIdleSprite(playerNumber, visualType);

        if (controller == null)
        {
            controller = fallbackController;
            Debug.LogWarning($"[HUDCharacterReceiver] Jugador {playerNumber}: " +
                             $"no se encontró hudAnimatorController para tipo={visualType}. " +
                             $"Usando fallback.");
        }

        if (sprite == null)
        {
            sprite = fallbackSprite;
            Debug.LogWarning($"[HUDCharacterReceiver] Jugador {playerNumber}: " +
                             $"no se encontró hudIdleSprite para tipo={visualType}. " +
                             $"Usando fallback.");
        }

        if (animator != null && controller != null)
            animator.runtimeAnimatorController = controller;

        if (characterImage != null && sprite != null)
            characterImage.sprite = sprite;
        // HOLA
    }
}