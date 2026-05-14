using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioDatabase database; //Tabla de sonidos

    [Header("Sources de música (para crossfade)")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("Pool de SFX")]
    [SerializeField] private int sfxPoolSize = 8; //Fuentes para efectos de sonidos

    private AudioSource[] sfxPool;
    private int sfxPoolIndex = 0;
    private AudioSource activeMusicSource;
    private Coroutine crossfadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSFXPool();
        activeMusicSource = musicSourceA;

       
       
    }

    private void OnDestroy()
    {
       
    }

    //  SFX 

    public void PlaySFX(SoundID id)
    {
        var entry = database.Get(id);
        if (entry == null || entry.clip == null) return;

        AudioSource source = GetNextSFXSource();
        source.clip = entry.clip;
        source.volume = entry.volume;
        source.loop = false;
        source.Play();
    }

    private AudioSource GetNextSFXSource()
    {
        // Busca un source libre primero
        for (int i = 0; i < sfxPool.Length; i++)
            if (!sfxPool[i].isPlaying) return sfxPool[i];

        // Si todos están ocupados, rota (sobrescribe el más viejo)
        AudioSource s = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
        return s;
    }

    // Música 

    public void PlayMusic(SoundID id, float fadeDuration = 0.8f)
    {
        var entry = database.Get(id);
        if (entry == null || entry.clip == null) return;

        // Si ya suena ese clip, no hacer nada
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

        incoming.clip = entry.clip;
        incoming.volume = 0f;
        incoming.loop = entry.loop;
        incoming.Play();

        float elapsed = 0f;
        float startVol = outgoing.volume;
        float targetVol = entry.volume;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            incoming.volume = Mathf.Lerp(0f, targetVol, t);
            outgoing.volume = Mathf.Lerp(startVol, 0f, t);
            elapsed += Time.unscaledDeltaTime; // usa unscaled por si hay pausa
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

   

   

    // Setup 

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