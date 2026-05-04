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
    [SerializeField] private Sprite spriteSpace; // sprite para el Minigame_5

    // Índice del array = sección de la ruleta (0, 1, 2)
    // El texto "Sin Modificador" en el slot 2 de Space mapea al enum None
    private static readonly Dictionary<int, string[]> modifierNames = new()
    {
        { 1, new[] { "Bonus Kill",   "Bonus Death",    "Bonus Winner"    } }, // DodgeDisk
        { 2, new[] { "Comeback x3", "Bonus Hardpoint", "Point Bleed"     } }, // KOH
        { 5, new[] { "Golden Kill",  "Combo Rounds",   "Sin Modificador" } }, // Space
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
        else if (targetMinigame == 5 && spriteSpace != null)
            sr.sprite = spriteSpace;
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

        string modName = modifierNames.ContainsKey(targetMinigame)
            ? modifierNames[targetMinigame][modIndex] : "?";

        Debug.Log($"Minijuego {targetMinigame} | Modificador: {modName}");

        if (ModifierManager.Instance != null)
        {
            if (targetMinigame == 1)
            {
                // DodgeDisk: None=0, BonusKill=1, BonusDeath=2, BonusWinner=3
                // los 3 slots siempre mapean a un mod real (no hay None)
                ModifierManager.Instance.SetDodgeDiskModifier(
                    (ModifierManager.DodgeDiskModifier)(modIndex + 1));
            }
            else if (targetMinigame == 2)
            {
                // KOH: None=0, Comeback=1, Progressive=2, PointBleed=3
                ModifierManager.Instance.SetKOHModifier(
                    (ModifierManager.KOHModifier)(modIndex + 1));
            }
            else if (targetMinigame == 5)
            {
                // Space: None=0, GoldenKill=1, ComboRounds=2
                // slot 0  GoldenKill (1)
                // slot 1  ComboRounds (2)
                // slot 2  Sin Modificador (0 = None)
                int spaceEnumIndex = modIndex < 2 ? modIndex + 1 : 0;
                ModifierManager.Instance.SetSpaceModifier(
                    (ModifierManager.SpaceModifier)spaceEnumIndex);
            }
        }

        SceneLoader.Instance.LoadMinigame(targetMinigame);
    }
}