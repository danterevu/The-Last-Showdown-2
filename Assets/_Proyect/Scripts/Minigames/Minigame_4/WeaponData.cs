using UnityEngine;


/// ScriptableObject con todas las stats de un arma.
/// Crear via: clic derecho en Project  Create  Weapons  WeaponData
/// Crear un asset por arma: Pistol_Data, Laser_Data, Shotgun_Data, Minigun_Data

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    public enum WeaponType { Pistol, Laser, Shotgun, Minigun }

    [Header("Identidad")]
    public WeaponType type;
    public string weaponName = "Arma";
    public Sprite icon;                  // ícono que aparece en el HUD

    [Header("Fire Points")]
    public int[] firePointIndexes;

    [Header("Projectile Prefabs")]
    public GameObject player1ProjectilePrefab;
    public GameObject player2ProjectilePrefab;
    public float projectileSpeed = 12f;
    public float damage          = 10f;

    [Header("Rango")]
    [Tooltip("Distancia en unidades antes de que el proyectil se destruya")]
    public float range = 10f;

    [Header("Munición")]
    public int maxAmmo = 30;


    [Header("Pistola - semi-automática")]
    [Tooltip("Delay mínimo entre disparos (evita spam de clicks)")]
    public float semiAutoDelay = 0.25f;

    [Header("Laser - carga y disparo")]
    [Tooltip("Segundos que hay que mantener el botón para cargar el tiro")]
    public float chargeTime = 1.5f;
    [Tooltip("Multiplicador de daño del laser respecto a damage base")]
    public float laserDamageMultiplier = 5f;
    [Tooltip("Multiplicador de tamaño del proyectil laser cargado")]
    public float laserSizeMultiplier = 3f;

    [Header("Escopeta - dispersión y knockback")]
    [Tooltip("Cantidad de proyectiles por disparo")]
    public int shotgunPellets = 6;
    [Tooltip("Ángulo total de dispersión en grados")]
    public float shotgunSpread = 30f;
    [Tooltip("Fuerza de knockback aplicada al jugador al disparar")]
    public float shotgunKnockback = 8f;
    [Tooltip("Delay entre disparos de escopeta")]
    public float shotgunFireRate = 0.6f;

    [Header("Metralleta - disparo automático")]
    [Tooltip("Disparos por segundo")]
    public float minigunshotsPerSecond = 12f;
}
