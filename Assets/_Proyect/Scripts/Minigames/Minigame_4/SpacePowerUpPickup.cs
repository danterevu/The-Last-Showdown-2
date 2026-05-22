using UnityEngine;


/// SETUP del prefab:
///   - SpriteRenderer con el sprite del power up
///   - CircleCollider2D con Is Trigger = true
///   - Este script
public class SpacePowerUpPickup : MonoBehaviour
{
    private SpacePowerUpSpawner spawner;
    private Transform spawnPoint;
    private SpacePowerUpType assignedType;

    /// Llamado por SpacePowerUpSpawner al instanciar este pickup.
    public void Init(SpacePowerUpType type, SpacePowerUpSpawner spawner, Transform spawnPoint)
    {
        this.assignedType = type;
        this.spawner = spawner;
        this.spawnPoint = spawnPoint;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

        PowerUpHolder holder = other.GetComponent<PowerUpHolder>();
        if (holder == null) return;

        // No entregar si ya tiene un power up guardado
        if (holder.HasPowerUp) return;

        holder.ReceivePowerUp(assignedType);
        spawner?.OnPickupCollected(spawnPoint);
        Destroy(gameObject);
    }
}