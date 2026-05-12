using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    private BoxCollider2D hitbox;
    private PlatformPlayerController owner;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        owner = GetComponentInParent<PlatformPlayerController>();
        hitbox.enabled = false; // empieza desactivada
    }


    public void Activate() => hitbox.enabled = true;
    public void Deactivate() => hitbox.enabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitbox.enabled) return;

        PlatformPlayerController target = other.GetComponentInParent<PlatformPlayerController>();
        if (target == null || target == owner) return;

        // Dirección del golpe según a dónde mira el jugador
        float dirX = owner.IsFacingRight() ? 1f : -1f;
        Vector2 knockDir = new Vector2(dirX, 0.3f).normalized;

        // Knockback al rival
        target.ReceiveKnockback(knockDir);

        // Mini knockback al atacante en dirección contraria
        owner.GetRigidbody().linearVelocity = new Vector2(-dirX * owner.SelfKnockback, owner.SelfKnockback * 0.3f);

        Deactivate();
    }
}