using UnityEngine;

public class HardPoint : MonoBehaviour
{
    // el manager le pregunta esto cada frame
    public bool IsPlayer1Inside { get; private set; }
    public bool IsPlayer2Inside { get; private set; }

    [Header("Visual")]
    [SerializeField] private SpriteRenderer zoneSprite;

    [Header("Colores")]
    [SerializeField] private Color neutralColor = new Color(0f, 1f, 0f, 0.3f);    // verde transparente
    [SerializeField] private Color player1Color = new Color(1f, 0f, 0f, 0.3f);    // rojo = P1 ganando
    [SerializeField] private Color player2Color = new Color(0f, 0f, 1f, 0.3f);    // azul = P2 ganando
    [SerializeField] private Color disputedColor = new Color(1f, 1f, 0f, 0.3f);   // amarillo = disputa

    private void Update()
    {
        if (IsPlayer1Inside && IsPlayer2Inside)
            zoneSprite.color = disputedColor;
        else if (IsPlayer1Inside)
            zoneSprite.color = player1Color;
        else if (IsPlayer2Inside)
            zoneSprite.color = player2Color;
        else
            zoneSprite.color = neutralColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
            IsPlayer1Inside = true;
        else if (other.CompareTag("Player2"))
            IsPlayer2Inside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("HardPoint detectó: " + other.gameObject.name + " tag: " + other.tag);

        if (other.CompareTag("Player1"))
            IsPlayer1Inside = false;
        else if (other.CompareTag("Player2"))
            IsPlayer2Inside = false;
    }
}