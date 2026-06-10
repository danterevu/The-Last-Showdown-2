using UnityEngine;
using System.Collections;

public class PowerUpEffects : MonoBehaviour
{
    [Header("Gancho")]
    [SerializeField] private GameObject hookProjectilePrefab;
    [SerializeField] private float hookLaunchSpeed = 16f;
    [SerializeField] private float hookMaxDistance = 12f;
    [SerializeField] private float hookPullSpeed = 14f;
    [SerializeField] private float hookPullStopDist = 1.3f;
    [SerializeField] private float hookGravityScale = 1.5f;
    [SerializeField] private LineRenderer hookLine;

    [Header("Gravedad")]
    [SerializeField] private float heavyGravityScale = 15f;
    [SerializeField] private float heavyGravityDuration = 3f;
    [SerializeField] private ParticleSystem heavyGravityVFXPlayer1;
    [SerializeField] private ParticleSystem heavyGravityVFXPlayer2;

    [Header("Control Espejo")]
    [SerializeField] private float mirrorDuration = 4f;

    [Header("Jetpack")]
    [SerializeField] private float jetpackDuration = 5f;
    [SerializeField] private float jetpackForce = 8f;
    [SerializeField] private GameObject jetpackObjectPlayer1;
    [SerializeField] private GameObject jetpackObjectPlayer2;
    [SerializeField] private Animator jetpackAnimatorPlayer1;
    [SerializeField] private Animator jetpackAnimatorPlayer2;

    private int GetPlayerIndex(PlatformPlayerController player)
        => player.CompareTag("Player1") ? 1 : 2;

