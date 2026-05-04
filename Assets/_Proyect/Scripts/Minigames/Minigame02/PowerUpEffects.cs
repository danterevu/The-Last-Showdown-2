using UnityEngine;
using System.Collections;

public class PowerUpEffects : MonoBehaviour
{
    [Header("Jaula")]
    [SerializeField] private float cageDuration = 5f;
    [SerializeField] private GameObject[] cagesByZone;

    [Header("Escudo")]
    [SerializeField] private float shieldDuration = 4f;
    [SerializeField] private float shieldKnockbackMultiplier = 3f;
    [SerializeField] private GameObject shieldVFXPlayer1;
    [SerializeField] private GameObject shieldVFXPlayer2;

    [Header("Gancho")]
    [SerializeField] private float hookSpeed = 15f;
    [SerializeField] private LineRenderer hookLine;
    [SerializeField] private LayerMask hookObstacleLayer;

    [Header("Gravedad")]
    [SerializeField] private float heavyGravityScale = 15f;
    [SerializeField] private float heavyGravityDuration = 3f;
    [SerializeField] private GameObject heavyGravityVFXPlayer1;
    [SerializeField] private GameObject heavyGravityVFXPlayer2;

    [Header("Control Espejo")]
    [SerializeField] private float mirrorDuration = 4f;
    [SerializeField] private GameObject mirrorVFXPlayer1;
    [SerializeField] private GameObject mirrorVFXPlayer2;

    [Header("Jetpack")]
    [SerializeField] private float jetpackDuration = 5f;
    [SerializeField] private float jetpackForce = 8f;
    [SerializeField] private GameObject jetpackObjectPlayer1;
    [SerializeField] private GameObject jetpackObjectPlayer2;
    [SerializeField] private Animator jetpackAnimatorPlayer1;
    [SerializeField] private Animator jetpackAnimatorPlayer2;

    private int GetPlayerIndex(PlatformPlayerController player)
        => player.CompareTag("Player1") ? 1 : 2;

    private GameObject GetVFX(GameObject vfx1, GameObject vfx2, int playerIndex)
        => playerIndex == 1 ? vfx1 : vfx2;

    // JAULA
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

    // ESCUDO
    public IEnumerator ActivateShield(PlatformPlayerController user)
    {
        int idx = GetPlayerIndex(user);
        GameObject vfx = GetVFX(shieldVFXPlayer1, shieldVFXPlayer2, idx);
        if (vfx) vfx.SetActive(true);

        user.SetShield(true, shieldKnockbackMultiplier);
        yield return new WaitForSeconds(shieldDuration);
        user.SetShield(false, 1f);

        if (vfx) vfx.SetActive(false);
    }

    // GANCHO
    public IEnumerator ActivateHook(PlatformPlayerController user, PlatformPlayerController target)
    {
        Vector2 userPos = user.transform.position;
        Vector2 targetPos = target.transform.position;
        Vector2 dir = targetPos - userPos;
        float dist = dir.magnitude;

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

    // GRAVEDAD AUMENTADA
    public IEnumerator ActivateHeavyGravity(PlatformPlayerController target)
    {
        int idx = GetPlayerIndex(target);
        GameObject vfx = GetVFX(heavyGravityVFXPlayer1, heavyGravityVFXPlayer2, idx);
        if (vfx) vfx.SetActive(true);

        target.SetHeavyGravity(true, heavyGravityScale);
        yield return new WaitForSeconds(heavyGravityDuration);
        target.SetHeavyGravity(false, 0f);

        if (vfx) vfx.SetActive(false);
    }

    // CONTROL ESPEJO
    public IEnumerator ActivateMirrorControl(PlatformPlayerController user, PlatformPlayerController target)
    {
        int idx = GetPlayerIndex(user);
        GameObject vfx = GetVFX(mirrorVFXPlayer1, mirrorVFXPlayer2, idx);
        if (vfx) vfx.SetActive(true);

        user.SetMirrorControl(true, target);
        yield return new WaitForSeconds(mirrorDuration);
        user.SetMirrorControl(false, null);

        if (vfx) vfx.SetActive(false);
    }

    // JETPACK
    public IEnumerator ActivateJetpack(PlatformPlayerController user)
    {
        int idx = GetPlayerIndex(user);
        GameObject jetpackObj = idx == 1 ? jetpackObjectPlayer1 : jetpackObjectPlayer2;
        Animator jetpackAnim = idx == 1 ? jetpackAnimatorPlayer1 : jetpackAnimatorPlayer2;

        user.SetJetpack(true, jetpackForce, jetpackObj, jetpackAnim);

        yield return new WaitForSeconds(jetpackDuration);

        user.SetJetpack(false, 0f, null, null);
    }
}