using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adjuntá este script al panel de Audio en tu settings.
/// Arrastrá los sliders de Música y SFX en el Inspector.
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isInitializing = false;

    private void OnEnable()
    {
        // Carga los valores guardados en los sliders al abrir el panel
        LoadSavedValues();
    }

    private void LoadSavedValues()
    {
        isInitializing = true;  // Evita que los callbacks disparen al setear valores

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = SettingsManager.MusicVolume;
            // Suscribí el callback UNA sola vez
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = SettingsManager.SFXVolume;
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        isInitializing = false;
    }

    // ─── Callbacks de los sliders ─────────────────────────────────────

    public void OnMusicVolumeChanged(float value)
    {
        if (isInitializing) return;
        SettingsManager.MusicVolume = value;
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (isInitializing) return;
        SettingsManager.SFXVolume = value;

        // Preview: reproduce un SFX de prueba para que el jugador escuche
        // el nuevo volumen. Podés cambiar PointAdd por cualquier SFX corto.
        AudioManager.Instance?.PlaySFX(SoundID.PointAdd);
    }

    private void OnDisable()
    {
        // Limpiamos los listeners para evitar duplicados si el panel
        // se activa/desactiva varias veces
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }
}
