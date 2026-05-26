using UnityEngine;

public class DNAPowerUpPickup : MonoBehaviour
{
    public enum DNAPowerUpType
    {
        Shrink,  // se vuelve más chiquito y rápido
        Mine,    // deja una mina que se arma y despues explota
        RemoteControl, // Activa paredes
        Berserk //se vuelve loco y ataca hacia adelante
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
        transform.rotation = Quaternion.identity; //  rotación siempre en 0
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

        PlayerControllerDNA player = other.GetComponent<PlayerControllerDNA>();
        if (player == null) return;
        if (player.HasPowerUp()) return;

        player.ReceiveDNAPowerUp(type);
        spawner?.OnPickupCollected(spawnPoint);

        if (explodeParticles != null)
            Instantiate(explodeParticles, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
