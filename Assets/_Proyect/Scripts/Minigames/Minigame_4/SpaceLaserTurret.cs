
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