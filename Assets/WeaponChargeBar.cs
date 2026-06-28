using UnityEngine;
using System.Collections;

public class WeaponChargeBar : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private MeshRenderer cubeMeshRenderer;

    [Header("Configuración Visual")]
    [Tooltip("El ancho máximo (Escala X) que alcanzará el cubo al estar 100% cargado")]
    [SerializeField] private float maxScaleX = 2f;
    [SerializeField] private Color chargingColor = Color.cyan;

    [Header("Pulido: Parpadeo (Full Charge)")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private float blinkSpeed = 0.08f;

    [Header("Pulido: Pálpito de impacto")]
    [SerializeField] private float pulseScaleMultiplier = 1.3f;
    [SerializeField] private float pulseDuration = 0.15f;

    private Vector3 initialScale;
    private Material propertyMaterial;

    private bool playedPulse = false;
    private Coroutine blinkRoutine;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        if (weaponController == null)
            weaponController = GetComponentInParent<WeaponController>();

        if (cubeMeshRenderer == null)
            cubeMeshRenderer = GetComponent<MeshRenderer>();

        initialScale = transform.localScale;

        if (cubeMeshRenderer != null)
        {
            propertyMaterial = cubeMeshRenderer.material;
           
            cubeMeshRenderer.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (weaponController != null)
        {
            weaponController.OnChargeChanged += UpdateBar;
            weaponController.OnWeaponChanged += HandleWeaponChanged;
        }
        UpdateBar(0f);
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.OnChargeChanged -= UpdateBar;
            weaponController.OnWeaponChanged -= HandleWeaponChanged;
        }
        StopVisualEffects();
    }

    private void UpdateBar(float progress)
    {
        // Si no hay carga o no es el láser, limpiamos todo y ocultamos
        if (progress <= 0f || (weaponController.CurrentWeapon != null && weaponController.CurrentWeapon.type != WeaponData.WeaponType.Laser))
        {
            cubeMeshRenderer.enabled = false;
            StopVisualEffects();
            playedPulse = false;
            return;
        }

        if (!cubeMeshRenderer.enabled)
            cubeMeshRenderer.enabled = true;

        // 1. Si está cargando pero aún no llegó al 100%
        if (progress < 1f)
        {
            StopVisualEffects();
            playedPulse = false;

            // Escala normal de carga
            transform.localScale = new Vector3(maxScaleX * progress, initialScale.y, initialScale.z);

            if (propertyMaterial != null)
                propertyMaterial.color = chargingColor;
        }
        // 2. ¡YA ESTÁ LISTO! (Progreso >= 1f)
        else
        {
            // Efecto A: Pequeño pálpito de impacto (se ejecuta una sola vez al llegar a 1)
            if (!playedPulse)
            {
                playedPulse = true;
                if (pulseRoutine != null) StopCoroutine(pulseRoutine);
                pulseRoutine = StartCoroutine(PulseCoroutine());
            }

            // Efecto B: Iniciar parpadeo continuo
            if (blinkRoutine == null)
            {
                blinkRoutine = StartCoroutine(BlinkCoroutine());
            }
        }
    }

    private IEnumerator PulseCoroutine()
    {
        Vector3 targetScale = new Vector3(maxScaleX * pulseScaleMultiplier, initialScale.y * pulseScaleMultiplier, initialScale.z);
        float elapsed = 0f;

        // Crece rápido
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(new Vector3(maxScaleX, initialScale.y, initialScale.z), targetScale, elapsed / pulseDuration);
            yield return null;
        }

        // Vuelve a su tamaño máximo normal
        elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, new Vector3(maxScaleX, initialScale.y, initialScale.z), elapsed / pulseDuration);
            yield return null;
        }
    }

    private IEnumerator BlinkCoroutine()
    {
        while (true)
        {
            if (propertyMaterial != null) propertyMaterial.color = chargingColor;
            yield return new WaitForSeconds(blinkSpeed);

            if (propertyMaterial != null) propertyMaterial.color = readyColor;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    private void StopVisualEffects()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    private void HandleWeaponChanged(WeaponData newWeapon, int currentAmmo)
    {
        UpdateBar(0f);
    }

    private void OnDestroy()
    {
        if (propertyMaterial != null)
            Destroy(propertyMaterial);
    }
}