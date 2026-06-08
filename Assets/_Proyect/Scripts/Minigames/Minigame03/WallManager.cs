using UnityEngine;
using System.Collections;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;

    [Header("Door Settings")]
    [SerializeField] private Animator[] wallAnimators;
    [SerializeField] private Collider2D[] wallColliders;   // mismo orden que wallAnimators
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private string closeAnimationTrigger = "Close";
    [SerializeField] private float reopenDelay = 3f;
    [SerializeField] private float colliderActivationDelay = 0.5f; // tiempo para activar/desactivar colliders

    private Coroutine deactivateCoroutine;
    private Coroutine reopenCoroutine;

    private void Awake()
    {
        Instance = this;
        ActivateAll(); // empiezan ABIERTAS
    }

    public void ActivateAll()
    {
        if (deactivateCoroutine != null)
            StopCoroutine(deactivateCoroutine);
        if (reopenCoroutine != null)
            StopCoroutine(reopenCoroutine);

        // Abrir puertas (animación)
        foreach (var anim in wallAnimators)
        {
            if (anim != null)
            {
                anim.ResetTrigger(openAnimationTrigger);
                anim.ResetTrigger(closeAnimationTrigger);
                anim.SetTrigger(openAnimationTrigger);
            }
        }

        // Desactivar colliders después de un breve retraso (para que la animación de apertura termine)
        StartCoroutine(SetCollidersState(false, colliderActivationDelay));
    }

    public void DeactivateAll()
    {
        if (deactivateCoroutine != null)
            StopCoroutine(deactivateCoroutine);
        if (reopenCoroutine != null)
            StopCoroutine(reopenCoroutine);

        // Cerrar puertas (animación)
        foreach (var anim in wallAnimators)
        {
            if (anim != null)
            {
                anim.ResetTrigger(openAnimationTrigger);
                anim.ResetTrigger(closeAnimationTrigger);
                anim.SetTrigger(closeAnimationTrigger);
            }
        }

        // Activar colliders inmediatamente (para bloquear al instante) o con retraso
        StartCoroutine(SetCollidersState(true, 0f)); // sin retraso

        // Programar reapertura después de reopenDelay
        reopenCoroutine = StartCoroutine(ReopenDoorsAfterDelay());
    }

    private IEnumerator SetCollidersState(bool active, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var col in wallColliders)
        {
            if (col != null)
                col.enabled = active;
        }
    }

    private IEnumerator ReopenDoorsAfterDelay()
    {
        yield return new WaitForSeconds(reopenDelay);
        ActivateAll();
        reopenCoroutine = null;
    }
}