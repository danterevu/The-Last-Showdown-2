using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private bool isPlayer1 = true;
    [SerializeField] private int playerIndex = 0; // 0 = P1, 1 = P2 para mando

    [Header("Fire Points")]
    [Tooltip("Arreglo global de puntos de disparo de la nave. WeaponData referencia por índice.")]
    [SerializeField] private Transform[] allFirePoints;

    [Header("Arma base (sin pickup)")]
    [SerializeField] private WeaponData defaultWeapon;

    [Header("Animacion")]
    [SerializeField] private Animator shipAnimator;

    [Header("Railgun VFX")]
    [SerializeField] private SpriteRenderer shipSprite;
    [SerializeField] private float blinkSpeed = 0.08f;

    [Header("Slow Field Effect")]
    [SerializeField] private float slowFieldBulletSpeedMultiplier = 0.4f;

    private bool isFullyCharged;
    private Coroutine blinkRoutine;
    // ─────────────────────────────────────────────────────────
    // PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────────────────

    public WeaponData CurrentWeapon => currentWeapon;
    public int CurrentAmmo => currentAmmo;
    public float ChargeProgress => chargeProgress;

    public event Action<WeaponData, int> OnWeaponChanged;
    public event Action<int> OnAmmoChanged;
    public event Action<float> OnChargeChanged;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────

    private WeaponData currentWeapon;
    private int currentAmmo;
    private float fireCooldown;

    // Laser
    private float chargeProgress;
    private float chargeElapsed;

    // Input
    private SpaceShipController shipController;
    private InputAction shootAction;
    private bool shootHeld;
    private bool shootPressedThisFrame; // calculado una sola vez por frame

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (shipAnimator == null)
            shipAnimator = GetComponentInChildren<Animator>();


        shipController = GetComponent<SpaceShipController>();
        SetupInput();

        if (allFirePoints == null || allFirePoints.Length == 0)
            Debug.LogWarning("[WeaponController] No hay FirePoints asignados.");
        if (shipSprite == null)
            shipSprite = GetComponentInChildren<SpriteRenderer>();
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

        // Input calculado una única vez — evita bugs al cambiar de arma entre frames
        /*bool wasHeld = shootHeld;
        shootHeld = shootAction != null && shootAction.IsPressed();
        shootPressedThisFrame = shootHeld && !wasHeld;*/

        // Leer disparo desde teclado (asset) o gamepad (R2)
        bool wasHeld = shootHeld;
        bool keyboardShoot = shootAction != null && shootAction.IsPressed();
        bool gamepadShoot = false;

        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
            gamepadShoot = gp.rightTrigger.isPressed;

        shootHeld = keyboardShoot || gamepadShoot;
        shootPressedThisFrame = shootHeld && !wasHeld;

        switch (currentWeapon.type)
        {
            case WeaponData.WeaponType.Pistol: UpdatePistol(); break;
            case WeaponData.WeaponType.Laser: UpdateLaser(); break;
            case WeaponData.WeaponType.Minigun: UpdateMinigun(); break;
            case WeaponData.WeaponType.Shotgun: UpdateShotgun(); break;
        }
    }

    // ─────────────────────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────────────────────

    private void SetupInput()
    {
        if (inputActionAsset == null) return;

        string mapName = playerIndex == 0 ? "Player1_TopDown" : "Player2_TopDown";
        var map = inputActionAsset.FindActionMap(mapName);

        if (map == null)
        {
            Debug.LogError($"[WeaponController] No se encontró el mapa '{mapName}'");
            return;
        }

        shootAction = map.FindAction("Shoot");
        map.Enable();
    }

    // ─────────────────────────────────────────────────────────
    // EQUIPAR / SOLTAR ARMA
    // ─────────────────────────────────────────────────────────

    public void EquipWeapon(WeaponData data)
    {
        currentWeapon = data;
        currentAmmo = data.maxAmmo;
        chargeProgress = 0f;
        chargeElapsed = 0f;
        fireCooldown = 0f;

      
        shootHeld = false;
        shootPressedThisFrame = false;

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

    public void ResetToDefault()
    {
        ResetCharge();
        DropWeapon();
    }

    private void SpendAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
        OnAmmoChanged?.Invoke(currentAmmo);

        if (currentAmmo <= 0)
            DropWeapon();
    }

    // ─────────────────────────────────────────────────────────
    // ANIMACIONES
    // ─────────────────────────────────────────────────────────

    private void UpdateWeaponAnimator()
    {
        if (shipAnimator == null) return;

        int index = currentWeapon == null ? 0 : currentWeapon.type switch
        {
            WeaponData.WeaponType.Pistol => 0,
            WeaponData.WeaponType.Laser => 1,
            WeaponData.WeaponType.Shotgun => 2,
            WeaponData.WeaponType.Minigun => 3,
            _ => 0
        };

      
        shipAnimator.SetInteger("WeaponIndex", index);

      
        shipAnimator.SetTrigger("WeaponChanged");
    }
    private void TriggerShootAnim()
    {
        if (shipAnimator == null) return;
        shipAnimator.SetTrigger("Shoot");
    }

  

    // ─────────────────────────────────────────────────────────
    // UPDATE POR TIPO DE ARMA
    // ─────────────────────────────────────────────────────────

    private void UpdatePistol()
    {
        if (!shootPressedThisFrame || fireCooldown > 0f) return;

        FireFromWeaponPoints(currentWeapon.damage, currentWeapon.projectileSpeed, 1f);
        TriggerShootAnim();
        fireCooldown = currentWeapon.semiAutoDelay;
        SpendAmmo();
    }

    private void UpdateLaser()
    {
        // 1. CARGA
        if (shootHeld)
        {
            chargeElapsed += Time.deltaTime;

            chargeProgress = Mathf.Clamp01(
                chargeElapsed / currentWeapon.chargeTime
            );

            OnChargeChanged?.Invoke(chargeProgress);

            if (shipAnimator != null)
                shipAnimator.SetFloat("ChargeProgress", chargeProgress);

            // llegó a full charge
            if (chargeProgress >= 1f && !isFullyCharged)
            {
                isFullyCharged = true;
                StartChargedBlink();
            }
        }

        // 2. DISPARO (SOLO cuando YA está cargado y suelta el botón)
        if (isFullyCharged && !shootHeld)
        {
            FireFromWeaponPoints(
                currentWeapon.damage * currentWeapon.laserDamageMultiplier,
                currentWeapon.projectileSpeed * 1.5f,
                currentWeapon.laserSizeMultiplier
            );

            TriggerShootAnim();
            SpendAmmo();

            ResetCharge();
        }

        // 3. CANCELA SI SUELTA ANTES DE CARGAR
        if (!shootHeld && !isFullyCharged && chargeElapsed > 0f)
        {
            ResetCharge();
        }
    }
    private void StartChargedBlink()
    {
        if (blinkRoutine != null) return;

        blinkRoutine = StartCoroutine(BlinkCoroutine());
    }

    private void StopChargedBlink()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        if (shipSprite != null)
            shipSprite.color = Color.white;
    }

    private IEnumerator BlinkCoroutine()
    {
        while (true)
        {
            shipSprite.color = Color.white;
            yield return new WaitForSeconds(blinkSpeed);

            shipSprite.color = Color.cyan;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
    private void UpdateMinigun()
    {
        if (!shootHeld || fireCooldown > 0f) return;

        FireFromWeaponPoints(currentWeapon.damage, currentWeapon.projectileSpeed, 1f);
        TriggerShootAnim();
        fireCooldown = 1f / currentWeapon.minigunshotsPerSecond;
        SpendAmmo();
    }

    private void UpdateShotgun()
    {
        if (!shootPressedThisFrame || fireCooldown > 0f) return;

        FireShotgun();
        TriggerShootAnim();
        fireCooldown = currentWeapon.shotgunFireRate;
        SpendAmmo();
    }

    private void ResetCharge()
    {
        chargeElapsed = 0f;
        chargeProgress = 0f;
        isFullyCharged = false;

        StopChargedBlink();

        if (shipAnimator != null)
            shipAnimator.SetFloat("ChargeProgress", 0f);

        OnChargeChanged?.Invoke(0f);
    }

    // ─────────────────────────────────────────────────────────
    // FIRE
    // ─────────────────────────────────────────────────────────

    private void FireFromWeaponPoints(float dmg, float spd, float sizeMultiplier)
    {
        if (currentWeapon.firePointIndexes == null) return;

        foreach (int index in currentWeapon.firePointIndexes)
        {
            if (index < 0 || index >= allFirePoints.Length) continue;
            FireSingle(allFirePoints[index], dmg, spd, sizeMultiplier);
        }
    }

    private void FireSingle(Transform shootPoint, float dmg, float spd, float sizeMultiplier, Vector2? overrideDir = null)
    {
        if (shootPoint == null) return;

        GameObject prefab = isPlayer1
            ? currentWeapon.player1ProjectilePrefab
            : currentWeapon.player2ProjectilePrefab;

        if (prefab == null)
        {
            Debug.LogError($"[WeaponController] Falta el prefab de proyectil para {(isPlayer1 ? "Player1" : "Player2")} en {currentWeapon.weaponName}.");
            return;
        }

        Vector2 dir = overrideDir ?? (Vector2)shootPoint.right;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float finalSpeed = spd;
        if (shipController != null && shipController.isInSlowField)
        {
            finalSpeed *= slowFieldBulletSpeedMultiplier;
        }

        GameObject obj = Instantiate(prefab, shootPoint.position, Quaternion.Euler(0f, 0f, angle));

        if (!Mathf.Approximately(sizeMultiplier, 1f))
            obj.transform.localScale *= sizeMultiplier;

        Projectile proj = obj.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(dir, finalSpeed, dmg, currentWeapon.range, isPlayer1 ? 1 : 2, currentWeapon.type);
    }

    private void FireShotgun()
    {
        if (currentWeapon.firePointIndexes == null) return;

        foreach (int index in currentWeapon.firePointIndexes)
        {
            if (index < 0 || index >= allFirePoints.Length) continue;

            Transform fp = allFirePoints[index];
            Vector2 baseDir = fp.right;

            for (int i = 0; i < currentWeapon.shotgunPellets; i++)
            {
                float randomAngle = UnityEngine.Random.Range(
                    -currentWeapon.shotgunSpread * 0.5f,
                     currentWeapon.shotgunSpread * 0.5f
                );
                Vector2 dir = RotateVector(baseDir, randomAngle);
                FireSingle(fp, currentWeapon.damage, currentWeapon.projectileSpeed, 1f, dir);
            }
        }

        // Knockback al disparar la escopeta
        if (shipController != null)
            shipController.AddImpulse(-(Vector2)transform.right * currentWeapon.shotgunKnockback);
    }

    // ─────────────────────────────────────────────────────────
    // UTILS
    // ─────────────────────────────────────────────────────────

    private Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    public bool HasWeapon => currentWeapon != null;
    public int PlayerNumber => isPlayer1 ? 1 : 2;

    public Transform GetFirePoint(int index)
    {
        if (index < 0 || index >= allFirePoints.Length) return null;
        return allFirePoints[index];
    }
}
