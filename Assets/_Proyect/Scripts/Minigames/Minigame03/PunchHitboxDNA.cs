using UnityEngine;

public class PunchHitboxDNA : MonoBehaviour
{
    private BoxCollider2D hitbox;
    private PlayerControllerDNA owner;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        owner = GetComponentInParent<PlayerControllerDNA>();
        hitbox.enabled = false; // empieza desactivada
    }

    public void Activate() => hitbox.enabled = true;
    public void Deactivate() => hitbox.enabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitbox.enabled) return;

        // Destruir caja si golpea
        Crate crate = other.GetComponent<Crate>();
        if (crate != null)
        {
            crate.DestroyCrate();
            Deactivate();
            return;
        }

        //Definimos al Target (al otro jugador)
        PlayerControllerDNA target = other.GetComponentInParent<PlayerControllerDNA>();
        if (target == null || target == owner) return;

        // Dirección del golpe según a dónde mira el jugador
        float dirX = owner.IsFacingRight() ? 1f : -1f;
        Vector2 knockDir = new Vector2(dirX, 0.3f).normalized;
        

        // Si el rival tiene DNA, lo lanzamos
        if (target.HasDNA() && target.GetCarriedDNA() != null)
        {
            target.GetCarriedDNA().transform.position = target.transform.position;
            target.GetCarriedDNA().gameObject.SetActive(true);

            // Dirección random: X mantiene la dirección del golpe pero con variación,
            // Y siempre va hacia arriba pero con variación
            Vector2 throwDir = new Vector2(dirX * Random.Range(-1f, 1f),Random.Range(-1f, 1f));

            target.GetCarriedDNA().ThrowDNA(throwDir);
            target.GetCarriedDNA().SetSpinEffect(); // rotación en el aire
            target.DropDNA();
        }
        // Knockback al rival
        target.ReceiveKnockback(knockDir);

        // Mini knockback al atacante en dirección contraria
        owner.ApplySelfKnockback(dirX);

        Deactivate();
    }
}
