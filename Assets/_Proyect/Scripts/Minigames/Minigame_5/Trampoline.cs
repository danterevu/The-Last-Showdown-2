using UnityEngine;



public class Trampoline : MonoBehaviour
{
    public enum BounceDirection { Up, Right, Left, Down }

    [Header("Rebote")]
    [SerializeField] private BounceDirection bounceDirection = BounceDirection.Up;
    [Tooltip("Fuerza del rebote. Para Up, reemplaza la velocityY del jugador.")]
    [SerializeField] private float bounceForce = 18f;
    [Tooltip("Si true, conserva la velocidad horizontal al rebotar hacia arriba.")]
    [SerializeField] private bool preserveHorizontalVelocity = true;

    [Header("Visual / Audio")]
    [SerializeField] private Animator animator;
    [Tooltip("Nombre del trigger del animator al activarse.")]
    [SerializeField] private string animTriggerName = "bounce";

    [Header("Cooldown")]
    [Tooltip("Tiempo mínimo entre rebotes del mismo jugador (para evitar doble rebote).")]
    [SerializeField] private float bounceCooldown = 0.3f;

    // Cooldown por jugador (hasta 2)
    private float[] cooldownTimers = new float[2];

    private void Update()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
            if (cooldownTimers[i] > 0f)
                cooldownTimers[i] -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ChaseRunPlayerController player = other.GetComponent<ChaseRunPlayerController>();
        if (player == null) return;

        int idx = player.PlayerNumber - 1;  // 0 o 1
        if (idx < 0 || idx >= cooldownTimers.Length) return;
        if (cooldownTimers[idx] > 0f) return;

        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        ApplyBounce(playerRb);
        cooldownTimers[idx] = bounceCooldown;

        if (animator != null && !string.IsNullOrEmpty(animTriggerName))
            animator.SetTrigger(animTriggerName);
    }

    private void ApplyBounce(Rigidbody2D playerRb)
    {
        Vector2 vel = playerRb.linearVelocity;

        switch (bounceDirection)
        {
            case BounceDirection.Up:
                playerRb.linearVelocity = new Vector2(
                    preserveHorizontalVelocity ? vel.x : 0f,
                    bounceForce
                );
                break;

            case BounceDirection.Down:
                playerRb.linearVelocity = new Vector2(
                    preserveHorizontalVelocity ? vel.x : 0f,
                    -bounceForce
                );
                break;

            case BounceDirection.Right:
                playerRb.linearVelocity = new Vector2(bounceForce, vel.y);
                break;

            case BounceDirection.Left:
                playerRb.linearVelocity = new Vector2(-bounceForce, vel.y);
                break;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        var c = GetComponent<Collider2D>();
        if (c != null)
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);

        // Flecha indicando dirección de rebote
        Gizmos.color = Color.cyan;
        Vector3 dir = bounceDirection switch
        {
            BounceDirection.Up => Vector3.up,
            BounceDirection.Down => -Vector3.up,
            BounceDirection.Right => Vector3.right,
            BounceDirection.Left => -Vector3.right,
            _ => Vector3.up
        };
        Gizmos.DrawRay(transform.position, dir * 0.8f);
    }
#endif
}
