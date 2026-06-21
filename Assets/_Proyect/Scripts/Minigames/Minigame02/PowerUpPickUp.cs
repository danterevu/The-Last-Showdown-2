using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private PowerUpType type;
    private PowerUpSpawner spawner;
    private Transform spawnPoint;
    public GameObject explodeParticles;

    public enum PowerUpType
    {
        Hook,           // gancho que atrae al otro
        HeavyGravity,   // aumenta gravedad del otro
        MirrorControl,  // copia tu movimiento al rival
        Jetpack,        // volar manteniendo salto
        Crusher         // aplastadora que mata a ambos jugadores sin dar puntos
    }

    private void Start()
    {
        transform.rotation = Quaternion.identity;
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

        if (player.HasPowerUp())
        {
            DroppedPowerUpSpawner dropSpawner = Object.FindFirstObjectByType<DroppedPowerUpSpawner>();
            if (dropSpawner != null)
            {
                Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.4f, 1f)).normalized;
                // pasar player como owner para que tenga 1s de cooldown
                DroppedPowerUp.Spawn(dropSpawner.Prefabs, player.GetCurrentPowerUp(),
                                     player.transform.position, dir, player);
                player.ClearPowerUpState();
            }
        }

        player.ReceivePowerUp(type);
        spawner.OnPickupCollected(spawnPoint);
        Explode();
        Destroy(gameObject);
    }

    public void Explode()
    {
        Instantiate(explodeParticles, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}