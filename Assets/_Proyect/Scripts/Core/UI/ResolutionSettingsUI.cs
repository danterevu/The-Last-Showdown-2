using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Adjuntá al panel de Accesibilidad/Settings.
/// Necesita un TMP_Dropdown para la resolución y opcionalmente un Toggle para pantalla completa.
///
/// ¿Cómo funciona Screen.SetResolution en Unity?
/// Unity tiene Screen.resolutions[] que lista todas las resoluciones que soporta el monitor.
/// Screen.SetResolution(width, height, fullscreen) aplica el cambio.
/// En builds, el cambio es inmediato. En el editor solo se ve al hacer build.
/// </summary>
public class ResolutionSettingsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    // Lista filtrada de resoluciones únicas (sin duplicados por refresh rate)
    private List<Resolution> availableResolutions = new List<Resolution>();

    private bool isInitializing = false;

    private void OnEnable()
    {
        BuildResolutionList();
        LoadSavedValues();
    }

    // ─── Construcción del dropdown ────────────────────────────────────

    private void BuildResolutionList()
    {
        if (resolutionDropdown == null) return;

        availableResolutions.Clear();
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        HashSet<string> seen = new HashSet<string>();

        // Screen.resolutions puede devolver duplicados con distinto refresh rate
        // Filtramos para mostrar solo combinaciones únicas de width × height
        foreach (Resolution res in Screen.resolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (seen.Contains(key)) continue;
            seen.Add(key);

            availableResolutions.Add(res);
            options.Add(key);
        }

        resolutionDropdown.AddOptions(options);
    }

    private void LoadSavedValues()
    {
        isInitializing = true;

        // Resolución
        if (resolutionDropdown != null)
        {
            int savedIdx = SettingsManager.ResolutionIndex;
            // Aseguramos que el índice guardado es válido
            savedIdx = Mathf.Clamp(savedIdx, 0, availableResolutions.Count - 1);

            // Buscamos la resolución actual en nuestra lista filtrada
            int currentIdx = FindCurrentResolutionIndex();
            resolutionDropdown.value = currentIdx >= 0 ? currentIdx : savedIdx;
            resolutionDropdown.RefreshShownValue();

            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        // Pantalla completa
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        isInitializing = false;
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return availableResolutions.Count - 1;
    }

    // ─── Callbacks ────────────────────────────────────────────────────

    public void OnResolutionChanged(int index)
    {
        if (isInitializing) return;
        if (index < 0 || index >= availableResolutions.Count) return;

        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        SettingsManager.ResolutionIndex = index;

        Debug.Log($"[ResolutionSettings] Resolución cambiada a {res.width}x{res.height}");
    }

    public void OnFullscreenChanged(bool value)
    {
        if (isInitializing) return;
        SettingsManager.IsFullscreen = value;
        Screen.fullScreen = value;
        Debug.Log($"[ResolutionSettings] Pantalla completa: {value}");
    }

    private void OnDisable()
    {
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }
}
