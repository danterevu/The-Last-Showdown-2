using UnityEngine;

/// <summary>
/// Ponerlo en el mismo GO que la flecha (con SpriteRenderer).
/// NO desactiva el GO, solo muestra/oculta el sprite.
/// Asigná ownerPlayer = 1 en el depósito del jugador 1, y 2 en el del jugador 2.
/// </summary>
public class DNAArrowIndicator : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("A qué jugador pertenece este depósito (1 o 2)")]
    [SerializeField] private int ownerPlayer = 1;

    [Header("Referencia al DNA")]
    [SerializeField] private DNA dna;

    [Header("Animación")]
    [Tooltip("Distancia del rebote hacia abajo (en unidades Unity)")]
    [SerializeField] private float bounceDistance = 0.2f;
    [Tooltip("Velocidad del rebote (ciclos por segundo)")]
    [SerializeField] private float bounceSpeed = 2f;

    private SpriteRenderer sr;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseLocalPosition = transform.position; // posición mundial porque no es hijo de nada

        // Empieza oculta pero el GO sigue activo
        if (sr != null) sr.enabled = false;
    }

    private void Update()
    {
        if (dna == null || sr == null) return;

        PlayerControllerDNA holder = dna.GetHolder();
        bool shouldShow = false;

        if (holder != null)
        {
            int holderPlayer = holder.playerIndex + 1; // playerIndex 0 → jugador 1
            shouldShow = (holderPlayer == ownerPlayer);
        }

        sr.enabled = shouldShow;

        // Animación de rebote solo si está visible
        if (shouldShow)
        {
            float offset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed * Mathf.PI * 2f)) * bounceDistance;
            transform.position = baseLocalPosition + new Vector3(0f, -offset, 0f);
        }
        else
        {
            transform.position = baseLocalPosition;
        }
    }
}