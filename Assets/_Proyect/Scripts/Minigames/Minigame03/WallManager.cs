using UnityEngine;
using System.Collections;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;

    [SerializeField] private float wallDuration = 5f;
    [SerializeField] private Collider2D[] wallColliders;     // arrastrás los colliders de las paredes
    [SerializeField] private SpriteRenderer[] wallRenderers; // arrastrás los sprites de las paredes

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

        foreach (var col in wallColliders)
            if (col != null) col.enabled = false;

        foreach (var sr in wallRenderers)
            if (sr != null) sr.enabled = false;
    }
}