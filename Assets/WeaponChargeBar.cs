using UnityEngine;

public class WeaponChargeBar : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private SpriteRenderer cubeMeshRenderer;

    [Header("Configuración Visual")]
    [Tooltip("El ancho máximo (Escala X) que alcanzará esta barra al estar 100% cargada")]
    [SerializeField] private float maxScaleX = 3f;
    [SerializeField] private Color readyColor = Color.white;

    private Vector3 initialScale;
    private Material propertyMaterial;
    private float ultimoProgreso = 0f;

  

    private void Awake()
    {
      
        // Si no se asignó en el inspector, buscamos hacia arriba en los padres al jugador
        if (weaponController == null)
            weaponController = GetComponentInParent<WeaponController>();

        if (cubeMeshRenderer != null)
        {
            cubeMeshRenderer.color = readyColor;
            cubeMeshRenderer.enabled = false;
        }

        // Al estar en el hijo, guardamos nuestra propia escala inicial limpia
        initialScale = transform.localScale;

        if (cubeMeshRenderer != null)
        {
            propertyMaterial = cubeMeshRenderer.material;
            if (propertyMaterial != null) propertyMaterial.color = readyColor;

            cubeMeshRenderer.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged += HandleWeaponChanged;
            weaponController.OnChargeChanged += HandleChargeChanged;

            EvaluarEstadoBarra();
        }
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= HandleWeaponChanged;
            weaponController.OnChargeChanged -= HandleChargeChanged;
        }
    }

    private void EvaluarEstadoBarra()
    {
        if (weaponController == null) return;

        bool tieneLaser = weaponController.CurrentWeapon != null &&
                          weaponController.CurrentWeapon.type == WeaponData.WeaponType.Laser;

        if (tieneLaser && ultimoProgreso > 0f)
        {
            ActualizarEscala(ultimoProgreso);
        }
        else
        {
            OcultarBarra();
        }
    }

    private void ActualizarEscala(float progreso)
    {
        if (cubeMeshRenderer != null && !cubeMeshRenderer.enabled)
        {
            cubeMeshRenderer.color = readyColor;
            cubeMeshRenderer.enabled = true;
        }

        float escalaCalculadaX = maxScaleX * Mathf.Clamp01(progreso);
        transform.localScale = new Vector3(escalaCalculadaX, initialScale.y, initialScale.z);
    }
    private void OcultarBarra()
    {
        transform.localScale = new Vector3(0f, initialScale.y, initialScale.z);
        if (cubeMeshRenderer != null)
            cubeMeshRenderer.enabled = false;
    }

    private void HandleWeaponChanged(WeaponData newWeapon, int currentAmmo)
    {
        ultimoProgreso = 0f;
        OcultarBarra();
    }

    private void HandleChargeChanged(float progress)
    {
        ultimoProgreso = progress;

        if (ultimoProgreso <= 0f)
        {
            OcultarBarra();
        }
        else
        {
            if (weaponController.CurrentWeapon != null && weaponController.CurrentWeapon.type == WeaponData.WeaponType.Laser)
            {
                ActualizarEscala(ultimoProgreso);
            }
            else
            {
                OcultarBarra();
            }
        }
    }

    private void OnDestroy()
    {
        if (propertyMaterial != null)
            Destroy(propertyMaterial);
    }
}