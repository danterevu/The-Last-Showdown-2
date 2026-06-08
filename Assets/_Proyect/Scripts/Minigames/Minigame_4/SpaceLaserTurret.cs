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

    [Header("Targeting")]
    [SerializeField] private float range = 12f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float targetFollowSmoothTime = 0.12f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private Vector2 headAimLocalAxis = Vector2.right;
    
    [Header("Rotation Limits")]
    [Tooltip("Minimum rotation angle in degrees (e.g., -90)")]
    [SerializeField] private float minRotation = -90f;
    [Tooltip("Maximum rotation angle in degrees (e.g., 90)")]
    [SerializeField] private float maxRotation = 90f;

    [Header("Firing")]
    [SerializeField] private float fireCooldown = 1.25f;
    [SerializeField] private float preFireDelay = 0.15f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private int pointsOnDestroy = 5;

    [Header("Damage Effects")]
    [SerializeField] private float vibrationIntensity = 0.1f;
    [SerializeField] private float vibrationDuration = 0.2f;
    [SerializeField] private float blinkSpeed = 0.1f;
    [SerializeField] private Color blinkColor = Color.red;
    [SerializeField] private int blinkCount = 3;

    [Header("Debug")]
    [SerializeField] private float _currentHealth;
    
    private float _health;
    private float _nextFireTime;
    private Coroutine _fireRoutine;
    private Coroutine _damageEffectRoutine;
    private List<Transform> _targetsInRange = new List<Transform>();
    private Transform _currentTarget;
    private Vector3 _smoothedTargetPosition;
    private Vector3 _targetSmoothVelocity;
    private bool _hasSmoothedTarget;
    private float _headTurnVelocity;
    private Vector3 _originalPosition;
    private Color _originalTurretColor;
    private Color _originalHeadColor;
    private bool _isInvincible;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    private void Awake()
    {
        _health = Mathf.Max(1f, maxHealth);
        _currentHealth = _health;
        _originalPosition = transform.position;
        
        // Obtener los SpriteRenderers si no están asignados
        if (turretSpriteRenderer == null)
            turretSpriteRenderer = GetComponent<SpriteRenderer>();
        if (headSpriteRenderer == null && head != null)
            headSpriteRenderer = head.GetComponent<SpriteRenderer>();
        
        // Guardar los colores originales
        if (turretSpriteRenderer != null)
            _originalTurretColor = turretSpriteRenderer.color;
        if (headSpriteRenderer != null)
            _originalHeadColor = headSpriteRenderer.color;
    }

    // Métodos públicos para el detector de rango
    public void AddTarget(Transform target)
    {
        if (!_targetsInRange.Contains(target))
        {
            _targetsInRange.Add(target);
        }
    }

    public void RemoveTarget(Transform target)
    {
        if (_targetsInRange.Contains(target))
        {
            _targetsInRange.Remove(target);
        }
    }

    private void Update()
    {
        _currentTarget = FindBestTarget();
        if (_currentTarget == null)
        {
            _hasSmoothedTarget = false;
            return;
        }

        Vector3 targetPos = _currentTarget.position;
        if (!_hasSmoothedTarget)
        {
            _smoothedTargetPosition = targetPos;
            _hasSmoothedTarget = true;
        }
        else if (targetFollowSmoothTime > 0f)
        {
            _smoothedTargetPosition = Vector3.SmoothDamp(
                _smoothedTargetPosition,
                targetPos,
                ref _targetSmoothVelocity,
                targetFollowSmoothTime
            );
        }
        else
        {
            _smoothedTargetPosition = targetPos;
        }

        RotateHeadTowards(_smoothedTargetPosition);

        if (Time.time < _nextFireTime) return;
        if (!HasLineOfSightTo(_smoothedTargetPosition)) return;

        _nextFireTime = Time.time + Mathf.Max(0.01f, fireCooldown);

        if (_fireRoutine != null)
            StopCoroutine(_fireRoutine);

        _fireRoutine = StartCoroutine(FireSequenceRoutine());
    }

    private Transform FindBestTarget()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        // Limpiar la lista de targets destruidos o desactivados
        _targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

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
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - axisAngle;
        
        // Clamp target angle to our limits
        targetAngle = Mathf.Clamp(targetAngle, minRotation, maxRotation);
        
        float currentAngle = head.eulerAngles.z;
        // Convert current angle to -180 to 180 range for proper clamping
        currentAngle = Mathf.Repeat(currentAngle + 180f, 360f) - 180f;
        
        float newAngle;
        if (targetFollowSmoothTime > 0f)
        {
            newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref _headTurnVelocity, targetFollowSmoothTime, Mathf.Max(0f, turnSpeed), Time.deltaTime);
        }
        else
        {
            newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, Mathf.Max(0f, turnSpeed) * Time.deltaTime);
        }
        
        // Clamp the final angle
        newAngle = Mathf.Clamp(newAngle, minRotation, maxRotation);
        
        head.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    private Vector2 GetWorldAimDirection()
    {
        if (head == null)
            return Vector2.right;

        Vector2 axis = headAimLocalAxis.sqrMagnitude > 0.0001f ? headAimLocalAxis.normalized : Vector2.right;
        Vector3 world = head.TransformDirection(new Vector3(axis.x, axis.y, 0f));
        Vector2 dir = new Vector2(world.x, world.y);
        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        return dir.normalized;
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

        if (col.GetComponentInParent<SpaceShipController>() != null)
            return true;

        if (col.CompareTag("Player1") || col.CompareTag("Player2"))
            return true;

        Transform t = col.transform;
        while (t != null)
        {
            if (t.CompareTag("Player1") || t.CompareTag("Player2"))
                return true;
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
        if (stunProjectilePrefab == null) return;
        if (firePoint == null) return;

        Vector2 dir = GetWorldAimDirection();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject obj = Instantiate(stunProjectilePrefab, firePoint.position, Quaternion.Euler(0f, 0f, angle));
        StunProjectile proj = obj.GetComponent<StunProjectile>();
        if (proj != null)
        {
            proj.Init(dir);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ApplyDamageFrom(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ApplyDamageFrom(other.gameObject);
    }

    private void ApplyDamageFrom(GameObject other)
    {
        if (_isInvincible) return;
        
        Debug.Log($"Torreta recibe colisión de: {other.name}");
        if (other == null) return;

        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null)
            projectile = other.GetComponentInParent<Projectile>();

        if (projectile == null)
        {
            Debug.Log("No se encontró el componente Projectile");
            return;
        }

        // No recibir daño de proyectiles de la propia torreta (aunque no debería pasar)
        if (projectile.OwnerPlayer == 0) return;

        float dmg = projectile.Damage > 0f ? projectile.Damage : 1f;
        int killer = projectile.OwnerPlayer;

        _health -= dmg;
        _currentHealth = _health;
        Debug.Log($"Torreta recibió {dmg} de daño. Vida actual: {_health}");
        Destroy(projectile.gameObject);

        PlayDamageEffects();

        if (_health <= 0f)
            Die(killer);
    }

    private void PlayDamageEffects()
    {
        if (_damageEffectRoutine != null)
            StopCoroutine(_damageEffectRoutine);
        
        _damageEffectRoutine = StartCoroutine(DamageEffectsCoroutine());
    }

    private IEnumerator DamageEffectsCoroutine()
    {
        _isInvincible = true;
        
        // Vibración y parpadeo al mismo tiempo
        float elapsedVibration = 0f;
        int blinkIndex = 0;
        float blinkTimer = 0f;
        bool isBlinkRed = false;

        // Usamos la duración más larga entre vibración y parpadeo
        float totalDuration = Mathf.Max(vibrationDuration, invincibilityDuration);

        while (elapsedVibration < totalDuration)
        {
            // Vibración
            if (elapsedVibration < vibrationDuration)
            {
                float offsetX = Random.Range(-vibrationIntensity, vibrationIntensity);
                float offsetY = Random.Range(-vibrationIntensity, vibrationIntensity);
                transform.position = _originalPosition + new Vector3(offsetX, offsetY, 0f);
            }
            else
            {
                transform.position = _originalPosition;
            }

            // Parpadeo
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkSpeed)
            {
                blinkTimer = 0f;
                isBlinkRed = !isBlinkRed;
                
                if (isBlinkRed)
                    SetColor(blinkColor);
                else
                    SetColor(_originalTurretColor);
            }

            elapsedVibration += Time.deltaTime;
            yield return null;
        }

        // Restaurar todo
        transform.position = _originalPosition;
        SetColor(_originalTurretColor);
        _isInvincible = false;
    }

    private void SetColor(Color color)
    {
        if (turretSpriteRenderer != null)
            turretSpriteRenderer.color = color;
        if (headSpriteRenderer != null)
            headSpriteRenderer.color = color;
    }

    private void Die(int killerPlayer)
    {
        if (killerPlayer == 1 || killerPlayer == 2)
            GameManager.Instance?.AddPoints(killerPlayer, pointsOnDestroy);

        Destroy(gameObject);
    }

    // Gizmos para ver el rango en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
