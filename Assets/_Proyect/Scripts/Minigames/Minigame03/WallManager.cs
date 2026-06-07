using UnityEngine;
using System.Collections;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;

    [SerializeField] private float wallDuration = 5f;
    [SerializeField] private Collider2D[] wallColliders;     // arrastras los colliders de las paredes/puertas
    [SerializeField] private SpriteRenderer[] wallRenderers; // arrastras los sprites de las paredes/puertas
    [SerializeField] private Animator[] wallAnimators;      // arrastras los Animators de las puertas
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private string closeAnimationTrigger = "Close";

    private Coroutine deactivateCoroutine;

    private void Awake()
    {
        Instance = this;
        DeactivateAll(); // empiezan desactivadas
    }

    private void OnEnable()
    {
        Deposit.OnAnyDeposit += DeactivateAll;
    }

    private void OnDisable()
    {
        Deposit.OnAnyDeposit -= DeactivateAll;
    }

    public void ActivateAll()
    {
        if (deactivateCoroutine != null)
            StopCoroutine(deactivateCoroutine);

        foreach (var anim in wallAnimators)
            if (anim != null) anim.SetTrigger(openAnimationTrigger);

        foreach (var col in wallColliders)
            if (col != null) col.enabled = true;

        foreach (var sr in wallRenderers)
            if (sr != null) sr.enabled = true;

        deactivateCoroutine = StartCoroutine(DeactivateAfterDelay());
    }

    private IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(wallDuration);
        DeactivateAll();
    }

    public void DeactivateAll()
    {
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }

        foreach (var anim in wallAnimators)
            if (anim != null) anim.SetTrigger(closeAnimationTrigger);

        foreach (var col in wallColliders)
            if (col != null) col.enabled = false;

        foreach (var sr in wallRenderers)
            if (sr != null) sr.enabled = false;
    }
}