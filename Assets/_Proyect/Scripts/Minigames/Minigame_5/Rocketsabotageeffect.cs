using System.Collections;
using UnityEngine;

/// Se adjunta temporalmente a la nave rival al activar Rocket Sabotage.
/// Hace que la nave acelere constantemente en su direccion actual
/// ignorando el input del jugador.

public class RocketSabotageEffect : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float extraAcceleration = 20f;
    [SerializeField] private float duration = 4f;

    private SpaceShipController ship;
    private Rigidbody2D rb;
    private float elapsed;

    public void Init(float extraAcceleration, float duration)
    {
        this.extraAcceleration = extraAcceleration;
        this.duration = duration;
    }

    private void Awake()
    {
        ship = GetComponent<SpaceShipController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        if (elapsed >= duration)
        {
            Destroy(this);
            return;
        }

        // Empujar la nave en la direccion en que mira (transform.right con rotationOffset=0)
        if (rb != null)
            rb.AddForce(transform.right * extraAcceleration, ForceMode2D.Force);
    }
}