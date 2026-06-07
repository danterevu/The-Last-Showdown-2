using UnityEngine;

/// SpaceCrossingShip
///
/// SETUP del prefab:
///   - SpriteRenderer con el sprite de la nave mediana
///   - Rigidbody2D: Gravity Scale=0, Body Type=Kinematic, Collision Detection=Continuous
///   - Collider2D (Box o Circle): Is Trigger = FALSE
///   - Este script
///
/// CAPAS: en Physics 2D > Layer Collision Matrix, la capa de este prefab
/// solo debe colisionar con la capa de los jugadores. Ignorar todo lo demas.

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceCrossingShip : MonoBehaviour
{
    [Header("Colision con jugadores")]
    [Tooltip("Fuerza de empuje que recibe el jugador al ser golpeado")]
    [SerializeField] private float knockbackForce = 18f;

    [Tooltip("Duracion del stun en segundos")]
    [SerializeField] private float stunDuration = 1.8f;

    [Header("Sprite")]
    [Tooltip("Offset de rotacion del sprite. 0 = apunta a la derecha, -90 = apunta arriba")]
    [SerializeField] private float spriteRotationOffset = 0f;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private float speed;
    private bool isActive = false;
    private Rigidbody2D rb;

    private bool hitPlayer1 = false;
    private bool hitPlayer2 = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Launch(Vector2 from, Vector2 to, float travelSpeed)
    {
        startPoint = from;
        endPoint = to;
        speed = travelSpeed;
        isActive = true;
        hitPlayer1 = false;
        hitPlayer2 = false;

        transform.position = from;

        Vector2 dir = (to - from).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteRotationOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        gameObject.SetActive(true);
        Debug.Log("[SpaceCrossingShip] Lanzada de " + from + " a " + to);
    }

    private void Update()
    {
        if (!isActive) return;

        transform.position = Vector2.MoveTowards(transform.position, endPoint, speed * Time.deltaTime);
        rb.MovePosition(transform.position);

        if (Vector2.Distance(transform.position, endPoint) < 0.1f)
            Deactivate();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;

        bool isP1 = collision.gameObject.CompareTag("Player1");
        bool isP2 = collision.gameObject.CompareTag("Player2");

        if (!isP1 && !isP2) return;
        if (isP1 && hitPlayer1) return;
        if (isP2 && hitPlayer2) return;

        if (isP1) hitPlayer1 = true;
        if (isP2) hitPlayer2 = true;

        SpaceShipController ship = collision.gameObject.GetComponent<SpaceShipController>();
        if (ship == null) return;

        Vector2 travelDir = (endPoint - startPoint).normalized;
        ship.ApplyStun(stunDuration);
        ship.AddImpulse(travelDir * knockbackForce);

        Debug.Log("[SpaceCrossingShip] Golpeo a " + collision.gameObject.name);
    }

    private void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
        SpaceMediumShipEvent.Instance?.OnCrossingShipFinished(this);
    }

    public void ForceDeactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!isActive) return;
        Gizmos.color = Color.red;
        Vector2 dir = (endPoint - (Vector2)transform.position).normalized;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dir * 1.5f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
#endif
}
