using UnityEngine;




[RequireComponent(typeof(Rigidbody2D))]
public class RotatingSaw : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 120f;  // grados por segundo
    [SerializeField] private bool clockwise = true;       // true = sentido horario

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        float direction = clockwise ? -1f : 1f;
        rb.MoveRotation(rb.rotation + direction * rotationSpeed * Time.fixedDeltaTime);
    }
}
