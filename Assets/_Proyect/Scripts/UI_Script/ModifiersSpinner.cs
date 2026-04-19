using UnityEngine;
using System.Collections.Generic;

public class ModifiersSpinner : MonoBehaviour
{
    [Header("Configuración Visual")]
    [SerializeField] private float minSpinPower = 40f;
    [SerializeField] private float maxSpinPower = 80f;
    [SerializeField] private float stopPower = 2f;

    [Header("Textos de secciones")]
    [SerializeField] private TMPro.TextMeshPro[] sectionTexts; // 3 textos hijos de la ruleta

    [Header("Sprites por minijuego")]
    [SerializeField] private Sprite spriteKOH;
    [SerializeField] private Sprite spriteDodgeDisk;

    private static readonly Dictionary<int, string[]> modifierNames = new()
    {
        { 1, new[] { "Bonus Kill", "Bonus Death", "Bonus Winner" } },
        { 2, new[] { "Comeback x3", "Bonus Hardpoint", "Point Bleed" } },
    };

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool hasSpun = false;
    private float stoppedTimer = 0f;
    private int targetMinigame = -1;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.angularDamping = 0.2f;

        targetMinigame = PlayerPrefs.GetInt("SelectedMinigame", 1);

        UpdateSprite();
        UpdateSectionTexts();
        SpinIt();
    }

    private void UpdateSprite()
    {
        if (sr == null) return;
        if (targetMinigame == 1 && spriteDodgeDisk != null)
            sr.sprite = spriteDodgeDisk;
        else if (targetMinigame == 2 && spriteKOH != null)
            sr.sprite = spriteKOH;
    }

    private void UpdateSectionTexts()
    {
        if (sectionTexts == null) return;
        string[] names = modifierNames.ContainsKey(targetMinigame)
            ? modifierNames[targetMinigame]
            : new[] { "?", "?", "?" };

        for (int i = 0; i < sectionTexts.Length && i < names.Length; i++)
        {
            if (sectionTexts[i] != null)
                sectionTexts[i].text = names[i];
        }
    }

    public void SpinIt()
    {
        float randomPower = Random.Range(minSpinPower, maxSpinPower);
        rb.AddTorque(randomPower, ForceMode2D.Impulse);
        hasSpun = true;
        stoppedTimer = 0f;
    }

    private void Update()
    {
        if (!hasSpun) return;

        if (rb.angularVelocity > 0)
        {
            rb.angularVelocity -= stopPower * Time.deltaTime;
            if (rb.angularVelocity < 0) rb.angularVelocity = 0;
        }

        if (rb.angularVelocity <= 0)
        {
            stoppedTimer += Time.deltaTime;
            if (stoppedTimer >= 0.5f)
            {
                hasSpun = false;
                stoppedTimer = 0f;
                SelectedModifier();
            }
        }
    }

    private void SelectedModifier()
    {
        int totalMods = 3;
        float rawAngle = transform.rotation.eulerAngles.z;
        float normalizedAngle = (rawAngle + 90f) % 360f;
        float degreesPerSlice = 360f / totalMods;
        int modIndex = Mathf.FloorToInt(normalizedAngle / degreesPerSlice);
        modIndex = Mathf.Clamp(modIndex, 0, totalMods - 1);

        // +1 para saltear el None=0 del enum
        int enumIndex = modIndex + 1;

        string modName = modifierNames.ContainsKey(targetMinigame)
            ? modifierNames[targetMinigame][modIndex] : "?";
        Debug.Log($"Minijuego {targetMinigame} | Modificador: {modName}");

        if (ModifierManager.Instance != null)
        {
            if (targetMinigame == 1)
                ModifierManager.Instance.SetDodgeDiskModifier((ModifierManager.DodgeDiskModifier)enumIndex);
            else if (targetMinigame == 2)
                ModifierManager.Instance.SetKOHModifier((ModifierManager.KOHModifier)enumIndex);
        }

        SceneLoader.Instance.LoadMinigame(targetMinigame);
    }
}