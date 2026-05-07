using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis { Horizontal, Vertical }

    [Header("Movimiento")]
    [SerializeField] private MoveAxis axis = MoveAxis.Horizontal;
    [SerializeField] private float distance = 4f;
    [SerializeField] private float speed = 2f;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private Vector2 pointA;
    private Vector2 pointB;
    private Vector2 target;
    private Vector2 delta;

    // jugadores que están parados arriba
    private readonly HashSet<Rigidbody2D> passengers = new HashSet<Rigidbody2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // suaviza el movimiento
    }

    private void Start()
    {
        startPos = rb.position;

        Vector2 dir = axis == MoveAxis.Horizontal ? Vector2.right : Vector2.up;
        pointA = startPos - dir * (distance / 2f);
        pointB = startPos + dir * (distance / 2f);
        target = pointB;
    }

    private void FixedUpdate()
    {
        // calcular delta real antes de mover
        Vector2 oldPos = rb.position;
        Vector2 newPos = Vector2.MoveTowards(oldPos, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
        delta = newPos - oldPos;

        // arrastrar a los pasajeros por el mismo delta
        // esto hace que el jugador siga la plataforma tanto horizontal como verticalmente
        foreach (var passenger in passengers)
        {
            if (passenger == null) continue;
            passenger.MovePosition(passenger.position + delta);
        }

        // cambiar target al llegar
        if (Vector2.Distance(newPos, target) < 0.02f)
            target = (target == pointA) ? pointB : pointA;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsPlayer(col.gameObject)) return;

        // solo agregar si el jugador está ARRIBA de la plataforma
        if (IsAbove(col))
            passengers.Add(col.rigidbody);
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (!IsPlayer(col.gameObject)) return;
        passengers.Remove(col.rigidbody);
    }

    private bool IsPlayer(GameObject go)
        => go.CompareTag("Player1") || go.CompareTag("Player2");

    // normal.y > 0.5 = el contacto viene desde arriba (jugador sobre la plataforma)
    private bool IsAbove(Collision2D col)
    {
        foreach (var contact in col.contacts)
            if (contact.normal.y > 0.5f) return true;
        return false;
    }

    // preview del recorrido en el editor
    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? startPos : (Vector2)transform.position;
        Vector2 dir = axis == MoveAxis.Horizontal ? Vector2.right : Vector2.up;
        Vector2 a = center - dir * (distance / 2f);
        Vector2 b = center + dir * (distance / 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.15f);
        Gizmos.DrawSphere(b, 0.15f);
    }
}
