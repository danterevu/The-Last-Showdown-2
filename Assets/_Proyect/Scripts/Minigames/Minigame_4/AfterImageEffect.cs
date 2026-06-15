using UnityEngine;
using System.Collections;

public class AfterImageEffect : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float interval = 0.05f;   // cada cuanto spawnea una imagen
    [SerializeField] private float lifetime = 0.3f;    // cuanto dura cada imagen
    [SerializeField] private Color color = new Color(0.5f, 0.3f, 1f, 0.6f); // color del after image
    [SerializeField] private bool flipped = false;     // flipear horizontalmente

    private SpriteRenderer sr;
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void StartEffect(bool flip)
    {
        flipped = flip;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopEffect()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        GameObject[] ghosts = GameObject.FindGameObjectsWithTag("AfterImage");

        foreach (GameObject g in ghosts)
            Destroy(g);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnImage();
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnImage()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject ghost = new GameObject("AfterImage");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();
        ghostSR.sprite = sr.sprite;
        ghostSR.sortingLayerName = sr.sortingLayerName;
        ghostSR.sortingOrder = sr.sortingOrder - 1;
        ghostSR.color = color;

        // flipear igual que el jugador, con opcion de invertir
        ghostSR.flipX = flipped ? !sr.flipX : sr.flipX;
        ghostSR.flipY = sr.flipY;

        StartCoroutine(FadeAndDestroy(ghostSR, ghost));
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer ghostSR, GameObject ghost)
    {
        float elapsed = 0f;
        Color startColor = ghostSR.color;

        while (elapsed < lifetime)
        {
            if (ghostSR == null) yield break;
            float t = elapsed / lifetime;
            ghostSR.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ghost != null) Destroy(ghost);
    }
}