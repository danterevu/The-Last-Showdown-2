using UnityEngine;
using System.Collections;

public class DroppedPowerUp : MonoBehaviour
{
    [Header("Tiempo de vida")]
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float blinkStartTime = 2f;

    [Header("Físicas")]
    [SerializeField] private float launchForce = 4f;

    [Header("Cooldown")]
    [SerializeField] private float pickupDelay = 1f;

    private PowerUpPickup.PowerUpType storedType;
    private PlatformPlayerController owner; // el jugador que dropeó esta esfera
    private Rigidbody2D rb;
    private Collider2D triggerCol;
    private SpriteRenderer[] srs;
    private bool collected = false;

    private void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        rb = GetComponentInParent<Rigidbody2D>();
        srs = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Initialize(PowerUpPickup.PowerUpType type, Vector2 launchDirection,
                           PlatformPlayerController ownerPlayer = null)
    {
        storedType = type;
        owner = ownerPlayer;

        if (rb != null)
            rb.AddForce(launchDirection.normalized * launchForce, ForceMode2D.Impulse);

        // bloquear al owner por pickupDelay segundos
        if (owner != null)
            owner.BlockDroppedPickup(pickupDelay);

        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        float waitTime = lifetime - blinkStartTime;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        float elapsed = 0f;
        while (elapsed < blinkStartTime)
        {
            elapsed += Time.deltaTime;
            float blinkSpeed = Mathf.Lerp(8f, 20f, elapsed / blinkStartTime);
            bool visible = Mathf.FloorToInt(elapsed * blinkSpeed) % 2 == 0;
            foreach (var sr in srs) if (sr != null) sr.enabled = visible;
            yield return null;
        }
        foreach (var sr in srs) if (sr != null) sr.enabled = true;

        if (!collected) Collect(null);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (collected) return;

        PlatformPlayerController player = other.GetComponentInParent<PlatformPlayerController>();
        if (player == null) return;

        if (player == owner && !player.CanPickupDropped()) return;

        Collect(player);
    }
    private void Collect(PlatformPlayerController player)
    {
        if (collected) return;
        collected = true;
        StopAllCoroutines();

        if (triggerCol != null) triggerCol.enabled = false;

        if (player != null)
        {
            if (player.HasPowerUp())
            {
                Vector2 dir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                dir.y = Mathf.Max(dir.y, 0.4f);
                SpawnDropped(player, player.GetCurrentPowerUp(), dir);
                player.ClearPowerUpState();
            }
            player.ReceivePowerUp(storedType);
        }

        Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
    }

    private void SpawnDropped(PlatformPlayerController player, PowerUpPickup.PowerUpType type, Vector2 dir)
    {
        DroppedPowerUpSpawner spawner = Object.FindFirstObjectByType<DroppedPowerUpSpawner>();
        if (spawner == null) return;
        DroppedPowerUp.Spawn(spawner.Prefabs, type, player.transform.position, dir, player);
    }

    public static void Spawn(GameObject[] prefabsByType, PowerUpPickup.PowerUpType type,
                             Vector3 position, Vector2 launchDir,
                             PlatformPlayerController owner = null)
    {
        int index = (int)type;
        if (prefabsByType == null || index >= prefabsByType.Length || prefabsByType[index] == null)
        {
            Debug.LogWarning($"[DroppedPowerUp] No hay prefab para tipo {type} (índice {index})");
            return;
        }

        GameObject go = Instantiate(prefabsByType[index], position, Quaternion.identity);
        DroppedPowerUp dropped = go.GetComponentInChildren<DroppedPowerUp>();
        if (dropped != null)
            dropped.Initialize(type, launchDir, owner);
    }
}