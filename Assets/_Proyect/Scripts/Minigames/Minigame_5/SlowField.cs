using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// SETUP del prefab:
///   - CircleCollider2D con Is Trigger = true, radio ajustable
///   - SpriteRenderer opcional para visualizar el campo
///   - Este script
[RequireComponent(typeof(CircleCollider2D))]
public class SlowField : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("Multiplicador de velocidad aplicado a naves dentro del campo (0.3 = 30% de velocidad)")]
    [SerializeField] private float speedMultiplier = 0.3f;

    [Tooltip("Multiplicador aplicado a proyectiles dentro del campo")]
    [SerializeField] private float bulletSpeedMultiplier = 0.4f;

    [Tooltip("Duracion del campo en segundos")]
    [SerializeField] private float duration = 4f;

    // Naves dentro del campo en este momento
    private List<SpaceShipController> shipsInside = new List<SpaceShipController>();
    // Proyectiles dentro del campo
    private List<Rigidbody2D> bulletsInside = new List<Rigidbody2D>();

    // Velocidades originales guardadas para restaurar
    private Dictionary<SpaceShipController, float> originalMaxSpeeds = new Dictionary<SpaceShipController, float>();
    private Dictionary<SpaceShipController, float> originalAccelerations = new Dictionary<SpaceShipController, float>();

    private void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nave
        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship != null && !shipsInside.Contains(ship))
        {
            shipsInside.Add(ship);
            ApplySlowToShip(ship);
            return;
        }

        // Proyectil (tiene Projectile y Rigidbody2D)
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null && !bulletsInside.Contains(rb))
            {
                bulletsInside.Add(rb);
                rb.linearVelocity *= bulletSpeedMultiplier;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Nave sale del campo: restaurar velocidad
        SpaceShipController ship = other.GetComponent<SpaceShipController>();
        if (ship != null && shipsInside.Contains(ship))
        {
            shipsInside.Remove(ship);
            RestoreShip(ship);
            return;
        }

        // Proyectil sale: no restauramos porque el campo ya lo afecto mientras estuvo dentro
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null) bulletsInside.Remove(rb);
        }
    }

    private void ApplySlowToShip(SpaceShipController ship)
    {
        if (originalMaxSpeeds.ContainsKey(ship)) return;

        // Guardar valores originales
        originalMaxSpeeds[ship] = ship.MaxSpeed;
        originalAccelerations[ship] = ship.Acceleration;

        // Aplicar slow
        ship.SetMaxSpeed(ship.MaxSpeed * speedMultiplier);
        ship.SetAcceleration(ship.Acceleration * speedMultiplier);

        // Frenar la velocidad actual inmediatamente
        ship.SetVelocity(ship.GetVelocity() * speedMultiplier);
    }

    private void RestoreShip(SpaceShipController ship)
    {
        if (!originalMaxSpeeds.ContainsKey(ship)) return;

        ship.SetMaxSpeed(originalMaxSpeeds[ship]);
        ship.SetAcceleration(originalAccelerations[ship]);

        originalMaxSpeeds.Remove(ship);
        originalAccelerations.Remove(ship);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);

        // Restaurar todas las naves que siguen dentro al expirar
        foreach (var ship in new List<SpaceShipController>(shipsInside))
            RestoreShip(ship);

        Destroy(gameObject);
    }
}
