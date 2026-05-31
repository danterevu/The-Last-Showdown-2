using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis { Horizontal, Vertical }

    [Header("Movimiento")]
    [SerializeField] private MoveAxis axis = MoveAxis.Horizontal;
    [SerializeField] private float distance = 4f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private Ease easeType = Ease.Linear;

    private Vector2 pointA;
    private Vector2 pointB;
    private Tween moveTween;

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        Vector2 startPos = transform.position;
        Vector2 dir = axis == MoveAxis.Horizontal ? Vector2.right : Vector2.up;
        pointA = startPos - dir * (distance / 2f);
        pointB = startPos + dir * (distance / 2f);

        transform.position = pointA;
        StartMovement();
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

        if (IsAbove(col))
        {
            col.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (!IsPlayer(col.gameObject)) return;
        
        if (col.transform.parent == transform)
        {
            col.transform.SetParent(null);
        }
    }

    private bool IsPlayer(GameObject go)
        => go.CompareTag("Player1") || go.CompareTag("Player2");

    private bool IsAbove(Collision2D col)
    {
        foreach (var contact in col.contacts)
            if (contact.normal.y > 0.5f) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? (Vector2)transform.position : (Vector2)transform.position;
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
