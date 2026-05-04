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

    [Header("Animacion")]
    [SerializeField] private Animator shipAnimator;

    public WeaponData CurrentWeapon => currentWeapon;
    public int CurrentAmmo => currentAmmo;
    public float ChargeProgress => chargeProgress;

    public event Action<WeaponData, int> OnWeaponChanged;
    public event Action<int> OnAmmoChanged;
    public event Action<float> OnChargeChanged;

    private WeaponData currentWeapon;
    private int currentAmmo;
    private float fireCooldown;

    private float chargeProgress;
    private float chargeElapsed;

    private SpaceShipController shipController;
    private InputAction shootAction;
    private bool shootHeld;
    private bool wasPressedLastFrame;

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
            case WeaponData.WeaponType.Minigun: UpdateMinigun(); break;
            case WeaponData.WeaponType.Shotgun: UpdateShotgun(); break;
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

    // ─── ARMAS ───────────────────────────────────────────────────────────────

    public void EquipWeapon(WeaponData data)
    {
        currentWeapon = data;
        currentAmmo = data.maxAmmo;
        chargeProgress = 0f;
        chargeElapsed = 0f;
        fireCooldown = 0f;

        UpdateWeaponAnimator();
        OnWeaponChanged?.Invoke(currentWeapon, currentAmmo);
    }


    private void DropWeapon()
    {
        if (defaultWeapon != null)
            EquipWeapon(defaultWeapon);
        else
        {
            currentWeapon = null;
            UpdateWeaponAnimator();
            OnWeaponChanged?.Invoke(null, 0);
        }
    }

    private void SpendAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
        OnAmmoChanged?.Invoke(currentAmmo);
        if (currentAmmo <= 0) DropWeapon();
    }

    // ─── ANIMACIONES ─────────────────────────────────────────────────────────

    /// Setea los booleanos del Animator según el arma equipada.
    private void UpdateWeaponAnimator()
    {
        if (shipAnimator == null) return;

        bool isSniperOrLaser = currentWeapon != null &&
                               currentWeapon.type == WeaponData.WeaponType.Laser;

        bool isRailgun = currentWeapon != null &&
                         currentWeapon.type == WeaponData.WeaponType.Minigun;

        // SniperWeapon = true solo con Laser, RailgunWeapon = true solo con Minigun
        // Si es Pistol o null, ambos son false (arma default)
        shipAnimator.SetBool("SniperWeapon", isSniperOrLaser);
        shipAnimator.SetBool("RailgunWeapon", isRailgun);
    }

    /// Dispara el trigger Shoot por un frame.
    private void TriggerShootAnim()
    {
        if (shipAnimator == null) return;
        shipAnimator.SetBool("Shoot", true);
        StartCoroutine(ResetShootAnim());
    }

    private IEnumerator ResetShootAnim()
    {
        yield return null; // espera un frame
        if (shipAnimator != null)
            shipAnimator.SetBool("Shoot", false);
    }

  

    private void UpdatePistol()
    {
        bool pressedThisFrame = shootHeld && !wasPressedLastFrame;
        wasPressedLastFrame = shootHeld;

        if (pressedThisFrame && fireCooldown <= 0f)
        {
            FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f);
            TriggerShootAnim();
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
            TriggerShootAnim();
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

    private void UpdateMinigun()
    {
        if (shootHeld && fireCooldown <= 0f)
        {
            FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f);
            TriggerShootAnim();
            fireCooldown = 1f / currentWeapon.minigunshotsPerSecond;
            SpendAmmo();
        }
    }

    private void UpdateShotgun()
    {
        bool pressedThisFrame = shootHeld && !wasPressedLastFrame;
        wasPressedLastFrame = shootHeld;

        if (pressedThisFrame && fireCooldown <= 0f)
        {
            FireShotgun();
            TriggerShootAnim();
            fireCooldown = currentWeapon.shotgunFireRate;
            SpendAmmo();
        }
    }

    // ─── FIRE ────────────────────────────────────────────────────────────────

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

    private void FireShotgun()
    {
        Vector2 baseDir = transform.right;

        // 3 balas: central, diagonal izquierda, diagonal derecha
        float half = currentWeapon.shotgunSpread / 2f;
        float[] angles = { 0f, -half, half };

        foreach (float angle in angles)
        {
            Vector2 dir = RotateVector(baseDir, angle);
            FireSingle(currentWeapon.damage, currentWeapon.projectileSpeed, 1f, dir);
        }

        // Knockback: empuja la nave hacia atrás
        if (shipController != null)
            shipController.AddImpulse(-baseDir * currentWeapon.shotgunKnockback);
    }

    private Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ─── UTILS ───────────────────────────────────────────────────────────────

    public bool HasWeapon => currentWeapon != null;
    public int PlayerNumber => isPlayer1 ? 1 : 2;
}