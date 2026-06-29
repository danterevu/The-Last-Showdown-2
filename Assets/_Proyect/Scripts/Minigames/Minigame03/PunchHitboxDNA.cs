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
    [SerializeField] private float wallCheckRadius = 1f;             // radio para detectar pared
    [SerializeField] private float wallCheckDelay = 0.12f;             // tiempo tras golpe para comprobar pared

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        owner = GetComponentInParent<PlayerControllerDNA>();
        hitbox.enabled = false; // empieza desactivada
    }

    public void Activate()
    {
        if (owner.HasDNA()) return; // no activar si tiene ADN
        hitbox.enabled = true;
    }
    public void Deactivate() => hitbox.enabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitbox.enabled) return;

        // 1. Detectar si golpea a un jugador
        PlayerControllerDNA target = other.GetComponentInParent<PlayerControllerDNA>();
        if (target != null && target != owner)
        {

            // --- PRIORIDAD: ESCUDO ---
            if (target.IsShieldActive())
            {
                float dirXShield = owner.IsFacingRight() ? 1f : -1f;
                Vector2 knockDirShield = new Vector2(dirXShield, 0.3f).normalized;
                float finalKnockback = owner.knockbackForce * (owner.IsBerserk() ? berserkKnockbackMultiplier : 1f);
                Debug.Log($"[PRE-KNOCK] Golpe a {target.name}. ¿Shield? {target.IsShieldActive()}. ¿Caja? {target.IsCarryingCrate()}.");
                target.ReceiveKnockback(knockDirShield, finalKnockback, owner);
                target.NotifyPowerUpHit(target.CompareTag("Player1") ? 1 : 2);
                Deactivate();
                return;
            }

            // --- GOLPE NORMAL ---
            float dirX = owner.IsFacingRight() ? 1f : -1f;
            Vector2 knockDir;
            float finalKnockbackForce = owner.knockbackForce;

            // Ajustar knockback si está en Berserk
            if (owner.IsBerserk())
            {
                target.NotifyPowerUpHit(owner.CompareTag("Player1") ? 1 : 2);
                knockDir = new Vector2(dirX * 0.8f, 1.2f).normalized;
                finalKnockbackForce = owner.knockbackForce * berserkKnockbackMultiplier;
                target.ReceiveBerserkHit(knockDir);
            }
            else
            {
                knockDir = new Vector2(dirX, 0.3f).normalized;
                finalKnockbackForce = owner.knockbackForce;
            }

            // --- Lanzar caja si el objetivo la tiene (antes del knockback) ---
            if (target.IsCarryingCrate())
            {
                Crate crate = target.GetCarriedCrate();
                if (crate != null)
                {
                    // La caja agarrada NUNCA se destruye, solo se lanza
                    int throwerPlayer = owner.CompareTag("Player1") ? 1 : 2;
                    // Usar la dirección del golpe (misma que el knockback) para lanzar la caja
                    crate.ThrowByHit(knockDir, throwerPlayer);
                    target.ForceDropCrate(); // Limpia la referencia en el jugador
                }
            }

            // --- Lanzar DNA si el objetivo lo tiene ---
            if (target.HasDNA() && target.GetCarriedDNA() != null)
            {
                DNA dna = target.GetCarriedDNA();
                dna.transform.position = target.transform.position;
                dna.gameObject.SetActive(true);
                Vector2 throwDir = new Vector2(dirX * Random.Range(0.8f, 1.2f), Random.Range(0.5f, 1f)).normalized;
                dna.ThrowByHit(throwDir, owner.CompareTag("Player1") ? 1 : 2);
                dna.SetSpinEffect();
                target.DropDNA();
            }

            // --- Aplicar knockback al objetivo ---
            target.ReceiveKnockback(knockDir, finalKnockbackForce, owner);

            // --- Self-knockback al atacante ---
            owner.ApplySelfKnockback(dirX);

            // --- Si está en Berserk, comprobar colisión con pared ---
            if (owner.IsBerserk())
            {
                StartCoroutine(CheckWallCollisionAfterKnockback(target, extraStunOnWall));
            }

            Deactivate();
            return;
        }

        // 2. Si no golpeó a un jugador, puede ser una caja suelta
        Crate looseCrate = other.GetComponent<Crate>();
        if (looseCrate != null)
        {
            // Solo destruir si NO está agarrada
            if (!looseCrate.IsHeld())
            {
                looseCrate.DestroyCrate();
            }
            Deactivate();
            return;
        }
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
