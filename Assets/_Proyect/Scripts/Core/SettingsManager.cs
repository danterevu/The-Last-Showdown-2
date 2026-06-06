using UnityEngine;

public static class SettingsManager
{
    private const string AutoTypeKey = "AutoTypeEnabled";
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
}
