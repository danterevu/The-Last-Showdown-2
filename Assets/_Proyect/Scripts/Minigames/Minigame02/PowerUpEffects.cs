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
    [SerializeField] private ParticleSystem heavyGravityVFXPlayer1;
    [SerializeField] private ParticleSystem heavyGravityVFXPlayer2;

    [Header("Control Espejo")]
    [SerializeField] private float mirrorDuration = 4f;
    // AfterImage se busca automaticamente en el jugador

    [Header("Jetpack")]
    [SerializeField] private float jetpackDuration = 5f;
    [SerializeField] private float jetpackForce = 8f;
    [SerializeField] private GameObject jetpackObjectPlayer1;
    [SerializeField] private GameObject jetpackObjectPlayer2;
    [SerializeField] private Animator jetpackAnimatorPlayer1;
    [SerializeField] private Animator jetpackAnimatorPlayer2;

   

    [Header("Colores jaula")]
    [SerializeField] private Color cageColorPlayer1 = Color.blue;
    [SerializeField] private Color cageColorPlayer2 = Color.red;
    private int GetPlayerIndex(PlatformPlayerController player)
        => player.CompareTag("Player1") ? 1 : 2;

    private void PlayVFX(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.gameObject.SetActive(true);  // asegurarse que esté activo
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    private void StopVFX(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.gameObject.SetActive(false); // desactivar al terminar
    }

    // JAULA
    public IEnumerator ActivateCage(int zoneIndex, int playerIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= cagesByZone.Length)
            yield break;

        AudioManager.Instance?.PlaySFX(SoundID.PUCaja);
        GameObject cage = cagesByZone[zoneIndex];

        SpriteRenderer[] renderers = cage.GetComponentsInChildren<SpriteRenderer>();
        Animator anim = cage.GetComponent<Animator>();

        Color cageColor = playerIndex == 1 ? cageColorPlayer1 : cageColorPlayer2;

        foreach (SpriteRenderer sr in renderers)
            sr.color = cageColor;

        cage.SetActive(true);

        if (anim != null)
            anim.SetBool("Active", true);

        float timeLeft = cageDuration;
        float warningTime = 0.75f;

        bool visible = true;

        while (timeLeft > 0)
        {

            timeLeft -= Time.deltaTime;

            // parpadeo suave en los últimos segundos
            if (timeLeft <= warningTime)
            {
                float blinkSpeed = 6f;

                visible = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;

                foreach (SpriteRenderer sr in renderers)
                {
                    Color c = sr.color;
                    c.a = visible ? 0.4f : 1f; // leve flicker
                    sr.color = c;
                }
            }

            yield return null;
        }

        // fade out 
        float fade = 1f;

        if (anim != null)
            anim.SetBool("Active", false);

        while (fade > 0)
        {
            fade -= Time.deltaTime * 2f;

            foreach (SpriteRenderer sr in renderers)
            {
                Color c = sr.color;
                c.a = fade;
                sr.color = c;
            }

            yield return null;
        }

        // reset 
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = Color.white;
        }

        cage.SetActive(false);
    }
    
    // ESCUDO
    public IEnumerator ActivateShield(PlatformPlayerController user)
    {
        AudioManager.Instance?.PlaySFX(SoundID.PUShield);
        int idx = GetPlayerIndex(user);
        GameObject vfx = idx == 1 ? shieldVFXPlayer1 : shieldVFXPlayer2;
        if (vfx != null) vfx.SetActive(true);

        user.SetShield(true, shieldKnockbackMultiplier);
        yield return new WaitForSeconds(shieldDuration);
        user.SetShield(false, 1f);

        if (vfx != null) vfx.SetActive(false);
    }

    // GANCHO
    public IEnumerator ActivateHook(PlatformPlayerController user, PlatformPlayerController target)
    {
        AudioManager.Instance?.PlaySFX(SoundID.PUHook);
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
        // ANTES: user.GetComponent
        // DESPUÉS: target.GetComponent
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
        float blinkStart = 2.0f; // últimos 2 segundos

        bool visible = true;
        Color originalColor = sr != null ? sr.color : Color.white;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            // parpadeo rojo
            if (timeLeft <= blinkStart && sr != null)
            {
                float blinkSpeed = 10f;

                visible = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;

                sr.color = visible ? Color.red : originalColor;
            }

            yield return null;
        }

        // fade out suave al terminar
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

            // reset visual
            Color reset = originalColor;
            reset.a = 1f;
            sr.color = reset;
        }

        user.SetJetpack(false, 0f, null, null);

        if (jetpackObj != null)
            jetpackObj.SetActive(false);
    }

    // CANCEL ALL
    public void CancelAll(PlatformPlayerController p1, PlatformPlayerController p2)
    {
        StopAllCoroutines();

        StopVFX(heavyGravityVFXPlayer1);
        StopVFX(heavyGravityVFXPlayer2);

        if (shieldVFXPlayer1 != null) shieldVFXPlayer1.SetActive(false);
        if (shieldVFXPlayer2 != null) shieldVFXPlayer2.SetActive(false);

        p1.GetComponent<AfterImageEffect>()?.StopEffect();
        p2.GetComponent<AfterImageEffect>()?.StopEffect();

        p1.ClearActivePowerUpEffects();
        p2.ClearActivePowerUpEffects();
 
        p1.GetComponent<AfterImageEffect>()?.StopEffect();
        p2.GetComponent<AfterImageEffect>()?.StopEffect();
    }
}