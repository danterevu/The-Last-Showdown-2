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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSFXPool();
        activeMusicSource = musicSourceA;

        // Carga los valores guardados sin llamar ApplyAllSettings todavía
        // (el AudioManager puede no estar listo cuando se llama)
        musicVolumeMaster = SettingsManager.MusicVolume;
        sfxVolumeMaster = SettingsManager.SFXVolume;
    }

    // Volumen maestro 

    /// <summary>Llamado por SettingsManager cuando el slider de música cambia</summary>
    public void SetMusicVolume(float vol)
    {
        musicVolumeMaster = Mathf.Clamp01(vol);
        // Aplica inmediatamente a la source activa
        if (activeMusicSource != null)
            activeMusicSource.volume = activeMusicEntryVolume * musicVolumeMaster;
    }

    /// <summary>Llamado por SettingsManager cuando el slider de SFX cambia</summary>
    public void SetSFXVolume(float vol)
    {
        sfxVolumeMaster = Mathf.Clamp01(vol);
        // Los SFX ya usarán el nuevo volumen en el próximo Play()
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
