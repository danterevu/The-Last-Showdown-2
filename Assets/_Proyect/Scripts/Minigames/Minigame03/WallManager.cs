using UnityEngine;
using System.Collections;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;

    [Header("Door Settings")]
    [SerializeField] private Animator[] wallAnimators;      // arrastras los Animators de las puertas
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private string closeAnimationTrigger = "Close";
    [SerializeField] private float reopenDelay = 3f; // Tiempo en segundos para volver a abrir las puertas (configurable)

    private Coroutine deactivateCoroutine;
    private Coroutine reopenCoroutine;

    private void Awake()
    {
        Instance = this;
        ActivateAll(); // empiezan ABIERTAS (usamos el trigger "Open")
        Debug.Log("WallManager: Initialized - doors start OPEN");
    }

    private void OnEnable()
    {
        // Quitamos la suscripción para que no se desactiven al depositar
        // Deposit.OnAnyDeposit += DeactivateAll;
    }

    private void OnDisable()
    {
        // Deposit.OnAnyDeposit -= DeactivateAll;
    }

    public void ActivateAll()
    {
        if (deactivateCoroutine != null)
            StopCoroutine(deactivateCoroutine);

        Debug.Log($"WallManager: Opening {wallAnimators.Length} doors with trigger: {openAnimationTrigger}");
        foreach (var anim in wallAnimators)
        {
            if (anim != null)
            {
                Debug.Log($"  - GameObject: {anim.gameObject.name}");
                
                // Reset both triggers first
                anim.ResetTrigger(openAnimationTrigger);
                anim.ResetTrigger(closeAnimationTrigger);
                
                // Set the new trigger
                anim.SetTrigger(openAnimationTrigger);
            }
        }
    }

    public void DeactivateAll()
    {
        // Stop any existing coroutines
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }
        if (reopenCoroutine != null)
        {
            StopCoroutine(reopenCoroutine);
            reopenCoroutine = null;
        }

        Debug.Log($"WallManager: Closing {wallAnimators.Length} doors with trigger: {closeAnimationTrigger}");
        foreach (var anim in wallAnimators)
        {
            if (anim != null)
            {
                Debug.Log($"  - GameObject: {anim.gameObject.name}");
                
                // Reset both triggers first
                anim.ResetTrigger(openAnimationTrigger);
                anim.ResetTrigger(closeAnimationTrigger);
                
                // Set the new trigger
                anim.SetTrigger(closeAnimationTrigger);
            }
        }

        // Start coroutine to reopen doors after delay
        reopenCoroutine = StartCoroutine(ReopenDoorsAfterDelay());
    }

    private IEnumerator ReopenDoorsAfterDelay()
    {
        Debug.Log($"WallManager: Doors will reopen in {reopenDelay} seconds...");
        yield return new WaitForSeconds(reopenDelay);
        Debug.Log("WallManager: Reopening doors now!");
        ActivateAll();
        reopenCoroutine = null;
    }
}