    private void PlayVFX(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.gameObject.SetActive(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    private void StopVFX(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.gameObject.SetActive(false);
    }

    // GANCHO
    public IEnumerator ActivateHook(PlatformPlayerController user, PlatformPlayerController target)
    {
        AudioManager.Instance?.PlaySFX(SoundID.PUHook);

        Vector2 origin = user.transform.position;
        Vector2 targetPos = target.transform.position;
        Vector2 directionToTarget = (targetPos - origin).normalized;

        GameObject hookGO = Instantiate(hookProjectilePrefab, origin, Quaternion.identity);
        HookProjectile hookProj = hookGO.GetComponent<HookProjectile>();

        Collider2D userCollider = user.GetCollider();
        Collider2D hookCollider = hookGO.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(hookCollider, userCollider, true);

        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        hookGO.transform.rotation = Quaternion.Euler(0, 0, angle);

        if (hookLine != null)
        {
            hookLine.enabled = true;
            hookLine.positionCount = 2;
        }

        Rigidbody2D hookRb = hookGO.GetComponent<Rigidbody2D>();
        hookRb.gravityScale = hookGravityScale;
        Vector2 launchVelocity = directionToTarget * hookLaunchSpeed;
        hookProj.Launch(launchVelocity, hookMaxDistance, origin);

        HookProjectile.HitType hitResult = HookProjectile.HitType.None;
        Vector2 hitPoint = Vector2.zero;
        Collider2D hitCollider = null;
        bool hitOccurred = false;

        hookProj.OnHit += (type, point, col) =>
        {
            hitResult = type;
            hitPoint = point;
            hitCollider = col;
            hitOccurred = true;
        };

        while (!hitOccurred && hookGO != null)
        {
            if (hookLine != null)
            {
                hookLine.SetPosition(0, user.transform.position);
                hookLine.SetPosition(1, hookGO.transform.position);
            }
            yield return null;
        }

        if (hookGO == null || !hitOccurred)
        {
            if (hookLine != null) hookLine.enabled = false;
            yield break;
        }

        if (hitResult == HookProjectile.HitType.Ground)
        {
            float pullTime = 0f;
            float maxPullTime = 1.2f;
            while (pullTime < maxPullTime)
            {
                float dist = Vector2.Distance(user.transform.position, hitPoint);
                if (dist < hookPullStopDist) break;

                Vector2 pullDir = (hitPoint - (Vector2)user.transform.position).normalized;
                user.SetPulled(true, pullDir * hookPullSpeed);

                if (hookLine != null)
                {
                    hookLine.SetPosition(0, user.transform.position);
                    hookLine.SetPosition(1, hitPoint);
                }

                pullTime += Time.deltaTime;
                yield return null;
            }
            user.SetPulled(false);
        }
        else if (hitResult == HookProjectile.HitType.Player)
        {
            float pullTime = 0f;
            float maxPullTime = 1.2f;
            while (pullTime < maxPullTime)
            {
                float dist = Vector2.Distance(target.transform.position, user.transform.position);
                if (dist < hookPullStopDist) break;

                Vector2 pullDir = ((Vector2)user.transform.position - (Vector2)target.transform.position).normalized;
                target.SetPulled(true, pullDir * hookPullSpeed);

                if (hookLine != null)
                {
                    hookLine.SetPosition(0, user.transform.position);
                    hookLine.SetPosition(1, target.transform.position);
                }

                pullTime += Time.deltaTime;
                yield return null;
            }
            target.SetPulled(false);
        }

        if (hookLine != null)
            hookLine.enabled = false;
    }

    // GRAVEDAD AUMENTADA
    public IEnumerator ActivateHeavyGravity(PlatformPlayerController target)
    {
        AudioManager.Instance?.PlaySFX(SoundID.PUGravity);
        int idx = GetPlayerIndex(target);
        ParticleSystem vfx = idx == 1 ? heavyGravityVFXPlayer1 : heavyGravityVFXPlayer2;
        PlayVFX(vfx);

        target.SetHeavyGravity(true, heavyGravityScale);
        target.SetCrushed(true);

        yield return new WaitForSeconds(heavyGravityDuration);

        target.SetHeavyGravity(false, 0f);
        target.SetCrushed(false);

        StopVFX(vfx);
    }

    // CONTROL ESPEJO
    public IEnumerator ActivateMirrorControl(PlatformPlayerController user, PlatformPlayerController target)
    {
        Debug.Log($"[PowerUpEffects] ActivateMirrorControl: user={user.gameObject.name}, target={target.gameObject.name}");
        AudioManager.Instance?.PlaySFX(SoundID.PUMirror);
        AfterImageEffect afterImage = target.GetComponent<AfterImageEffect>();
        afterImage?.StartEffect(true);

        user.SetMirrorControl(true, target);
        yield return new WaitForSeconds(mirrorDuration);
        user.SetMirrorControl(false, null);

        afterImage?.StopEffect();
    }

    // JETPACK
    public IEnumerator ActivateJetpack(PlatformPlayerController user)
    {
        int idx = GetPlayerIndex(user);
        GameObject jetpackObj = idx == 1 ? jetpackObjectPlayer1 : jetpackObjectPlayer2;
        Animator jetpackAnim = idx == 1 ? jetpackAnimatorPlayer1 : jetpackAnimatorPlayer2;

        SpriteRenderer sr = jetpackObj != null ? jetpackObj.GetComponent<SpriteRenderer>() : null;

        user.SetJetpack(true, jetpackForce, jetpackObj, jetpackAnim);

        float timeLeft = jetpackDuration;
        float blinkStart = 2.0f;
        bool visible = true;
        Color originalColor = sr != null ? sr.color : Color.white;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft <= blinkStart && sr != null)
            {
                float blinkSpeed = 10f;
                visible = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;
                sr.color = visible ? Color.red : originalColor;
            }

            yield return null;
        }

        if (sr != null)
        {
            float fade = 1f;
            while (fade > 0)
            {
                fade -= Time.deltaTime * 2f;
                Color c = sr.color;
                c.a = fade;
                sr.color = c;
                yield return null;
            }

            Color reset = originalColor;
            reset.a = 1f;
            sr.color = reset;
        }

        user.SetJetpack(false, 0f, null, null);
        if (jetpackObj != null) jetpackObj.SetActive(false);
    }

    // CANCEL ALL
    public void CancelAll(PlatformPlayerController p1, PlatformPlayerController p2)
    {
        StopAllCoroutines();

        StopVFX(heavyGravityVFXPlayer1);
        StopVFX(heavyGravityVFXPlayer2);

        p1.GetComponent<AfterImageEffect>()?.StopEffect();
        p2.GetComponent<AfterImageEffect>()?.StopEffect();

        p1.ClearActivePowerUpEffects();
        p2.ClearActivePowerUpEffects();
    }
}