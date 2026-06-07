using UnityEngine;

public static class SettingsManager
{
    // ─── Claves ───────────────────────────────────────────────────────
    private const string AutoTypeKey = "AutoTypeEnabled";
    private const string KEY_MUSIC_VOLUME = "MusicVolume";
    private const string KEY_SFX_VOLUME = "SFXVolume";
    private const string KEY_RESOLUTION_IDX = "ResolutionIndex";
    private const string KEY_FULLSCREEN = "Fullscreen";

    // ─── Auto-tipo (tu código original, sin cambios) ──────────────────
    private const bool DefaultAutoType = false;

    public static bool AutoTypeEnabled
    {
        get => PlayerPrefs.GetInt(AutoTypeKey, DefaultAutoType ? 1 : 0) == 1;
        set => PlayerPrefs.SetInt(AutoTypeKey, value ? 1 : 0);
    }

    public static void ToggleAutoType()
    {
        AutoTypeEnabled = !AutoTypeEnabled;
    }

    // ─── Volumen música ───────────────────────────────────────────────
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 1f);
        set
        {
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            AudioManager.Instance?.SetMusicVolume(Mathf.Clamp01(value));
        }
    }

    // ─── Volumen SFX ──────────────────────────────────────────────────
    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1f);
        set
        {
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            AudioManager.Instance?.SetSFXVolume(Mathf.Clamp01(value));
        }
    }

    // ─── Pantalla completa ────────────────────────────────────────────
    public static bool IsFullscreen
    {
        get => PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = value;
        }
    }

    // ─── Índice de resolución (lo usa ResolutionSettingsUI) ───────────
    public static int ResolutionIndex
    {
        get => PlayerPrefs.GetInt(KEY_RESOLUTION_IDX, -1); // -1 = usar la actual
        set
        {
            PlayerPrefs.SetInt(KEY_RESOLUTION_IDX, value);
            PlayerPrefs.Save();
        }
    }

    // ─── Aplicar todo al arrancar ─────────────────────────────────────
    /// <summary>
    /// Llamá esto en el Awake de tu GameManager para que los valores
    /// guardados se apliquen cada vez que arranca el juego.
    /// </summary>
    public static void ApplyAllSettings()
    {
        AudioManager.Instance?.SetMusicVolume(MusicVolume);
        AudioManager.Instance?.SetSFXVolume(SFXVolume);
        Screen.fullScreen = IsFullscreen;
    }
}