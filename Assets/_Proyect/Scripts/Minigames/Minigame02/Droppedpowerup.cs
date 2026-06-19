using UnityEngine;
using System.Collections;

/// <summary>
/// Poner este script en cada prefab de esfera (uno por power up).
/// La esfera tiene un Rigidbody2D y un CircleCollider2D (NO trigger).
/// El icono es un SpriteRenderer hijo con el sprite del power up.
/// 
/// Cuando un jugador agarra un power up teniendo ya uno,
/// PowerUpPickup llama a DroppedPowerUp.Spawn() en lugar de descartarlo.
/// </summary>
public class DroppedPowerUp : MonoBehaviour
{
    [Header("Tiempo de vida")]
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float blinkStartTime = 2f; // cuándo antes de morir empieza a parpadear

    [Header("Físicas")]
    [SerializeField] private float launchForce = 6f;       // impulso inicial al salir del jugador
    [SerializeField] private PhysicsMaterial2D bounceMaterial;  // bounciness ~0.4, friction ~0.4

    private PowerUpPickup.PowerUpType storedType;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool collected = false;
    private float timeLeft;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // el SpriteRenderer del icono puede estar en un hijo
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Llamado desde PowerUpPickup justo después de instanciar la esfera.
    /// </summary>
    public void Initialize(PowerUpPickup.PowerUpType type, Vector2 launchDirection)
    {
        storedType = type;
        timeLeft = lifetime;

        if (rb != null)
        {
            if (bounceMaterial != null)
                rb.GetComponent<Collider2D>().sharedMaterial = bounceMaterial;

            rb.AddForce(launchDirection * launchForce, ForceMode2D.Impulse);
        }

        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        // esperar hasta que quede blinkStartTime
        float waitTime = lifetime - blinkStartTime;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        // parpadeo
        if (sr != null)
        {
            float elapsed = 0f;
            float blinkSpeed = 8f;
            while (elapsed < blinkStartTime)
            {
                elapsed += Time.deltaTime;
                // acelera el parpadeo al final
                blinkSpeed = Mathf.Lerp(8f, 20f, elapsed / blinkStartTime);
                sr.enabled = Mathf.FloorToInt(elapsed * blinkSpeed) % 2 == 0;
                yield return null;
            }
            sr.enabled = true;
        }

        if (!collected)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (collected) return;
        if (!col.gameObject.CompareTag("Player1") && !col.gameObject.CompareTag("Player2")) return;

        PlatformPlayerController player = col.gameObject.GetComponent<PlatformPlayerController>();
        if (player == null) return;

        collected = true;
        StopAllCoroutines();

        if (player.HasPowerUp())
        {
            // el jugador ya tiene uno — dropeamos el suyo y le damos este
            SpawnDropped(player, player.GetCurrentPowerUp());
        }

        player.ReceivePowerUp(storedType);
        Destroy(gameObject);
    }

    /// <summary>
    /// Instancia una esfera dropeada. Llamado desde PowerUpPickup y desde aquí mismo.
    /// prefabs: array con un prefab por tipo, en el mismo orden que el enum PowerUpType.
    /// </summary>
    public static void Spawn(GameObject[] prefabsByType, PowerUpPickup.PowerUpType type,
                             Vector3 position, Vector2 launchDir)
    {
        int index = (int)type;
        if (prefabsByType == null || index >= prefabsByType.Length || prefabsByType[index] == null)
        {
            Debug.LogWarning($"[DroppedPowerUp] No hay prefab para tipo {type} (índice {index})");
            return;
        }

        GameObject go = Instantiate(prefabsByType[index], position, Quaternion.identity);
        DroppedPowerUp dropped = go.GetComponent<DroppedPowerUp>();
        if (dropped != null)
            dropped.Initialize(type, launchDir);
    }

    // helper interno para dropear el power up actual del jugador
    private void SpawnDropped(PlatformPlayerController player, PowerUpPickup.PowerUpType type)
    {
        // busca el DroppedPowerUpSpawner en la escena para obtener los prefabs
        DroppedPowerUpSpawner spawner = Object.FindFirstObjectByType<DroppedPowerUpSpawner>();
        if (spawner == null) return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        dir.y = Mathf.Abs(dir.y) * 0.5f + 0.3f; // siempre sube un poco
        DroppedPowerUp.Spawn(spawner.Prefabs, type, player.transform.position, dir);
    }
}