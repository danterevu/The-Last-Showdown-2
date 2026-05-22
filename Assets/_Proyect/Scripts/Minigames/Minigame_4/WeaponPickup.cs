using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;

    private WeaponSpawner spawner;
    private Transform spawnPoint;

    // Para evitar doble trigger raro
    private bool collected;

    /// Llamado por WeaponSpawner al instanciar este pickup
    public void Init(WeaponData data, WeaponSpawner spawner, Transform spawnPoint)
    {
        this.weaponData = data;
        this.spawner = spawner;
        this.spawnPoint = spawnPoint;

        // Nunca spawnear la pistola default
        if (weaponData != null &&
            weaponData.type == WeaponData.WeaponType.Pistol)
        {
            spawner?.OnPickupCollected(spawnPoint);
            Destroy(gameObject);
            return;
        }

        // Cambiar sprite visual
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null &&
            weaponData != null &&
            weaponData.icon != null)
        {
            sr.sprite = weaponData.icon;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        // Detectar jugadores
        if (!other.CompareTag("Player1") &&
            !other.CompareTag("Player2"))
            return;

        WeaponController wc = other.GetComponent<WeaponController>();

        if (wc == null || weaponData == null)
            return;

        collected = true;

        // Equipar arma
        wc.EquipWeapon(weaponData);

        // Avisar al spawner
        spawner?.OnPickupCollected(spawnPoint);

        Destroy(gameObject);
    }
}