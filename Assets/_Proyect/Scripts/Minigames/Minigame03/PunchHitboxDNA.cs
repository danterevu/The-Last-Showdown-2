using System.Collections;
using UnityEngine;

public class PunchHitboxDNA : MonoBehaviour
{
    private BoxCollider2D hitbox;
    private PlayerControllerDNA owner;

    [Header("Berserk")]
    [SerializeField] private float berserkKnockbackMultiplier = 2.5f;   // multiplicador de fuerza total (antes 1.8)
    [SerializeField] private float berserkVerticalMultiplier = 1.5f;   // aumento extra en Y (para que vuele más alto)
    [SerializeField] private float extraStunOnWall = 2f;               // segundos extra si choca con pared (antes 1)
    [SerializeField] private float wallCheckRadius = 0.6f;             // radio para detectar pared
    [SerializeField] private float wallCheckDelay = 0.12f;             // tiempo tras golpe para comprobar pared

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

        PlayerControllerDNA target = other.GetComponentInParent<PlayerControllerDNA>();
        if (target == null || target == owner) return;

        // Dirección base del golpe
        float dirX = owner.IsFacingRight() ? 1f : -1f;

        // Si está en Berserk, la dirección tiene mucha más vertical
        Vector2 knockDir;
        float finalKnockbackForce = owner.knockbackForce;

        if (owner.IsBerserk())
        {
            // Dirección con mucha más altura (0.7 horizontal, 0.7 vertical ? normalizado da ~0.7 cada uno)
            // O puedes usar (dirX, 1.2f) y normalizar. Yo pondré (dirX * 0.8f, 1.2f)
            knockDir = new Vector2(dirX * 0.8f, 1.2f).normalized;
            finalKnockbackForce = owner.knockbackForce * berserkKnockbackMultiplier;
        }
        else
        {
            knockDir = new Vector2(dirX, 0.3f).normalized;
            finalKnockbackForce = owner.knockbackForce;
        }

        // Aplicar knockback
        target.ReceiveKnockback(knockDir, finalKnockbackForce);

        // Si tiene DNA, lanzarlo
        if (target.HasDNA() && target.GetCarriedDNA() != null)
        {
            DNA dna = target.GetCarriedDNA();
            dna.transform.position = target.transform.position;
            dna.gameObject.SetActive(true);
            Vector2 throwDir = new Vector2(dirX * Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
            dna.ThrowDNA(throwDir);
            dna.SetSpinEffect();
            target.DropDNA();
        }

        // Self-knockback al atacante
        owner.ApplySelfKnockback(dirX);

        //  Si está en Berserk, comprobar si el rival choca con pared
        if (owner.IsBerserk())
        {
            StartCoroutine(CheckWallCollisionAfterKnockback(target, extraStunOnWall));
        }

        Deactivate();
    }

    private IEnumerator CheckWallCollisionAfterKnockback(PlayerControllerDNA target, float extraStun)
    {
        yield return new WaitForSeconds(wallCheckDelay);

        // Verificar si el rival está tocando una pared (capa "Walls")
        Collider2D[] hits = Physics2D.OverlapCircleAll(target.transform.position, wallCheckRadius, LayerMask.GetMask("Walls"));
        if (hits.Length > 0)
        {
            // Añadir tiempo de stun extra (no reiniciar, sumar)
            target.AddStunTime(extraStun);
            Debug.Log(target.name + " chocó contra pared por Berserk, +" + extraStun + "s de stun");
        }
    }
}
