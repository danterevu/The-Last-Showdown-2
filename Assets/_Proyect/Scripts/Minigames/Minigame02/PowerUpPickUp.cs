using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private PowerUpType type;

    private PowerUpSpawner spawner;
    private Transform spawnPoint;

    public enum PowerUpType
    {
        Cage,           // jaula alrededor del hardpoint
        Shield,         // escudo que devuelve knockback
        Hook,           // gancho que atrae al otro
        DoubleJump,     // doble salto temporal
        HeavyGravity,   // aumenta gravedad del otro
        MirrorControl,  // copia tu movimiento al rival
        InvertControls, // invierte inputs del rival
        Jetpack         // volar manteniendo salto
    }

    public void Initialize(PowerUpSpawner spawner, Transform spawnPoint)
    {
        this.spawner = spawner;
        this.spawnPoint = spawnPoint;
        type = (PowerUpType)Random.Range(0, System.Enum.GetValues(typeof(PowerUpType)).Length);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

        PlatformPlayerController player = other.GetComponent<PlatformPlayerController>();
        if (player == null) return;

        // FIX: chequeo duplicado eliminado - solo uno es necesario
        if (player.HasPowerUp()) return;

        player.ReceivePowerUp(type);
        spawner.OnPickupCollected(spawnPoint);
        Destroy(gameObject);
    }
}
