using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// Va en el mismo GameObject que SpaceShipController.
/// Lee la acción "Shoot" del mapa TopDown del jugador correspondiente.
/// Maneja los 4 modos de disparo y la munición.
///
/// SETUP:
///   - inputActionAsset: mismo asset que SpaceShipController (1 o 2)
///   - isPlayer1: mismo valor que SpaceShipController
///   - firePoint: Transform hijo vacío en la punta de la nave
///   - La nave base (sin arma) se asigna en defaultWeapon (puede ser null)
public class WeaponController : MonoBehaviour
{
   

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private bool isPlayer1 = true;

    [Header("Disparo")]
    [SerializeField] private Transform firePoint; // punta de la nave

    [Header("Arma base (sin pickup)")]
    [SerializeField] private WeaponData defaultWeapon; // pistola débil por defecto, puede ser null

    

    public WeaponData CurrentWeapon => currentWeapon;
    public int        CurrentAmmo   => currentAmmo;
    public float      ChargeProgress => chargeProgress; // 0..1 para la barra del laser

    // Eventos para el HUD
    public event Action<WeaponData, int> OnWeaponChanged; // arma, ammo
    public event Action<int>             OnAmmoChanged;    // ammo actual
    public event Action<float>           OnChargeChanged;  // 0..1

   

    private WeaponData currentWeapon;
    private int        currentAmmo;

    // control de cadencia
    private float fireCooldown;

    // laser
    private bool  isCharging;
    private float chargeProgress; // 0..1
    private float chargeElapsed;

    // referencia al SpaceShipController para el knockback de escopeta
    private SpaceShipController shipController;

    // input
    private InputAction shootAction;
    private bool shootHeld;

   

    private void Awake()
    {
        shipController = GetComponent<SpaceShipController>();
        SetupInput();
    }

    private void Start()
    {
        if (defaultWeapon != null)
            EquipWeapon(defaultWeapon);
    }

    private void Update()
    {
        if (currentWeapon == null) return;

        fireCooldown -= Time.deltaTime;
        shootHeld = shootAction != null && shootAction.IsPressed();

        switch (currentWeapon.type)
        {
            case WeaponData.WeaponType.Pistol:    UpdatePistol();    break;
            case WeaponData.WeaponType.Laser:     UpdateLaser();     break;
            case WeaponData.WeaponType.Shotgun:   UpdateShotgun();   break;
            case WeaponData.WeaponType.Minigun:   UpdateMinigun();   break;
        }
    }

   
    private void SetupInput()
    {
        if (inputActionAsset == null) return;

        // Shoot está en el mapa TopDown
        string mapName = isPlayer1 ? "Player1_TopDown" : "Player2_TopDown";
        var map = inputActionAsset.FindActionMap(mapName);
        if (map == null) { Debug.LogError($"[WeaponController] No se encontró el mapa '{mapName}'"); return; }

        shootAction = map.FindAction("Shoot");
        map.Enable();
    }

    

   
    public void EquipWeapon(WeaponData data)
    {
        currentWeapon = data;
        currentAmmo   = data.maxAmmo;
        isCharging    = false;
        chargeProgress = 0f;
        fireCooldown   = 0f;
        OnWeaponChanged?.Invoke(currentWeapon, currentAmmo);
    }

    private void DropWeapon()
    {
        // Si hay arma base, volver a ella; si no, quedar sin arma
        if (defaultWeapon != null)
            EquipWeapon(defaultWeapon);
        else
        {
            currentWeapon = null;
            OnWeaponChanged?.Invoke(null, 0);
        }
    }

