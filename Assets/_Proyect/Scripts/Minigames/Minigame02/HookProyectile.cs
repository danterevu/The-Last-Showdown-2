using System;
using System.Collections.Generic;
using UnityEngine;

public class HookProjectile : MonoBehaviour
{
    public enum HitType { Ground, Player, None }

    public event Action<HitType, Vector2, Collider2D> OnHit;

    private bool hasHit = false;
    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (hasHit) return;
        hasHit = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        HitType type = HitType.None;
        if (col.collider.CompareTag("Player1") || col.collider.CompareTag("Player2"))
            type = HitType.Player;
        else
            type = HitType.Ground;

        OnHit?.Invoke(type, transform.position, col.collider);
    }

    // Timeout: si no toca nada en X segundos, avisa None
    public IEnumerator<float> Timeout(float seconds)
    {
        float t = 0f;
        while (t < seconds && !hasHit)
        {
            t += Time.deltaTime;
            yield return 0f;
        }
        if (!hasHit)
        {
            hasHit = true;
            OnHit?.Invoke(HitType.None, transform.position, null);
        }
    }
    public void ForceTimeout()
    {
        if (!hasHit)
        {
            hasHit = true;
            OnHit?.Invoke(HitType.None, transform.position, null);
        }
    }
}