using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HolographicTile : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteCracked;
    [SerializeField] private Sprite spriteBroken;

    [Header("Settings")]
    [SerializeField] private float breakDelay = 0.5f;
    [SerializeField] private float standStillBreakTime = 3f; // Aumentado a 3 segundos

    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private BoxCollider2D stepTrigger;

    public enum TileState { Normal, Cracked, Broken }

    [Header("Debug")]
    [SerializeField] private TileState currentState = TileState.Normal;

    private bool isBreaking = false;
    private HolographicPlatforms gameManager;

    private HashSet<string> playersOnTile = new HashSet<string>();
    private Dictionary<string, float> stayTimers = new Dictionary<string, float>();
    private HashSet<string> playersAlreadyNotified = new HashSet<string>();

    public void Initialize(Sprite normal, Sprite cracked, Sprite broken, float delay, float size, HolographicPlatforms manager)
    {
        spriteNormal = normal;
        spriteCracked = cracked;
        spriteBroken = broken;
        breakDelay = delay;
        gameManager = manager;

        stepTrigger.size = new Vector2(size * 0.6f, size * 0.6f);
        stepTrigger.offset = Vector2.zero;

        SetState(TileState.Normal);
    }

    public void ResetTile()
    {
        StopAllCoroutines();
        isBreaking = false;
        playersOnTile.Clear();
        stayTimers.Clear();
        playersAlreadyNotified.Clear();
        SetState(TileState.Normal);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        if (currentState == TileState.Broken)
        {
            NotifyFall(other.tag);
            return;
        }

        playersOnTile.Add(other.tag);
        stayTimers[other.tag] = 0f;
        ProgressState();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (currentState == TileState.Broken) return;
        if (isBreaking) return;

        // Obtener velocidad del jugador
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        bool isMoving = rb.linearVelocity.magnitude > 0.1f;

        string tag = other.tag;
        if (!stayTimers.ContainsKey(tag)) stayTimers[tag] = 0f;

        if (!isMoving)
        {
            stayTimers[tag] += Time.deltaTime;
            if (stayTimers[tag] >= standStillBreakTime)
            {
                stayTimers[tag] = 0f;
                ProgressState();
            }
        }
        else
        {
            stayTimers[tag] = 0f; // Reiniciar si se mueve
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playersOnTile.Remove(other.tag);
        stayTimers.Remove(other.tag);
    }

    private void NotifyFall(string playerTag)
    {
        if (playersAlreadyNotified.Contains(playerTag)) return;
        playersAlreadyNotified.Add(playerTag);
        int playerNum = playerTag == "Player1" ? 1 : 2;
        gameManager?.OnPlayerFell(playerNum);
    }

    private void ProgressState()
    {
        if (isBreaking) return;
        switch (currentState)
        {
            case TileState.Normal:
                SetState(TileState.Cracked);
                break;
            case TileState.Cracked:
                StartCoroutine(BreakRoutine());
                break;
        }
    }

    private IEnumerator BreakRoutine()
    {
        isBreaking = true;

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

        foreach (string tag in new HashSet<string>(playersOnTile))
            NotifyFall(tag);
    }

    private void SetState(TileState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case TileState.Normal:
                sr.sprite = spriteNormal;
                stepTrigger.enabled = true;
                sr.enabled = true;
                break;
            case TileState.Cracked:
                sr.sprite = spriteCracked;
                stepTrigger.enabled = true;
                break;
            case TileState.Broken:
                sr.sprite = spriteBroken;
                stepTrigger.enabled = true;
                break;
        }
    }

    private bool IsPlayer(Collider2D col) => col.CompareTag("Player1") || col.CompareTag("Player2");

    public TileState GetState() => currentState;
}