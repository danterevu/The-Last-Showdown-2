using UnityEngine;
using System.Collections;

public class PowerUpEffects : MonoBehaviour
{
    [Header("Jaula")]
    [SerializeField] private float cageDuration = 5f;
    [SerializeField] private GameObject[] cagesByZone; // una jaula prefab por zona, asignar en Inspector

    [Header("Escudo")]
    [SerializeField] private float shieldDuration = 4f;
    [SerializeField] private float shieldKnockbackMultiplier = 3f;

    [Header("Gancho")]
    [SerializeField] private float hookSpeed = 15f;
    [SerializeField] private LineRenderer hookLine;
    [SerializeField] private LayerMask hookObstacleLayer; // layers que bloquean el gancho

    [Header("Doble Salto")]
    [SerializeField] private float doubleJumpDuration = 6f;

    [Header("Gravedad")]
    [SerializeField] private float heavyGravityScale = 15f;
    [SerializeField] private float heavyGravityDuration = 3f;

    [Header("Control Espejo")]
    [SerializeField] private float mirrorDuration = 4f;

    [Header("Invertir Controles")]
    [SerializeField] private float invertDuration = 4f;

    [Header("Jetpack")]
    [SerializeField] private float jetpackDuration = 5f;
    [SerializeField] private float jetpackForce = 8f;

    // JAULA: activa/desactiva el GameObject de jaula de la zona actual
    public IEnumerator ActivateCage(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= cagesByZone.Length)
        {
            Debug.LogWarning("ActivateCage: zoneIndex fuera de rango: " + zoneIndex);
            yield break;
        }
        GameObject cage = cagesByZone[zoneIndex];
        cage.SetActive(true);
        yield return new WaitForSeconds(cageDuration);
        cage.SetActive(false);
    }

    // ESCUDO: devuelve knockback al atacante
    public IEnumerator ActivateShield(PlatformPlayerController user)
    {
        user.SetShield(true, shieldKnockbackMultiplier);
        yield return new WaitForSeconds(shieldDuration);
        user.SetShield(false, 1f);
    }

    // GANCHO: jala al target hacia el user si hay linea de vision libre
    public IEnumerator ActivateHook(PlatformPlayerController user, PlatformPlayerController target)
    {
        Vector2 userPos = user.transform.position;
        Vector2 targetPos = target.transform.position;
        Vector2 dir = targetPos - userPos;
        float dist = dir.magnitude;

        // chequear obstaculos entre user y target
        RaycastHit2D[] hits = Physics2D.RaycastAll(userPos, dir.normalized, dist, hookObstacleLayer);
        bool blocked = false;
        RaycastHit2D firstHit = default;
        foreach (var hit in hits)
        {
            if (hit.collider == user.GetCollider()) continue;
            if (hit.collider == target.GetCollider()) continue;
            blocked = true;
            firstHit = hit;
            break;
        }

        if (blocked)
        {
            Debug.Log("Gancho bloqueado");
            if (hookLine != null)
            {
                hookLine.enabled = true;
                hookLine.positionCount = 2;
                hookLine.SetPosition(0, userPos);
                hookLine.SetPosition(1, firstHit.point);
                yield return new WaitForSeconds(0.2f);
                hookLine.enabled = false;
            }
            yield break;
        }

        // ejecutar gancho
        if (hookLine != null) { hookLine.enabled = true; hookLine.positionCount = 2; }

        float elapsed = 0f;
        float pullTime = 0.6f;

        while (elapsed < pullTime)
        {
            float currentDist = Vector2.Distance(user.transform.position, target.transform.position);
            if (currentDist < 1.5f) break;

            if (hookLine != null)
            {
                hookLine.SetPosition(0, user.transform.position);
                hookLine.SetPosition(1, target.transform.position);
            }

            Vector2 pullDir = ((Vector2)user.transform.position - (Vector2)target.transform.position).normalized;
            target.ForceVelocityRaw(pullDir * hookSpeed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (hookLine != null) hookLine.enabled = false;
    }

    // DOBLE SALTO
    public IEnumerator ActivateDoubleJump(PlatformPlayerController user)
    {
        user.SetDoubleJump(true);
        yield return new WaitForSeconds(doubleJumpDuration);
        user.SetDoubleJump(false);
    }

    // GRAVEDAD AUMENTADA
    public IEnumerator ActivateHeavyGravity(PlatformPlayerController target)
    {
        target.SetHeavyGravity(true, heavyGravityScale);
        yield return new WaitForSeconds(heavyGravityDuration);
        target.SetHeavyGravity(false, 0f);
    }

    // CONTROL ESPEJO
    public IEnumerator ActivateMirrorControl(PlatformPlayerController user, PlatformPlayerController target)
    {
        user.SetMirrorControl(true, target);
        yield return new WaitForSeconds(mirrorDuration);
        user.SetMirrorControl(false, null);
    }

    // CONTROLES INVERTIDOS
    public IEnumerator ActivateInvertControls(PlatformPlayerController target)
    {
        target.SetInvertControls(true);
        yield return new WaitForSeconds(invertDuration);
        target.SetInvertControls(false);
    }

    // JETPACK
    public IEnumerator ActivateJetpack(PlatformPlayerController user)
    {
        user.SetJetpack(true, jetpackForce);
        yield return new WaitForSeconds(jetpackDuration);
        user.SetJetpack(false, 0f);
    }
}
