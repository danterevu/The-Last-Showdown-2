using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceLaserTurret : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject stunProjectilePrefab;
    [SerializeField] private SpriteRenderer turretSpriteRenderer;
    [SerializeField] private SpriteRenderer headSpriteRenderer;

    [Header("Sprites de estado")]
    [Tooltip("Sprite normal del cuerpo de la torreta")]
    [SerializeField] private Sprite spriteNormal;
    [Tooltip("Sprite cuando la torreta esta destruida/danada")]
    [SerializeField] private Sprite spriteDamaged;

    [Header("Targeting")]
    [SerializeField] private float range = 12f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float targetFollowSmoothTime = 0.12f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private Vector2 headAimLocalAxis = Vector2.right;

    [Header("Rotation Limits")]
    [SerializeField] private float minRotation = -90f;
    [SerializeField] private float maxRotation = 90f;

    [Header("Firing")]
    [SerializeField] private float fireCooldown = 1.25f;
    [SerializeField] private float preFireDelay = 0.15f;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 30f;
    [Tooltip("Dano que recibe por cada bala")]
    [SerializeField] private float damagePerHit = 10f;
    [Tooltip("Segundos sin recibir dano antes de empezar a regenerar")]
    [SerializeField] private float regenDelay = 4f;
    [Tooltip("Vida regenerada por segundo")]
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private int pointsOnDestroy = 5;

    [Header("Barra de vida")]
    [Tooltip("Offset en Y respecto al centro del cañon")]
    [SerializeField] private float healthBarOffsetY = 1.2f;
    [Tooltip("Ancho total de la barra en unidades de mundo")]
    [SerializeField] private float healthBarWidth = 1.5f;
    [Tooltip("Alto de la barra en unidades de mundo")]
    [SerializeField] private float healthBarHeight = 0.18f;
    [SerializeField] private Color healthBarColorFull = Color.green;
    [SerializeField] private Color healthBarColorLow = Color.red;
    [Tooltip("Porcentaje al que la barra cambia a color 'low'")]
    [SerializeField] private float lowHealthThreshold = 0.35f;
    [Tooltip("Color del fondo de la barra")]
    [SerializeField] private Color healthBarBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Header("Damage Effects")]
    [SerializeField] private float vibrationIntensity = 0.1f;
    [SerializeField] private float vibrationDuration = 0.2f;
    [SerializeField] private float blinkSpeed = 0.1f;
    [SerializeField] private Color blinkColor = Color.red;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private float _currentHealth;

    // --- Estado interno ---
    private float _health;
    private bool _isDestroyed = false;
    private bool _isInvincible = false;

    private float _lastDamageTime = -999f;
    private Coroutine _regenCoroutine;
    private Coroutine _damageEffectRoutine;
    private Coroutine _fireRoutine;

    // --- Targeting ---
    private float _nextFireTime;
    private List<Transform> _targetsInRange = new List<Transform>();
    private Transform _currentTarget;
    private Vector3 _smoothedTargetPosition;
    private Vector3 _targetSmoothVelocity;
    private bool _hasSmoothedTarget;
    private float _headTurnVelocity;

    // --- Efectos ---
    private Vector3 _originalPosition;
    private Color _originalTurretColor;
    private Color _originalHeadColor;

    // --- Barra de vida (world space, generada por codigo) ---
    private GameObject _healthBarRoot;
    private SpriteRenderer _healthBarBg;
    private SpriteRenderer _healthBarFill;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        _health = Mathf.Max(1f, maxHealth);
        _currentHealth = _health;
        _originalPosition = transform.position;

        if (turretSpriteRenderer == null)
            turretSpriteRenderer = GetComponent<SpriteRenderer>();
        if (headSpriteRenderer == null && head != null)
            headSpriteRenderer = head.GetComponent<SpriteRenderer>();

        if (turretSpriteRenderer != null)
            _originalTurretColor = turretSpriteRenderer.color;
        if (headSpriteRenderer != null)
            _originalHeadColor = headSpriteRenderer.color;
    }

    private void Start()
    {
        CreateHealthBar();
        UpdateHealthBar();
    }

    // -------------------------------------------------------------------------
    //  BARRA DE VIDA (world space, hija del cañon)
    // -------------------------------------------------------------------------

    private void CreateHealthBar()
    {
        _healthBarRoot = new GameObject("HealthBar");
        _healthBarRoot.transform.SetParent(transform, false);
        _healthBarRoot.transform.localPosition = new Vector3(0f, healthBarOffsetY, 0f);

        // Fondo
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(_healthBarRoot.transform, false);
        bgGO.transform.localPosition = Vector3.zero;
        _healthBarBg = bgGO.AddComponent<SpriteRenderer>();
        _healthBarBg.sprite = CreateWhiteSprite();
        _healthBarBg.color = healthBarBgColor;
        _healthBarBg.sortingOrder = 10;
        bgGO.transform.localScale = new Vector3(healthBarWidth, healthBarHeight, 1f);

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(_healthBarRoot.transform, false);
        fillGO.transform.localPosition = new Vector3(-healthBarWidth * 0.5f, 0f, 0f);
        _healthBarFill = fillGO.AddComponent<SpriteRenderer>();
        _healthBarFill.sprite = CreateWhiteSprite();
        _healthBarFill.color = healthBarColorFull;
        _healthBarFill.sortingOrder = 11;
        fillGO.transform.localScale = new Vector3(healthBarWidth, healthBarHeight * 0.8f, 1f);

        // El pivot del fill esta en el centro del sprite, lo movemos para que escale desde la izquierda
        // usando un GO hijo con offset
        fillGO.transform.localPosition = new Vector3(0f, 0f, 0f);

        // Rehacer con pivot correcto: usamos un contenedor
        Destroy(fillGO);

        GameObject fillContainer = new GameObject("FillContainer");
        fillContainer.transform.SetParent(_healthBarRoot.transform, false);
        fillContainer.transform.localPosition = new Vector3(-healthBarWidth * 0.5f, 0f, 0f);

        GameObject fillInner = new GameObject("FillInner");
        fillInner.transform.SetParent(fillContainer.transform, false);
        fillInner.transform.localPosition = new Vector3(healthBarWidth * 0.5f, 0f, 0f);
        _healthBarFill = fillInner.AddComponent<SpriteRenderer>();
        _healthBarFill.sprite = CreateWhiteSprite();
        _healthBarFill.color = healthBarColorFull;
        _healthBarFill.sortingOrder = 11;
        fillInner.transform.localScale = new Vector3(healthBarWidth, healthBarHeight * 0.8f, 1f);

        // Guardamos la referencia al container para escalar
        _healthBarFill.transform.parent.gameObject.name = "FillContainer";
    }

    private void UpdateHealthBar()
    {
        if (_healthBarFill == null) return;

        float ratio = Mathf.Clamp01(_health / maxHealth);

        // Escalar el contenedor del fill en X
        Transform container = _healthBarFill.transform.parent;
        if (container != null)
            container.localScale = new Vector3(ratio, 1f, 1f);

        // Color segun vida
        _healthBarFill.color = ratio <= lowHealthThreshold ? healthBarColorLow : healthBarColorFull;

        // Ocultar barra si esta llena
        _healthBarRoot.SetActive(_health < maxHealth);
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    // -------------------------------------------------------------------------
    //  TARGETING
    // -------------------------------------------------------------------------

    public void AddTarget(Transform target)
    {
        if (!_targetsInRange.Contains(target))
            _targetsInRange.Add(target);
    }

    public void RemoveTarget(Transform target)
    {
        _targetsInRange.Remove(target);
    }

    private void Update()
    {
        // Si esta destruida no apunta ni dispara
        if (_isDestroyed) return;

        _currentTarget = FindBestTarget();
        if (_currentTarget == null) { _hasSmoothedTarget = false; return; }

        Vector3 targetPos = _currentTarget.position;
        if (!_hasSmoothedTarget) { _smoothedTargetPosition = targetPos; _hasSmoothedTarget = true; }
        else if (targetFollowSmoothTime > 0f)
            _smoothedTargetPosition = Vector3.SmoothDamp(_smoothedTargetPosition, targetPos, ref _targetSmoothVelocity, targetFollowSmoothTime);
        else
            _smoothedTargetPosition = targetPos;

        RotateHeadTowards(_smoothedTargetPosition);

        if (Time.time < _nextFireTime) return;
        if (!HasLineOfSightTo(_smoothedTargetPosition)) return;

        _nextFireTime = Time.time + Mathf.Max(0.01f, fireCooldown);

        if (_fireRoutine != null) StopCoroutine(_fireRoutine);
        _fireRoutine = StartCoroutine(FireSequenceRoutine());
    }

    private Transform FindBestTarget()
    {
        _targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (Transform target in _targetsInRange)
        {
            float sqr = ((Vector2)(target.position - transform.position)).sqrMagnitude;
            if (sqr <= range * range && sqr < bestSqr) { best = target; bestSqr = sqr; }
        }

        return best;
    }

    private void RotateHeadTowards(Vector3 worldTarget)
    {
        if (head == null) return;
        Vector2 dir = (worldTarget - head.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector2 axis = headAimLocalAxis.sqrMagnitude > 0.0001f ? headAimLocalAxis.normalized : Vector2.right;
        float axisAngle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Clamp(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - axisAngle, minRotation, maxRotation);

        float currentAngle = Mathf.Repeat(head.eulerAngles.z + 180f, 360f) - 180f;
        float newAngle;
        if (targetFollowSmoothTime > 0f)
            newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref _headTurnVelocity, targetFollowSmoothTime, Mathf.Max(0f, turnSpeed), Time.deltaTime);
        else
            newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, Mathf.Max(0f, turnSpeed) * Time.deltaTime);

        head.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(newAngle, minRotation, maxRotation));
    }

    private Vector2 GetWorldAimDirection()
    {
        if (head == null) return Vector2.right;
        Vector2 axis = headAimLocalAxis.sqrMagnitude > 0.0001f ? headAimLocalAxis.normalized : Vector2.right;
        Vector3 world = head.TransformDirection(new Vector3(axis.x, axis.y, 0f));
        Vector2 dir = new Vector2(world.x, world.y);
        return dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;
    }

    private bool HasLineOfSightTo(Vector3 worldTarget)
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)worldTarget - origin);
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, range, lineOfSightMask);
        if (hit.collider == null) return false;
        return IsPlayerCollider(hit.collider);
    }

    private bool IsPlayerCollider(Collider2D col)
    {
        if (col == null) return false;
        if (col.GetComponentInParent<SpaceShipController>() != null) return true;
        if (col.CompareTag("Player1") || col.CompareTag("Player2")) return true;
        Transform t = col.transform;
        while (t != null) { if (t.CompareTag("Player1") || t.CompareTag("Player2")) return true; t = t.parent; }
        return false;
    }

    private IEnumerator FireSequenceRoutine()
    {
        if (preFireDelay > 0f) yield return new WaitForSeconds(preFireDelay);
        FireProjectile();
        _fireRoutine = null;
    }

    private void FireProjectile()
    {
        if (stunProjectilePrefab == null || firePoint == null) return;
        Vector2 dir = GetWorldAimDirection();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GameObject obj = Instantiate(stunProjectilePrefab, firePoint.position, Quaternion.Euler(0f, 0f, angle));
        StunProjectile proj = obj.GetComponent<StunProjectile>();
        if (proj != null) proj.Init(dir);
    }

    // -------------------------------------------------------------------------
    //  DAÑO Y VIDA
    // -------------------------------------------------------------------------

    private void OnCollisionEnter2D(Collision2D collision) { ApplyDamageFrom(collision.gameObject); }
    private void OnTriggerEnter2D(Collider2D other) { ApplyDamageFrom(other.gameObject); }

    private void ApplyDamageFrom(GameObject other)
    {
        Debug.Log($"[Turret] ApplyDamageFrom llamado: {other.name}");
        if (_isInvincible) return;
        if (other == null) return;

        Projectile projectile = other.GetComponent<Projectile>() ?? other.GetComponentInParent<Projectile>();
        Debug.Log($"[Turret] Projectile encontrado: {projectile}");
        if (projectile == null) return;

        int killer = projectile.OwnerPlayer;
        Destroy(projectile.gameObject);

        TakeDamage(damagePerHit, killer);
    }

    private void TakeDamage(float amount, int killerPlayer)
    {
        _health -= amount;
        _health = Mathf.Max(0f, _health);
        _currentHealth = _health;
        _lastDamageTime = Time.time;

        UpdateHealthBar();
        PlayDamageEffects();

        // Reiniciar regeneracion
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        _regenCoroutine = StartCoroutine(RegenRoutine());

        if (_health <= 0f && !_isDestroyed)
            EnterDestroyedState(killerPlayer);
    }

    private void EnterDestroyedState(int killerPlayer)
    {
        _isDestroyed = true;
        _hasSmoothedTarget = false;

        // Cambiar sprite a danado
        if (turretSpriteRenderer != null && spriteDamaged != null)
            turretSpriteRenderer.sprite = spriteDamaged;

        // Ocultar cabeza
        if (head != null) head.gameObject.SetActive(false);

        // Dar puntos al que lo derribo
        if (killerPlayer == 1 || killerPlayer == 2)
            GameManager.Instance?.AddPoints(killerPlayer, pointsOnDestroy);

        Debug.Log($"[SpaceLaserTurret] Destruida por jugador {killerPlayer}. Regenerando...");
    }

    private void ExitDestroyedState()
    {
        _isDestroyed = false;

        // Restaurar sprite normal
        if (turretSpriteRenderer != null && spriteNormal != null)
            turretSpriteRenderer.sprite = spriteNormal;

        // Mostrar cabeza
        if (head != null) head.gameObject.SetActive(true);

        Debug.Log("[SpaceLaserTurret] Reparada. Volviendo a funcionar.");
    }

    // -------------------------------------------------------------------------
    //  REGENERACION
    // -------------------------------------------------------------------------

    private IEnumerator RegenRoutine()
    {
        // Esperar el delay inicial sin recibir dano
        yield return new WaitForSeconds(regenDelay);

        // Regenerar vida gradualmente
        while (_health < maxHealth)
        {
            _health += regenPerSecond * Time.deltaTime;
            _health = Mathf.Min(_health, maxHealth);
            _currentHealth = _health;
            UpdateHealthBar();

            // Si estaba destruida y ya tiene algo de vida, volver a activar
            if (_isDestroyed && _health >= maxHealth * 0.3f)
                ExitDestroyedState();

            yield return null;
        }

        _health = maxHealth;
        _currentHealth = _health;
        UpdateHealthBar();
        _regenCoroutine = null;
    }

    // -------------------------------------------------------------------------
    //  EFECTOS DE DANO
    // -------------------------------------------------------------------------

    private void PlayDamageEffects()
    {
        if (_damageEffectRoutine != null) StopCoroutine(_damageEffectRoutine);
        _damageEffectRoutine = StartCoroutine(DamageEffectsCoroutine());
    }

    private IEnumerator DamageEffectsCoroutine()
    {
        _isInvincible = true;

        float elapsed = 0f;
        float blinkTimer = 0f;
        bool isBlinkRed = false;
        float totalDuration = Mathf.Max(vibrationDuration, invincibilityDuration);

        while (elapsed < totalDuration)
        {
            if (elapsed < vibrationDuration)
            {
                float offsetX = Random.Range(-vibrationIntensity, vibrationIntensity);
                float offsetY = Random.Range(-vibrationIntensity, vibrationIntensity);
                transform.position = _originalPosition + new Vector3(offsetX, offsetY, 0f);
            }
            else
            {
                transform.position = _originalPosition;
            }

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkSpeed)
            {
                blinkTimer = 0f;
                isBlinkRed = !isBlinkRed;
                SetColor(isBlinkRed ? blinkColor : _originalTurretColor);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = _originalPosition;
        SetColor(_originalTurretColor);
        _isInvincible = false;
    }

    private void SetColor(Color color)
    {
        if (turretSpriteRenderer != null) turretSpriteRenderer.color = color;
        if (headSpriteRenderer != null) headSpriteRenderer.color = color;
    }

    // -------------------------------------------------------------------------
    //  GIZMOS
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}