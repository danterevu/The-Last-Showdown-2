using UnityEngine;
using System.Collections;
public class Mine : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float armDelay = 1f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField]
    private float stunDuration = 1.5f;
    [SerializeField] private float armedAlpha = 0.15f; // casi invisible

    [Header("DNA")]
    [SerializeField] private float dnaThrowForceX = 6f;
    [SerializeField] private float dnaThrowForceY = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D triggerCol;
    private bool isArmed = false;
    private int ownerPlayer; // para no dañar al que la puso

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // buscar el collider trigger
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col.isTrigger) { triggerCol = col; break; }
        }

        triggerCol.enabled = false; // no detecta nada hasta armarse
    }

    public void Init(int owner)
    {
        ownerPlayer = owner;
        StartCoroutine(ArmSequence());
    }

    private IEnumerator ArmSequence()
    {
        // esperar que se arme
        yield return new WaitForSeconds(armDelay);

        // armada: casi invisible
        Color c = sr.color;
        c.a = armedAlpha;
        sr.color = c;

        // detener física y quedarse fija
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        isArmed = true;
        triggerCol.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isArmed) return;

        // solo daña al rival, no al dueño
        int hitPlayer = other.CompareTag("Player1") ? 1 : other.CompareTag("Player2") ? 2 : 0;
        if (hitPlayer == 0 || hitPlayer == ownerPlayer) return;

        PlayerControllerDNA target = other.GetComponent<PlayerControllerDNA>();
        if (target == null) return;

        // dirección de la explosión — aleatoria en X, siempre sube
        float dirX = Random.value > 0.5f ? 1f : -1f;
        Vector2 knockDir = new Vector2(dirX, 0.5f).normalized;

        // knockback + stun al rival
        target.ReceiveMineHit(knockDir, knockbackForce, stunDuration);

        // si tiene DNA, sale volando
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

        Destroy(gameObject);
    }
}
