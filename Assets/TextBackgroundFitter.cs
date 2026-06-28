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

        float height = tmp.preferredHeight;
        if (height < 0.01f) return;

        // Agrandar el rect del texto para que no se corte
        Vector2 size = tmp.rectTransform.sizeDelta;
        size.y = height;
        tmp.rectTransform.sizeDelta = size;

        // Ajustar el fondo al rect actualizado
        float width = tmp.rectTransform.rect.width;
        background.size = new Vector2(width, height) + padding * 2f;
    }
}