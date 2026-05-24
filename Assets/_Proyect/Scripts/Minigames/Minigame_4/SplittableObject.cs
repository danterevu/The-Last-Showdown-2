using UnityEngine;

public class SplittableObject : MonoBehaviour
{
    [Header("Prefabs de los fragmentos")]
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 2;

    [Header("Fuerza de lanzamiento")]
    [SerializeField] private float launchForce = 5f;

    [Header("Colision")]
    [SerializeField] private bool splitOnBulletCollision = true;
    [SerializeField] private LayerMask bulletLayer;
    [SerializeField] private bool destroyBulletOnHit = true;

    [Header("Opcional: Destruir original al partir")]
    [SerializeField] private bool destroyOriginal = true;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public void Split()
    {
        Split(Vector2.right);
    }

    public void Split(Vector2 hitDirection)
    {
        if (fragmentPrefab == null)
        {
            Debug.LogWarning("[SplittableObject] No hay prefab de fragmento asignado!", this);
            if (destroyOriginal) Destroy(gameObject);
            return;
        }

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                Vector2 randomDir = (hitDirection + Random.insideUnitCircle).normalized;
                rb.AddForce(randomDir * launchForce, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-180f, 180f));
            }
        }

        if (destroyOriginal)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject, collision.contacts[0].normal);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject, (other.transform.position - transform.position).normalized);
    }

    private void HandleCollision(GameObject other, Vector2 hitDirection)
    {
        if (!splitOnBulletCollision) return;

        // Verificar por layer
        if (((1 << other.layer) & bulletLayer) != 0)
        {
            SplitAndDestroyBullet(other, hitDirection);
            return;
        }

        // Verificar por componente Projectile
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            SplitAndDestroyBullet(other, hitDirection);
            return;
        }

        // Verificar por componente HomingMissile
        HomingMissile homingMissile = other.GetComponent<HomingMissile>();
        if (homingMissile != null)
        {
            SplitAndDestroyBullet(other, hitDirection);
        }
    }

    private void SplitAndDestroyBullet(GameObject bullet, Vector2 hitDirection)
    {
        Split(hitDirection);

        if (destroyBulletOnHit)
        {
            Destroy(bullet);
        }
    }
}
