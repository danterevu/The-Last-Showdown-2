using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class WeaponController : MonoBehaviour
{
   
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private bool isPlayer1 = true;

    [Header("Disparo")]
    [SerializeField] private Transform firePoint;

    [Header("Arma base (sin pickup)")]
    [SerializeField] private WeaponData defaultWeapon;

   
    public WeaponData CurrentWeapon => currentWeapon;
    public int CurrentAmmo => currentAmmo;
    public float ChargeProgress => chargeProgress;

    public event Action<WeaponData, int> OnWeaponChanged;
    public event Action<int> OnAmmoChanged;
    public event Action<float> OnChargeChanged;

   

    private WeaponData currentWeapon;
    private int currentAmmo;
    private float fireCooldown;

    // laser
    private float chargeProgress;
    private float chargeElapsed;

    private SpaceShipController shipController;

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
            case WeaponData.WeaponType.Pistol: UpdatePistol(); break;
            case WeaponData.WeaponType.Laser: UpdateLaser(); break;
            // case WeaponData.WeaponType.Shotgun: UpdateShotgun(); break;  // TODO: revisar spawn de pellets
            case WeaponData.WeaponType.Minigun: UpdateMinigun(); break;
        }
    }

   
    private void SetupInput()
    {
        if (inputActionAsset == null) return;

        string mapName = isPlayer1 ? "Player1_TopDown" : "Player2_TopDown";
        var map = inputActionAsset.FindActionMap(mapName);
        if (map == null) { Debug.LogError($"[WeaponController] No se encontro el mapa '{mapName}'"); return; }

        shootAction = map.FindAction("Shoot");
        map.Enable();
    }

   
    public void EquipWeapon(WeaponData data)
    {
        currentWeapon = data;
        currentAmmo = data.maxAmmo;
        chargeProgress = 0f;
        chargeElapsed = 0f;
        fireCooldown = 0f;
        OnWeaponChanged?.Invoke(currentWeapon, currentAmmo);
    }

    private void DropWeapon()
    {
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

   

    private void UpdateLaser()
    {
        if (shootHeld)
        {
            chargeElapsed += Time.deltaTime;
            chargeProgress = Mathf.Clamp01(chargeElapsed / currentWeapon.chargeTime);
            OnChargeChanged?.Invoke(chargeProgress);
        }
        else if (chargeProgress >= 1f)
        {
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
            ResetCharge();
        }
    }

    private void ResetCharge()
    {
        chargeElapsed = 0f;
        chargeProgress = 0f;
        OnChargeChanged?.Invoke(0f);
    }

   

    // private void UpdateShotgun()
    // {
    //     bool pressedThisFrame = shootHeld && !wasPressedLastFrame;
    //     wasPressedLastFrame = shootHeld;
    //
    //     if (pressedThisFrame && fireCooldown <= 0f)
    //     {
    //         FireShotgunBurst();
    //         ApplyShotgunKnockback();
    //         fireCooldown = currentWeapon.shotgunFireRate;
    //         SpendAmmo();
    //     }
    // }

    // private void FireShotgunBurst()
    // {
    //     int   pellets    = currentWeapon.shotgunPellets;
    //     float spread     = currentWeapon.shotgunSpread;
    //     float halfSpread = spread / 2f;
    //
    //     Vector2 baseDir   = transform.right;
    //     float   baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
    //
    //     for (int i = 0; i < pellets; i++)
    //     {
    //         float t     = pellets == 1 ? 0f : (float)i / (pellets - 1);
    //         float angle = baseAngle + Mathf.Lerp(-halfSpread, halfSpread, t);
    //         float rad   = angle * Mathf.Deg2Rad;
    //         Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    //         FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f, dir);
    //     }
    // }

    // private void ApplyShotgunKnockback()
    // {
    //     Vector2 kickDir = -(Vector2)transform.right;
    //     shipController?.AddImpulse(kickDir * currentWeapon.shotgunKnockback);
    // }

   

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

      
        Vector2 dir = overrideDir ?? (Vector2)transform.right;

       
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion bulletRot = Quaternion.Euler(0f, 0f, angle);

        GameObject obj = Instantiate(
            currentWeapon.projectilePrefab,
            firePoint != null ? firePoint.position : transform.position,
            bulletRot
        );

        if (!Mathf.Approximately(sizeMultiplier, 1f))
            obj.transform.localScale *= sizeMultiplier;

        Projectile proj = obj.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(dir, spd, dmg, currentWeapon.range, isPlayer1 ? 1 : 2);
    }

    

    public bool HasWeapon => currentWeapon != null;
    public int PlayerNumber => isPlayer1 ? 1 : 2;
}
