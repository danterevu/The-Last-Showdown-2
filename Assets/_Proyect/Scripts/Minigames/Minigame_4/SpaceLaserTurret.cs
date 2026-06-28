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
    [Tooltip("Sprite cuando la torreta esta danada/desactivada")]
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
    [Tooltip("Vida maxima de la torreta")]
    [SerializeField] private float maxHealth = 30f;
    [Tooltip("Dano que recibe por cada bala")]
    [SerializeField] private float damagePerHit = 10f;
    [Tooltip("Segundos sin recibir dano antes de empezar a regenerar")]
    [SerializeField] private float regenDelay = 4f;
    [Tooltip("Vida regenerada por segundo")]
    [SerializeField] private float regenPerSecond = 5f;

    [Header("Barra de vida")]
    [Tooltip("Offset en Y respecto al centro del canon")]
    [SerializeField] private float healthBarOffsetY = 1.2f;
    [Tooltip("Ancho total de la barra en unidades de mundo")]
    [SerializeField] private float healthBarWidth = 1.5f;
    [Tooltip("Alto de la barra en unidades de mundo")]
    [SerializeField] private float healthBarHeight = 0.18f;
    [SerializeField] private Color healthBarColorFull = Color.green;
    [SerializeField] private Color healthBarColorLow = Color.red;
    [Tooltip("Porcentaje de vida al que la barra cambia a color low")]
    [SerializeField] private float lowHealthThreshold = 0.35f;
    [SerializeField] private Color healthBarBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Header("Damage Effects")]
    [SerializeField] private float vibrationIntensity = 0.1f;
    [SerializeField] private float vibrationDuration = 0.2f;
    [SerializeField] private float blinkSpeed = 0.1f;
    [SerializeField] private Color blinkColor = Color.red;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private float _currentHealth;

    // --- Estado ---
    private float _health;
    private bool _isDestroyed = false;
    private bool _isInvincible = false;

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

    // --- Barra de vida ---
    private GameObject _healthBarRoot;
    private SpriteRenderer _healthBarFill;

    // -------------------------------------------------------------------------
    //  INIT
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
    //  BARRA DE VIDA
    // -------------------------------------------------------------------------

    private void CreateHealthBar()
    {
        _healthBarRoot = new GameObject("TurretHealthBar");
        _healthBarRoot.transform.SetParent(transform, false);
        _healthBarRoot.transform.localPosition = new Vector3(0f, healthBarOffsetY, 0f);

        // Fondo
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(_healthBarRoot.transform, false);
        SpriteRenderer bgSr = bgGO.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateWhiteSprite();
        bgSr.color = healthBarBgColor;
        bgSr.sortingOrder = 10;
        bgGO.transform.localScale = new Vector3(healthBarWidth, healthBarHeight, 1f);

        // Contenedor del fill (se escala en X desde la izquierda)
        GameObject fillContainer = new GameObject("FillContainer");
        fillContainer.transform.SetParent(_healthBarRoot.transform, false);
        // Pivot izquierdo: mover el contenedor a la izquierda
        fillContainer.transform.localPosition = new Vector3(-healthBarWidth * 0.5f, 0f, 0f);

        // Fill interior (centrado dentro del contenedor, con offset para que escale desde izquierda)
        GameObject fillInner = new GameObject("Fill");
        fillInner.transform.SetParent(fillContainer.transform, false);
        fillInner.transform.localPosition = new Vector3(healthBarWidth * 0.5f, 0f, 0f);
        _healthBarFill = fillInner.AddComponent<SpriteRenderer>();
        _healthBarFill.sprite = CreateWhiteSprite();
        _healthBarFill.color = healthBarColorFull;
        _healthBarFill.sortingOrder = 11;
        fillInner.transform.localScale = new Vector3(healthBarWidth, healthBarHeight * 0.75f, 1f);
    }

    private void UpdateHealthBar()
    {
        if (_healthBarFill == null || _healthBarRoot == null) return;

        float ratio = Mathf.Clamp01(_health / maxHealth);

        // Escalar el contenedor en X para que la barra se encoja desde la derecha
        Transform container = _healthBarFill.transform.parent;
        if (container != null)
            container.localScale = new Vector3(ratio, 1f, 1f);

        _healthBarFill.color = ratio <= lowHealthThreshold ? healthBarColorLow : healthBarColorFull;

        // Mostrar solo cuando no tiene vida llena
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
        if (_isDestroyed) return;

        _currentTarget = FindBestTarget();

      

        if (_currentTarget == null) { _hasSmoothedTarget = false; return; }

        Vector3 targetPos = _currentTarget.position;
        if (!_hasSmoothedTarget)
        {
            _smoothedTargetPosition = targetPos;
            _hasSmoothedTarget = true;
        }
        else if (targetFollowSmoothTime > 0f)
        {
            _smoothedTargetPosition = Vector3.SmoothDamp(
                _smoothedTargetPosition, targetPos,
                ref _targetSmoothVelocity, targetFollowSmoothTime);
        }
        else
        {
            _smoothedTargetPosition = targetPos;
        }

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
            if (sqr <= range * range && sqr < bestSqr)
            {
                best = target;
                bestSqr = sqr;
            }
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
        float targetAngle = Mathf.Clamp(
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - axisAngle,
            minRotation, maxRotation);

        float currentAngle = Mathf.Repeat(head.eulerAngles.z + 180f, 360f) - 180f;

        float newAngle;
        if (targetFollowSmoothTime > 0f)
            newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle,
                ref _headTurnVelocity, targetFollowSmoothTime,
                Mathf.Max(0f, turnSpeed), Time.deltaTime);
        else
            newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle,
                Mathf.Max(0f, turnSpeed) * Time.deltaTime);

        head.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(newAngle, minRotation, maxRotation));
    }

    private Vector2 GetWorldAimDirection()
    {
        if (head == null) return Vector2.right;

        Vector2 axis = headAimLocalAxis.sqrMagnitude > 0.0001f
            ? headAimLocalAxis.normalized : Vector2.right;
        Vector3 world = head.TransformDirection(new Vector3(axis.x, axis.y, 0f));
        Vector2 d = new Vector2(world.x, world.y);
        return d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
    }

    private bool HasLineOfSightTo(Vector3 worldTarget)
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)worldTarget - origin);
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, range, lineOfSightMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Ignorar colliders propios
            if (hit.collider.transform.IsChildOf(transform) ||
                hit.collider.transform == transform) continue;

            // Ignorar triggers (Boundary, zonas, etc.)
            if (hit.collider.isTrigger) continue;

            // El primer collider sólido: ¿es el player?
            return IsPlayerCollider(hit.collider);
        }
        return false;
    }
    private bool IsPlayerCollider(Collider2D col)
    {
        if (col == null) return false;
        if (col.GetComponentInParent<SpaceShipController>() != null) return true;
        if (col.CompareTag("Player1") || col.CompareTag("Player2")) return true;
        Transform t = col.transform;
        while (t != null)
        {
            if (t.CompareTag("Player1") || t.CompareTag("Player2")) return true;
            t = t.parent;
        }
        return false;
    }

    private IEnumerator FireSequenceRoutine()
    {
        if (preFireDelay > 0f)
            yield return new WaitForSeconds(preFireDelay);
        FireProjectile();
        _fireRoutine = null;
    }

    private void FireProjectile()
    {
        if (stunProjectilePrefab == null || firePoint == null) return;

        Vector2 dir = GetWorldAimDirection();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GameObject obj = Instantiate(stunProjectilePrefab,
            firePoint.position, Quaternion.Euler(0f, 0f, angle));
        StunProjectile proj = obj.GetComponent<StunProjectile>();
        if (proj != null) proj.Init(dir);
    }

    // -------------------------------------------------------------------------
    //  DAÑO - llamado directamente por Projectile.cs
    // -------------------------------------------------------------------------

    /// El proyectil llama a este metodo y se destruye el mismo.
    /// No registra kills ni da puntos, solo aplica dano a la torreta.
    public void ReceiveDamageFromProjectile(float amount)
    {
        if (_isInvincible) return;
        TakeDamage(amount);
    }

    private void TakeDamage(float amount)
    {
        _health -= amount;
        _health = Mathf.Max(0f, _health);
        _currentHealth = _health;

        UpdateHealthBar();
        PlayDamageEffects();

        // Reiniciar regen
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        _regenCoroutine = StartCoroutine(RegenRoutine());

        if (_health <= 0f && !_isDestroyed)
            EnterDestroyedState();
    }

    private void EnterDestroyedState()
    {
        _isDestroyed = true;
        _hasSmoothedTarget = false;

        if (turretSpriteRenderer != null && spriteDamaged != null)
            turretSpriteRenderer.sprite = spriteDamaged;

        if (head != null) head.gameObject.SetActive(false);

        Debug.Log("[SpaceLaserTurret] Destruida. Regenerando...");
    }

    private void ExitDestroyedState()
    {
        _isDestroyed = false;

        if (turretSpriteRenderer != null && spriteNormal != null)
            turretSpriteRenderer.sprite = spriteNormal;

        if (head != null) head.gameObject.SetActive(true);

        Debug.Log("[SpaceLaserTurret] Reparada.");
    }

    // -------------------------------------------------------------------------
    //  REGENERACION
    // -------------------------------------------------------------------------

    private IEnumerator RegenRoutine()
    {
        yield return new WaitForSeconds(regenDelay);

        while (_health < maxHealth)
        {
            _health += regenPerSecond * Time.deltaTime;
            _health = Mathf.Min(_health, maxHealth);
            _currentHealth = _health;
            UpdateHealthBar();

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
                float ox = Random.Range(-vibrationIntensity, vibrationIntensity);
                float oy = Random.Range(-vibrationIntensity, vibrationIntensity);
                transform.position = _originalPosition + new Vector3(ox, oy, 0f);
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