using UnityEngine;

/// <summary>
/// Ruleta de modificadores. Recibe el MinigameConfig del minijuego seleccionado
/// (pasado explícitamente por RouletteShowDialogueSystem) y elige un modificador
/// al azar cuando la ruleta se detiene.
///
/// SETUP:
///   - Rigidbody2D en el mismo GO (se configura en Awake)
///   - SpriteRenderer en el mismo GO
///   - sectionTexts: 3 TextMeshPro 3D hijos de la ruleta, uno por sector
///   - Asignar los MinigameConfig en el Inspector (configs[])
/// </summary>
public class ModifiersSpinner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Física de la ruleta")]
    [SerializeField] private float minSpinPower = 40f;
    [SerializeField] private float maxSpinPower = 80f;
    [SerializeField] private float stopPower = 2f;
    [SerializeField] private float settleDelay = 0.5f;   // segundos quieta antes de confirmar

    [Header("Textos de sección (3 hijos de la ruleta)")]
    [SerializeField] private TMPro.TextMeshPro[] sectionTexts;

    [Header("Configuraciones de minijuego")]
    [Tooltip("Un MinigameConfig por minijuego. El correcto se busca por MinigameID.")]
    [SerializeField] private MinigameConfig[] configs;

    // ── Eventos ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Se dispara cuando la ruleta se detiene y determina el resultado.
    /// Parámetros: (MinigameID minijuego, int modifierEnumValue)
    /// </summary>
    public event System.Action<MinigameID, int> OnModifierComplete;

    // ── Estado interno ────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool hasSpun = false;
    private float stoppedTimer = 0f;
    private MinigameID targetMinigame = MinigameID.None;
    private MinigameConfig activeConfig = null;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.angularDamping = 0.2f;
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la ruleta para el minijuego indicado y la hace girar.
    /// Llamado por RouletteShowDialogueSystem en vez de leer PlayerPrefs.
    /// </summary>
    public void Initialize(MinigameID minigame)
    {
        targetMinigame = minigame;
        activeConfig = FindConfig(minigame);

        if (activeConfig == null)
            Debug.LogWarning($"[ModifiersSpinner] No hay MinigameConfig para {minigame}.");

        ApplySprite();
        UpdateSectionTexts();
        Spin();
    }

    public void Spin()
    {
        float power = Random.Range(minSpinPower, maxSpinPower);
        rb.AddTorque(power, ForceMode2D.Impulse);
        hasSpun = true;
        stoppedTimer = 0f;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!hasSpun) return;

        // Frenar progresivamente
        if (rb.angularVelocity > 0f)
        {
            rb.angularVelocity -= stopPower * Time.deltaTime;
            if (rb.angularVelocity < 0f) rb.angularVelocity = 0f;
        }

        // Cuando está quieta, esperar settleDelay antes de confirmar
        if (rb.angularVelocity <= 0f)
        {
            stoppedTimer += Time.deltaTime;
            if (stoppedTimer >= settleDelay)
            {
                hasSpun = false;
                stoppedTimer = 0f;
                ConfirmSelection();
            }
        }
    }

    // ── Lógica interna ────────────────────────────────────────────────────────

    private void ApplySprite()
    {
        // El sprite de la ruleta de modificadores se configura directamente
        // en el SpriteRenderer del prefab. No se cambia por código.
    }

    private void UpdateSectionTexts()
    {
        if (sectionTexts == null)
        {
            Debug.LogWarning("[ModifiersSpinner] sectionTexts es null! Asigna los textos en el Inspector.");
            return;
        }

        Debug.Log($"[ModifiersSpinner] UpdateSectionTexts: {sectionTexts.Length} textos, activeConfig={(activeConfig != null ? activeConfig.id.ToString() : "null")}");

        for (int i = 0; i < sectionTexts.Length; i++)
        {
            if (sectionTexts[i] == null)
            {
                Debug.LogWarning($"[ModifiersSpinner] sectionTexts[{i}] es null!");
                continue;
            }

            string label = "?";
            if (activeConfig != null &&
                activeConfig.modifiers != null &&
                i < activeConfig.modifiers.Length)
            {
                label = activeConfig.modifiers[i].displayName;
                Debug.Log($"[ModifiersSpinner] Texto {i}: {label}");
            }

            sectionTexts[i].text = label;
        }
    }

    private void ConfirmSelection()
    {
        int sectorCount = (activeConfig != null && activeConfig.modifiers != null)
            ? activeConfig.modifiers.Length
            : 3;

        sectorCount = Mathf.Max(1, sectorCount);

        // Calcular sector a partir del ángulo actual
        float rawAngle = transform.rotation.eulerAngles.z;
        float normalizedAngle = (rawAngle + 90f) % 360f;
        float degreesPerSlice = 360f / sectorCount;
        int modIndex = Mathf.FloorToInt(normalizedAngle / degreesPerSlice);
        modIndex = Mathf.Clamp(modIndex, 0, sectorCount - 1);

        // Leer el valor del enum desde la config (o fallback: modIndex + 1)
        int enumValue = modIndex + 1;
        if (activeConfig != null &&
            activeConfig.modifiers != null &&
            modIndex < activeConfig.modifiers.Length)
        {
            enumValue = activeConfig.modifiers[modIndex].enumValue;
        }

        // Aplicar al ModifierManager
        ApplyModifier(modIndex, enumValue);

        Debug.Log($"[ModifiersSpinner] Minijuego={targetMinigame} | Sector={modIndex} | EnumValue={enumValue}");

        // Notificar al sistema de ruleta
        OnModifierComplete?.Invoke(targetMinigame, enumValue);
    }

    private void ApplyModifier(int modIndex, int enumValue)
    {
        if (ModifierManager.Instance == null) return;

        switch (targetMinigame)
        {
            case MinigameID.DodgeDisk:
                ModifierManager.Instance.SetDodgeDiskModifier(
                    (ModifierManager.DodgeDiskModifier)enumValue);
                break;

            case MinigameID.KingOfHill:
                ModifierManager.Instance.SetKOHModifier(
                    (ModifierManager.KOHModifier)enumValue);
                break;

            case MinigameID.Space:
                ModifierManager.Instance.SetSpaceModifier(
                    (ModifierManager.SpaceModifier)enumValue);
                break;

            case MinigameID.DNA:
                Debug.Log($"[ModifiersSpinner] DNA modifier {modIndex} seleccionado (pendiente de implementar en ModifierManager).");
                break;

            case MinigameID.ChaseRun:
                Debug.Log($"[ModifiersSpinner] ChaseRun modifier {modIndex} seleccionado (pendiente de implementar en ModifierManager).");
                break;

            default:
                Debug.LogWarning($"[ModifiersSpinner] Minijuego sin switch: {targetMinigame}");
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private MinigameConfig FindConfig(MinigameID id)
    {
        if (configs == null) return null;
        foreach (MinigameConfig cfg in configs)
            if (cfg != null && cfg.id == id) return cfg;
        return null;
    }

    /// <summary>
    /// Permite que sistemas externos (ej. RouletteShowDialogueSystem) lean
    /// la configuración de un minijuego sin depender de PlayerPrefs.
    /// </summary>
    public MinigameConfig GetConfig(MinigameID id) => FindConfig(id);
}