using UnityEngine;
using System.Collections;

public class DNADroppedPowerUp : MonoBehaviour
{
    [Header("Tiempo de vida")]
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float blinkStartTime = 2f;

    [Header("Físicas")]
    [SerializeField] private float launchForce = 4f;

    [Header("Cooldown")]
    [SerializeField] private float pickupDelay = 1f;

    private DNAPowerUpPickup.DNAPowerUpType storedType;
    private PlayerControllerDNA owner;
    private Rigidbody2D rb;
    private Collider2D triggerCol;
    private SpriteRenderer[] srs;
    private bool collected = false;
    private bool canOwnerPickup = false;

    private void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        srs = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Initialize(DNAPowerUpPickup.DNAPowerUpType type, Vector2 launchDirection,
                           PlayerControllerDNA ownerPlayer = null)
    {
        storedType = type;
        owner = ownerPlayer;

        if (rb != null)
            rb.AddForce(launchDirection.normalized * launchForce, ForceMode2D.Impulse);

        StartCoroutine(PickupDelayRoutine());
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator PickupDelayRoutine()
    {
        canOwnerPickup = false;
        yield return new WaitForSeconds(pickupDelay);
        canOwnerPickup = true;
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

        PlayerControllerDNA player = other.GetComponentInParent<PlayerControllerDNA>();
        if (player == null) player = other.GetComponent<PlayerControllerDNA>();
        if (player == null) return;

        // Bloquear al owner por pickupDelay
        if (player == owner && !canOwnerPickup) return;

        Collect(player);
    }

    private void Collect(PlayerControllerDNA player)
    {
        if (collected) return;
        collected = true;
        StopAllCoroutines();

        if (triggerCol != null) triggerCol.enabled = false;

        if (player != null)
        {
            // Si ya tiene un powerup, soltarlo como esfera antes de agarrar este
            if (player.HasPowerUp())
            {
                Vector2 dir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                dir.y = Mathf.Max(dir.y, 0.4f);
                SpawnDropped(player, player.GetCurrentPowerUp(), dir);
                player.ClearPowerUpState();
            }
            player.ReceiveDNAPowerUp(storedType);
        }

        Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
    }

    private void SpawnDropped(PlayerControllerDNA player, DNAPowerUpPickup.DNAPowerUpType type, Vector2 dir)
    {
        DNADroppedPowerUpSpawner spawner = Object.FindFirstObjectByType<DNADroppedPowerUpSpawner>();
        if (spawner == null) return;
        DNADroppedPowerUp.Spawn(spawner.Prefabs, type, player.transform.position, dir, player);
    }

    public static void Spawn(GameObject[] prefabsByType, DNAPowerUpPickup.DNAPowerUpType type,
                             Vector3 position, Vector2 launchDir,
                             PlayerControllerDNA owner = null)
    {
        int index = GetPrefabIndex(type);
        if (index < 0)
        {
            Debug.LogWarning($"[DNADroppedPowerUp] Tipo {type} no tiene esfera (RemoteControl excluido)");
            return;
        }
        if (prefabsByType == null || index >= prefabsByType.Length || prefabsByType[index] == null)
        {
            Debug.LogWarning($"[DNADroppedPowerUp] No hay prefab para tipo {type} (índice {index})");
            return;
        }

        GameObject go = Instantiate(prefabsByType[index], position, Quaternion.identity);
        DNADroppedPowerUp dropped = go.GetComponentInChildren<DNADroppedPowerUp>();
        if (dropped == null) dropped = go.GetComponent<DNADroppedPowerUp>();
        if (dropped != null)
            dropped.Initialize(type, launchDir, owner);
    }

    // Mapeo del enum al índice de prefab (RemoteControl excluido = índice -1)
    private static int GetPrefabIndex(DNAPowerUpPickup.DNAPowerUpType type)
    {
        switch (type)
        {
            case DNAPowerUpPickup.DNAPowerUpType.Shrink: return 0;
            case DNAPowerUpPickup.DNAPowerUpType.Mine: return 1;
            case DNAPowerUpPickup.DNAPowerUpType.Berserk: return 2;
            case DNAPowerUpPickup.DNAPowerUpType.SlimeShot: return 3;
            case DNAPowerUpPickup.DNAPowerUpType.Shield: return 4;
            case DNAPowerUpPickup.DNAPowerUpType.RemoteControl: return -1; // no tiene esfera
            default: return -1;
        }
    }
}