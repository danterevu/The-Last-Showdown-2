using UnityEngine;

public class DNAPowerUpPickup : MonoBehaviour
{
    public enum DNAPowerUpType
    {
        Shrink,
        Mine,
        RemoteControl,
        Berserk,
        SlimeShot,
        Shield
    }

    [SerializeField] private DNAPowerUpType type;
    [SerializeField] private GameObject explodeParticles;
    private DNAPowerUpSpawner spawner;
    private Transform spawnPoint;

    public void Initialize(DNAPowerUpSpawner spawner, Transform spawnPoint)
    {
        this.spawner = spawner;
        this.spawnPoint = spawnPoint;
        type = (DNAPowerUpType)Random.Range(0, System.Enum.GetValues(typeof(DNAPowerUpType)).Length);
        Vector3 spawnPos = transform.position;
        spawnPos.z = 0f;
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

        PlayerControllerDNA player = other.GetComponent<PlayerControllerDNA>();
        if (player == null) return;

        // Si ya tiene un powerup, soltarlo como esfera antes de agarrar este
        if (player.HasPowerUp())
        {
            DNAPowerUpPickup.DNAPowerUpType currentType = player.GetCurrentPowerUp();

            // Solo spawnear esfera si el tipo tiene esfera (no RemoteControl)
            if (currentType != DNAPowerUpType.RemoteControl)
            {
                Vector2 launchDir = new Vector2(
                    player.IsFacingRight() ? -1f : 1f, // sale por atrás del jugador
                    0.5f
                ).normalized;

                DNADroppedPowerUpSpawner dnaSpawner = Object.FindFirstObjectByType<DNADroppedPowerUpSpawner>();
                if (dnaSpawner != null)
                    DNADroppedPowerUp.Spawn(dnaSpawner.Prefabs, currentType, player.transform.position, launchDir, player);
            }

            player.ClearPowerUpState();
        }

        player.ReceiveDNAPowerUp(type);
        spawner?.OnPickupCollected(spawnPoint);

        if (explodeParticles != null)
            Instantiate(explodeParticles, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}