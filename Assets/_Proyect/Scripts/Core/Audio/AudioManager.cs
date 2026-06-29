using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioDatabase database;

    [Header("Sources de música (para crossfade)")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("Pool de SFX")]
    [SerializeField] private int sfxPoolSize = 8;

    private AudioSource[] sfxPool;
    private int sfxPoolIndex = 0;
    private AudioSource activeMusicSource;
    private Coroutine crossfadeCoroutine;

    // Volúmenes maestros (0-1), se aplican al reproducir
    private float musicVolumeMaster = 1f;
    private float sfxVolumeMaster = 1f;

    // Guarda el volumen "real" (del AudioEntry) del clip activo para
    // que al cambiar el slider el nuevo valor sea proporcional
    private float activeMusicEntryVolume = 1f;

    // Estado de pausa de música (para poder reanudar desde donde quedó)
    private AudioClip pausedMusicClip;
    private float pausedMusicTime = 0f;
    private bool hasPausedMusic = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSFXPool();
        activeMusicSource = musicSourceA;


        musicVolumeMaster = SettingsManager.MusicVolume;
        sfxVolumeMaster = SettingsManager.SFXVolume;
    }

    // Volumen maestro 


    public void SetMusicVolume(float vol)
    {
        musicVolumeMaster = Mathf.Clamp01(vol);

        if (activeMusicSource != null)
            activeMusicSource.volume = activeMusicEntryVolume * musicVolumeMaster;
    }


    public void SetSFXVolume(float vol)
    {
        sfxVolumeMaster = Mathf.Clamp01(vol);

    }

    public float GetMusicVolume() => musicVolumeMaster;
    public float GetSFXVolume() => sfxVolumeMaster;

    // ─── SFX ─────────────────────────────────────────────────────────

    public void PlaySFX(SoundID id)
    {
        var entry = database.Get(id);
        if (entry == null || entry.clip == null) return;

        AudioSource source = GetNextSFXSource();
        source.clip = entry.clip;
        source.volume = entry.volume * sfxVolumeMaster;
        source.loop = false;
        source.Play();
    }

    private AudioSource GetNextSFXSource()
    {
        for (int i = 0; i < sfxPool.Length; i++)
            if (!sfxPool[i].isPlaying) return sfxPool[i];

        AudioSource s = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
        return s;
    }

    // ─── Música ──────────────────────────────────────────────────────

    public void PlayMusic(SoundID id, float fadeDuration = 0.8f)
    {
        var entry = database.Get(id);
        if (entry == null || entry.clip == null) return;

        if (activeMusicSource.clip == entry.clip && activeMusicSource.isPlaying) return;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossFade(entry, fadeDuration));
    }

    public void StopMusic(float fadeDuration = 0.8f)
    {
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(FadeOut(activeMusicSource, fadeDuration));
    }

    /// <summary>
    /// Pausa la música actual con un fade y recuerda en qué momento estaba
    /// para poder reanudarla más adelante con ResumeMusic().
    /// </summary>
    public void PauseMusic(float fadeDuration = 0.5f)
    {
        if (activeMusicSource == null || activeMusicSource.clip == null) return;

        pausedMusicClip = activeMusicSource.clip;
        pausedMusicTime = activeMusicSource.time;
        hasPausedMusic = true;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(FadeOutAndPause(activeMusicSource, fadeDuration));
    }

    /// <summary>
    /// Reanuda la música indicada. Si es la misma que se pausó con PauseMusic(),
    /// continúa desde el mismo punto. Si no, la inicia normalmente (como PlayMusic).
    /// </summary>
    public void ResumeMusic(SoundID id, float fadeDuration = 0.5f)
    {
        var entry = database.Get(id);
        if (entry == null || entry.clip == null) return;

        // Ya está sonando esta música -> no hacer nada
        if (activeMusicSource.clip == entry.clip && activeMusicSource.isPlaying) return;

        float resumeTime = (hasPausedMusic && pausedMusicClip == entry.clip) ? pausedMusicTime : 0f;
        hasPausedMusic = false;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossFadeFromTime(entry, resumeTime, fadeDuration));
    }

    private IEnumerator CrossFade(AudioDatabase.AudioEntry entry, float duration)
    {
        AudioSource outgoing = activeMusicSource;
        AudioSource incoming = outgoing == musicSourceA ? musicSourceB : musicSourceA;

        activeMusicEntryVolume = entry.volume;

        incoming.clip = entry.clip;
        incoming.volume = 0f;
        incoming.loop = entry.loop;
        incoming.Play();

        float elapsed = 0f;
        float startVol = outgoing.volume;
        float targetVol = entry.volume * musicVolumeMaster;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            incoming.volume = Mathf.Lerp(0f, targetVol, t);
            outgoing.volume = Mathf.Lerp(startVol, 0f, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        incoming.volume = targetVol;
        outgoing.Stop();
        activeMusicSource = incoming;
        crossfadeCoroutine = null;
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        source.Stop();
    }

    private IEnumerator FadeOutAndPause(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        source.volume = 0f;
        source.Pause(); // Pause en vez de Stop: conserva el clip "vivo" en la fuente
        crossfadeCoroutine = null;
    }

    private IEnumerator CrossFadeFromTime(AudioDatabase.AudioEntry entry, float startTime, float duration)
    {
        AudioSource outgoing = activeMusicSource;
        AudioSource incoming = outgoing == musicSourceA ? musicSourceB : musicSourceA;

        activeMusicEntryVolume = entry.volume;

        incoming.clip = entry.clip;
        incoming.volume = 0f;
        incoming.loop = entry.loop;
        incoming.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, entry.clip.length - 0.01f));
        incoming.Play();

        float elapsed = 0f;
        float startVol = outgoing.volume;
        float targetVol = entry.volume * musicVolumeMaster;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            incoming.volume = Mathf.Lerp(0f, targetVol, t);
            outgoing.volume = Mathf.Lerp(startVol, 0f, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        incoming.volume = targetVol;
        outgoing.Stop();
        activeMusicSource = incoming;
        crossfadeCoroutine = null;
    }

    // ─── Setup ───────────────────────────────────────────────────────

    private void BuildSFXPool()
    {
        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(transform);
            sfxPool[i] = go.AddComponent<AudioSource>();
        }
    }
}