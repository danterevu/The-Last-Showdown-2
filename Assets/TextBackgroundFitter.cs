using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class TextBackgroundFitter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private Vector2 padding = new Vector2(0.5f, 0.3f);

    private TextMeshPro tmp;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    private void LateUpdate()
    {
        if (background == null) return;

        tmp.ForceMeshUpdate();
        Vector2 textSize = tmp.GetRenderedValues(onlyVisibleCharacters: false);

        if (textSize.x < 0.01f || textSize.y < 0.01f) return;

        background.size = textSize + padding * 2f;
    }
}