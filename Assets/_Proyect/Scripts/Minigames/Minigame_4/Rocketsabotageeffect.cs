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

    [Header("Destruir Asteroides")]
    [SerializeField] private bool destroyAsteroidsOnCollision = true;
    [SerializeField] private GameObject explosionVfxPrefab;

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

    private void Start()
    {
        if (ship != null)
        {
            ship.ActivateRocketSabotage();
        }
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!destroyAsteroidsOnCollision) return;

        // Chocar con BreakableAsteroid
        BreakableAsteroid breakableAsteroid = collision.gameObject.GetComponent<BreakableAsteroid>();
        if (breakableAsteroid != null)
        {
            DestroyAsteroid(collision.gameObject, collision.contacts[0].normal);
            return;
        }

        // Chocar con InteractiveAsteroid
        InteractiveAsteroid interactiveAsteroid = collision.gameObject.GetComponent<InteractiveAsteroid>();
        if (interactiveAsteroid != null)
        {
            DestroyAsteroid(collision.gameObject, collision.contacts[0].normal);
        }
    }

    private void DestroyAsteroid(GameObject asteroid, Vector2 collisionNormal)
    {
        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, asteroid.transform.position, Quaternion.identity);
        }

        // Intentar usar SplittableObject si lo tiene
        SplittableObject splittable = asteroid.GetComponent<SplittableObject>();
        if (splittable != null)
        {
            splittable.Split(collisionNormal);
            return;
        }

        Destroy(asteroid);
    }

    private void OnDestroy()
    {
        if (ship != null)
        {
            ship.DeactivateRocketSabotage();
        }
    }
}