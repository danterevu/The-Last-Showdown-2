using UnityEngine;
using System.Collections;

public class HolographicTile : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteCracked;
    [SerializeField] private Sprite spriteBroken;

    [Header("Settings")]
    [SerializeField] private float breakDelay = 0.5f;

    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D solidCollider;   // el suelo real
    [SerializeField] private Collider2D stepTrigger;     // deteccion de pisada

    public enum TileState { Normal, Cracked, Broken }

    [Header("Debug")]
    [SerializeField] private TileState currentState = TileState.Normal;

    private bool isBreaking = false; // esta en el delay de rotura?

    // TileMapManager lo llama al instanciar para asignar sprites por codigo
    public void Initialize(Sprite normal, Sprite cracked, Sprite broken, float delay)
    {
        spriteNormal = normal;
        spriteCracked = cracked;
        spriteBroken = broken;
        breakDelay = delay;
        SetState(TileState.Normal);
    }

    // DeathZone llama RebuildAll que llama esto en cada tile
    public void ResetTile()
    {
        StopAllCoroutines();
        isBreaking = false;
        SetState(TileState.Normal);
    }

    // el trigger de pisada detecta al jugador
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;
        if (isBreaking) return; // ya esta rompiendo, ignorar

        OnPlayerStep();
    }

    private void OnPlayerStep()
    {
        switch (currentState)
        {
            case TileState.Normal:
                SetState(TileState.Cracked);
                break;

            case TileState.Cracked:
                StartCoroutine(BreakRoutine());
                break;

            case TileState.Broken:
                // ya roto, no hace nada
                break;
        }
    }

    private IEnumerator BreakRoutine()
    {
        isBreaking = true;

        // feedback visual durante el delay: parpadeo
        float elapsed = 0f;
        bool visible = true;
        while (elapsed < breakDelay)
        {
            visible = !visible;
            sr.enabled = visible;
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        sr.enabled = true;
        SetState(TileState.Broken);
        isBreaking = false;
    }

    private void SetState(TileState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case TileState.Normal:
                sr.sprite = spriteNormal;
                solidCollider.enabled = true;
                stepTrigger.enabled = true;
                sr.enabled = true;
                break;

            case TileState.Cracked:
                sr.sprite = spriteCracked;
                solidCollider.enabled = true;  // todavia pisable
                stepTrigger.enabled = true;
                break;

            case TileState.Broken:
                sr.sprite = spriteBroken;
                solidCollider.enabled = false; // ya no es suelo
                stepTrigger.enabled = false;   // ya no detecta pisadas
                break;
        }
    }

    public TileState GetState() => currentState;
}