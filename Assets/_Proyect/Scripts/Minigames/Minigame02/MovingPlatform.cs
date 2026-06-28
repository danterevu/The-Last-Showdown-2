using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis { Horizontal, Vertical }

    [Header("Movimiento")]
    [SerializeField] private MoveAxis axis = MoveAxis.Horizontal;
    [SerializeField] private float distance = 4f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private Ease easeType = Ease.Linear;

    private Vector2 lastPosition;
    private Vector2 platformVelocity;
    private Vector2 pointA;
    private Vector2 pointB;
    private Tween moveTween;

    // Diccionario para registrar a qué controladores estamos afectando activamente
    private readonly Dictionary<Collider2D, PlatformPlayerController> activePlayers = new();

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        lastPosition = transform.position;
        Vector2 startPos = transform.position;
        Vector2 dir = axis == MoveAxis.Horizontal ? Vector2.right : Vector2.up;
        pointA = startPos - dir * (distance / 2f);
        pointB = startPos + dir * (distance / 2f);

        transform.position = pointA;
        StartMovement();
    }

    private void FixedUpdate()
    {
        // Calcular la velocidad real de la plataforma en este frame de física
        Vector2 currentPosition = transform.position;
        platformVelocity = (currentPosition - lastPosition) / Time.fixedDeltaTime;
        lastPosition = currentPosition;

        // Inyectar de manera constante la velocidad calculada a todos los jugadores que sigan arriba
        foreach (var playerController in activePlayers.Values)
        {
            if (playerController != null)
            {
                playerController.SetPlatformVelocity(platformVelocity);
            }
        }
    }

    private void StartMovement()
    {
        float duration = distance / speed;

        moveTween = transform.DOMove(pointB, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsPlayer(col.gameObject)) return;
        if (!IsAbove(col)) return;

        if (col.gameObject.TryGetComponent<PlatformPlayerController>(out var playerController))
        {
            if (!activePlayers.ContainsKey(col.collider))
            {
                activePlayers.Add(col.collider, playerController);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        // Por seguridad, si el jugador se desliza o cambia su ángulo y ya no está "arriba", lo removemos
        if (activePlayers.ContainsKey(col.collider) && !IsAbove(col))
        {
            RemovePlayer(col.collider);
        }
        // O si entra por fricción lateral y luego termina arriba
        else if (!activePlayers.ContainsKey(col.collider) && IsPlayer(col.gameObject) && IsAbove(col))
        {
            if (col.gameObject.TryGetComponent<PlatformPlayerController>(out var playerController))
            {
                activePlayers.Add(col.collider, playerController);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (activePlayers.ContainsKey(col.collider))
        {
            RemovePlayer(col.collider);
        }
    }

    private void RemovePlayer(Collider2D collider)
    {
        if (activePlayers.TryGetValue(collider, out var playerController))
        {
            if (playerController != null)
            {
                playerController.SetPlatformVelocity(Vector2.zero);
            }
            activePlayers.Remove(collider);
        }
    }

    private bool IsPlayer(GameObject go)
        => go.CompareTag("Player1") || go.CompareTag("Player2");

    private bool IsAbove(Collision2D col)
    {
        // Las normales de contacto en colisiones 2D apuntan desde la otra superficie hacia esta
        // Una normal con Y > 0.5f (apuntando hacia arriba) significa que el objeto colisionó desde el tope
        foreach (var contact in col.contacts)
        {
            if (contact.normal.y < -0.5f) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? pointA + (pointB - pointA) * 0.5f : (Vector2)transform.position;
        Vector2 dir = axis == MoveAxis.Horizontal ? Vector2.right : Vector2.up;
        Vector2 a = center - dir * (distance / 2f);
        Vector2 b = center + dir * (distance / 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.15f);
        Gizmos.DrawSphere(b, 0.15f);
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
    }
}