using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;

    private WeaponSpawner spawner;
    private Transform spawnPoint;

    /// Llamado por WeaponSpawner al instanciar este pickup.
    public void Init(WeaponData data, WeaponSpawner spawner, Transform spawnPoint)
    {
        this.weaponData = data;
        this.spawner = spawner;
        this.spawnPoint = spawnPoint;

        // La pistola es el arma default, no debe aparecer como pickup
        // Si por error llega una pistola al spawner, destruir el pickup de inmediato
        if (data.type == WeaponData.WeaponType.Pistol)
        {
            spawner?.OnPickupCollected(spawnPoint);
            Destroy(gameObject);
            return;
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && data.icon != null)
            sr.sprite = data.icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

        WeaponController wc = other.GetComponent<WeaponController>();
        if (wc == null) return;

        wc.EquipWeapon(weaponData);
        spawner?.OnPickupCollected(spawnPoint);
        Destroy(gameObject);
    }
}