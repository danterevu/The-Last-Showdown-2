using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// Muestra el arma actual, la munición y la barra de carga del laser
/// para un jugador. Poner dos instancias en el Canvas (una por jugador).

/// SETUP:
///   - Arrastrá el WeaponController del jugador correspondiente
///   - weaponIcon: Image del ícono del arma
///   - ammoText: TextMeshProUGUI con "bala/total"
///   - chargeBar: Slider o Image con FillAmount para la barra de carga del laser
///   - chargeBarRoot: el GameObject padre de la barra (se activa solo con laser)

public class WeaponHUD : MonoBehaviour
{
    [Header("Referencia al jugador")]
    [SerializeField] private WeaponController weaponController;

    [Header("UI - Arma")]
    [SerializeField] private Image           weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;

    [Header("UI - Munición")]
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("UI - Barra de carga (solo laser)")]
    [SerializeField] private GameObject chargeBarRoot;  // se activa/desactiva
    [SerializeField] private Image      chargeBarFill;  // Image con Image Type = Filled

   

    private void OnEnable()
    {
        if (weaponController == null) return;
        weaponController.OnWeaponChanged += HandleWeaponChanged;
        weaponController.OnAmmoChanged   += HandleAmmoChanged;
        weaponController.OnChargeChanged += HandleChargeChanged;
    }

    private void OnDisable()
    {
        if (weaponController == null) return;
        weaponController.OnWeaponChanged -= HandleWeaponChanged;
        weaponController.OnAmmoChanged   -= HandleAmmoChanged;
        weaponController.OnChargeChanged -= HandleChargeChanged;
    }

    private void Start()
    {
        // Inicializar con el arma actual si ya tiene una
        if (weaponController.CurrentWeapon != null)
            HandleWeaponChanged(weaponController.CurrentWeapon, weaponController.CurrentAmmo);
        else
            SetEmpty();
    }

   

    private void HandleWeaponChanged(WeaponData data, int ammo)
    {
        if (data == null) { SetEmpty(); return; }

        if (weaponIcon    != null) weaponIcon.sprite  = data.icon;
        if (weaponNameText != null) weaponNameText.text = data.weaponName;

        UpdateAmmoText(ammo, data.maxAmmo);

        // La barra de carga solo aparece para el laser
        bool isLaser = data.type == WeaponData.WeaponType.Laser;
        if (chargeBarRoot != null) chargeBarRoot.SetActive(isLaser);
    }

    private void HandleAmmoChanged(int ammo)
    {
        if (weaponController.CurrentWeapon == null) return;
        UpdateAmmoText(ammo, weaponController.CurrentWeapon.maxAmmo);
    }

    private void HandleChargeChanged(float progress)
    {
        if (chargeBarFill != null)
            chargeBarFill.fillAmount = progress;
    }

    private void UpdateAmmoText(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = current + " / " + max;
    }

    private void SetEmpty()
    {
        if (weaponIcon     != null) weaponIcon.sprite   = null;
        if (weaponNameText != null) weaponNameText.text  = "—";
        if (ammoText       != null) ammoText.text        = "— / —";
        if (chargeBarRoot  != null) chargeBarRoot.SetActive(false);
    }
}
