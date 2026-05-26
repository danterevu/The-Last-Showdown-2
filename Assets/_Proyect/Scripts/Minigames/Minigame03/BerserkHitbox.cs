using UnityEngine;

public class BerserkHitbox : MonoBehaviour
{
    private BoxCollider2D hitbox;
    private PlayerControllerDNA owner;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        owner = GetComponentInParent<PlayerControllerDNA>();
        hitbox.enabled = false;
    }

    public void Activate() => hitbox.enabled = true;
    public void Deactivate() => hitbox.enabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitbox.enabled) return;

        PlayerControllerDNA target = other.GetComponentInParent<PlayerControllerDNA>();
        if (target == null || target == owner) return;

        float dirX = owner.IsFacingRight() ? 1f : -1f;
        Vector2 knockDir = new Vector2(dirX, 0.3f).normalized;

        // Si tiene DNA, lo suelta
        if (target.HasDNA() && target.GetCarriedDNA() != null)
        {
            target.GetCarriedDNA().transform.position = target.transform.position;
            target.GetCarriedDNA().gameObject.SetActive(true);
            Vector2 throwDir = new Vector2(
                dirX * Random.Range(0.8f, 1.2f),
                Random.Range(0.8f, 1.2f)
            );
            target.GetCarriedDNA().ThrowDNA(throwDir);
            target.GetCarriedDNA().SetSpinEffect();
            target.DropDNA();
        }

        // Knockback + stun de 1 segundo
        target.ReceiveBerserkHit(knockDir);
    }
}
