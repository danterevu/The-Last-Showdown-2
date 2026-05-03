using UnityEngine;

/// Va en el prefab de pickup de arma. Cuando una nave lo toca,
/// le entrega el arma y el objeto desaparece.
///
/// SETUP del prefab:
///   - SpriteRenderer (ícono del arma, puede usarse el WeaponData.icon)
///   - CircleCollider2D con Is Trigger = true
///   - Este script
///   - weaponData: arrastrar el ScriptableObject del arma correspondiente
///   
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;

    private WeaponSpawner spawner;
    private Transform     spawnPoint;

    /// Llamado por WeaponSpawner al instanciar este pickup.
    public void Init(WeaponData data, WeaponSpawner spawner, Transform spawnPoint)
    {
        this.weaponData  = data;
        this.spawner     = spawner;
        this.spawnPoint  = spawnPoint;

        // Mostrar el ícono del arma en el SpriteRenderer si existe
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && data.icon != null)
            sr.sprite = data.icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

        WeaponController wc = other.GetComponent<WeaponController>();
        if (wc == null) return;

        // Entregar el arma al jugador
        wc.EquipWeapon(weaponData);

        // Notificar al spawner para que respawnee el punto después de un delay
        spawner?.OnPickupCollected(spawnPoint);

        Destroy(gameObject);
    }
}
