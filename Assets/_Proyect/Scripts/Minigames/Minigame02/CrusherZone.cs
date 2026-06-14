using UnityEngine;
using System.Collections;


/// Colocar este componente en un GameObject vacío de cada zona.
/// Asignar en el Inspector:
///   - alertObject    el prefab/objeto de ALERTA (inactivo al inicio)
///   - crusherObject  el prefab/objeto de APLASTADORA (inactivo al inicio)
/// El PowerUpEffects llama a Activate() cuando el jugador usa el power up.

public class CrusherZone : MonoBehaviour
{
    [Header("Prefabs de la zona (asignar inactivos)")]
    [SerializeField] private GameObject alertObject;
    [SerializeField] private GameObject crusherObject;

    [Header("Tiempos")]
    [Tooltip("Cuánto dura la alerta antes de que aparezca la aplastadora")]
    [SerializeField] private float alertDuration = 2f;

    [Tooltip("Cuánto tiempo permanece visible la aplastadora antes de desactivarse")]
    [SerializeField] private float crusherDuration = 1.5f;

    [Header("Animación")]
    [Tooltip("Nombre del trigger en el Animator de la aplastadora")]
    [SerializeField] private string crusherAnimTrigger = "Crush";

    private Animator crusherAnimator;
    private bool isActive = false;

    private void Awake()
    {
        // Asegurarse de que ambos objetos empiezan inactivos
        if (alertObject != null) alertObject.SetActive(false);
        if (crusherObject != null) crusherObject.SetActive(false);

        if (crusherObject != null)
            crusherAnimator = crusherObject.GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Llamado desde PowerUpEffects cuando se activa el power up de aplastadora.
    /// </summary>
    public void Activate()
    {
        if (isActive) return;
        StartCoroutine(CrusherSequence());
    }

    private IEnumerator CrusherSequence()
    {
        isActive = true;

        // --- FASE 1: Mostrar alerta ---
        if (alertObject != null)
            alertObject.SetActive(true);

        yield return new WaitForSeconds(alertDuration);

        // --- FASE 2: Ocultar alerta, mostrar aplastadora ---
        if (alertObject != null)
            alertObject.SetActive(false);

        if (crusherObject != null)
        {
            crusherObject.SetActive(true);

            if (crusherAnimator != null)
                crusherAnimator.SetTrigger(crusherAnimTrigger);
        }

        // La aplastadora detecta jugadores a través de su propio trigger (CrusherKillZone)
        yield return new WaitForSeconds(crusherDuration);

        // --- FASE 3: Desactivar aplastadora ---
        if (crusherObject != null)
            crusherObject.SetActive(false);

        isActive = false;
    }

    /// <summary>
    /// Cancela la secuencia (ej: cambio de zona)
    /// </summary>
    public void Cancel()
    {
        StopAllCoroutines();
        if (alertObject != null) alertObject.SetActive(false);
        if (crusherObject != null) crusherObject.SetActive(false);
        isActive = false;
    }
}