using UnityEngine;

[DefaultExecutionOrder(-99)]
public class CharacterHandsReceiver : MonoBehaviour
{
    [SerializeField] private InputAssigner.VisualType visualType;

    [SerializeField] private Animator handsAnimator;
    [SerializeField] private SpriteRenderer handsSpriteRenderer;

    [Header("Gloppk")]
    [SerializeField] private RuntimeAnimatorController gloppkController;
    [SerializeField] private Sprite gloppkIdle;

    [Header("Chopi")]
    [SerializeField] private RuntimeAnimatorController chopiController;
    [SerializeField] private Sprite chopiIdle;

    private void Awake()
    {
        if (handsAnimator == null)
            handsAnimator = GetComponent<Animator>();

        if (handsSpriteRenderer == null)
            handsSpriteRenderer = GetComponent<SpriteRenderer>();

        ApplyHands();
    }

    private void ApplyHands()
    {
        int internalIndex = GetInternalIndex();

        if (internalIndex == 0)
            return;

        var playerData = InputAssigner.GetInternalPlayer(internalIndex);

        if (playerData == null)
            return;

        bool isGloppk = playerData.character == InputAssigner.Character.Gloppk;

        if (handsAnimator != null)
            handsAnimator.runtimeAnimatorController =
                isGloppk ? gloppkController : chopiController;

        if (handsSpriteRenderer != null)
            handsSpriteRenderer.sprite =
                isGloppk ? gloppkIdle : chopiIdle;
    }

    private int GetInternalIndex()
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.CompareTag("Player1"))
                return 1;

            if (current.CompareTag("Player2"))
                return 2;

            current = current.parent;
        }

        return 0;
    }
}