    private void SpendAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
        OnAmmoChanged?.Invoke(currentAmmo);
        if (currentAmmo <= 0) DropWeapon();
    }

    // El jugador puede hacer click rápido pero hay un cooldown mínimo.
    // "Semi-auto" = un disparo por presión, no mantener.

    private bool wasPressedLastFrame;

    private void UpdatePistol()
    {
        bool pressedThisFrame = shootHeld && !wasPressedLastFrame;
        wasPressedLastFrame = shootHeld;

        if (pressedThisFrame && fireCooldown <= 0f)
        {
            FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f);
            fireCooldown = currentWeapon.semiAutoDelay;
            SpendAmmo();
        }
    }

    
    // Mientras se mantiene el botón, chargeProgress sube de 0 a 1.
    // Al soltar, si está cargado al 100%, dispara un proyectil grande.
    // Si suelta antes de cargar, no pasa nada (cancelado).

    private void UpdateLaser()
    {
        if (shootHeld)
        {
            // Acumular carga
            chargeElapsed += Time.deltaTime;
            chargeProgress = Mathf.Clamp01(chargeElapsed / currentWeapon.chargeTime);
            OnChargeChanged?.Invoke(chargeProgress);
        }
        else if (chargeProgress >= 1f)
        {
            // Soltar con carga completa → disparar
            FireSingle(
                currentWeapon.damage * currentWeapon.laserDamageMultiplier,
                currentWeapon.projectileSpeed * 1.5f,
                currentWeapon.laserSizeMultiplier
            );
            SpendAmmo();
            ResetCharge();
        }
        else if (chargeElapsed > 0f)
        {
            // Soltó antes de cargar → cancelar silenciosamente
            ResetCharge();
        }
    }

    private void ResetCharge()
    {
        chargeElapsed  = 0f;
        chargeProgress = 0f;
        OnChargeChanged?.Invoke(0f);
    }

   
    // Dispara N pellets en un cono. Empuja la nave hacia atrás.

    private void UpdateShotgun()
    {
        bool pressedThisFrame = shootHeld && !wasPressedLastFrame;
        wasPressedLastFrame = shootHeld;

        if (pressedThisFrame && fireCooldown <= 0f)
        {
            FireShotgunBurst();
            ApplyShotgunKnockback();
            fireCooldown = currentWeapon.shotgunFireRate;
            SpendAmmo();
        }
    }

    private void FireShotgunBurst()
    {
        int    pellets   = currentWeapon.shotgunPellets;
        float  spread    = currentWeapon.shotgunSpread;
        float  halfSpread = spread / 2f;

        // Dirección base = hacia donde mira la nave (transform.up si el sprite mira arriba)
        Vector2 baseDir = transform.up;
        float   baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < pellets; i++)
        {
            // Distribuir los pellets uniformemente en el arco de dispersión
            float t = pellets == 1 ? 0f : (float)i / (pellets - 1);
            float angle = baseAngle + Mathf.Lerp(-halfSpread, halfSpread, t);
            float rad   = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f, dir);
        }
    }

    private void ApplyShotgunKnockback()
    {
        // Empujar la nave en dirección contraria a donde mira
        Vector2 kickDir = -(Vector2)transform.up;
        shipController?.AddImpulse(kickDir * currentWeapon.shotgunKnockback);
    }

   
    private void UpdateMinigun()
    {
        if (shootHeld && fireCooldown <= 0f)
        {
            FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f);
            fireCooldown = 1f / currentWeapon.minigunshotsPerSecond;
            SpendAmmo();
        }
    }

   

    
    private void FireSingle(float dmg, float spd, float sizeMultiplier, Vector2? overrideDir = null)
    {
        if (currentWeapon.projectilePrefab == null) return;

        Vector2 dir = overrideDir ?? (Vector2)transform.up;

        GameObject obj = Instantiate(
            currentWeapon.projectilePrefab,
            firePoint != null ? firePoint.position : transform.position,
            Quaternion.identity
        );

        // Escalar el proyectil (útil para el laser)
        if (!Mathf.Approximately(sizeMultiplier, 1f))
            obj.transform.localScale *= sizeMultiplier;

        Projectile proj = obj.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(dir, spd, dmg, currentWeapon.range, isPlayer1 ? 1 : 2);
    }

   
    public bool HasWeapon => currentWeapon != null;
    public int  PlayerNumber => isPlayer1 ? 1 : 2;
}
