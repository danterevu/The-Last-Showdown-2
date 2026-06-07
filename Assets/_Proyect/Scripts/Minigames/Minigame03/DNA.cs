using UnityEngine;

public class DNA : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float throwBaseForce = 3f;
    [SerializeField] private float throwRunningForce = 10f;
    [SerializeField] private float throwUpward = 2f;
    [SerializeField] private GameObject[] spawnPoints;
    [SerializeField] private float respawnDelay = 1f;

    [Header("Física del golpe (cuando es lanzado por golpe enemigo)")]
    [SerializeField] private float hitThrowForceX = 6f;
    [SerializeField] private float hitThrowForceY = 8f;

    private SpriteRenderer sr;
    private Collider2D triggerCol;
    private Collider2D physicsCol;
    private Rigidbody2D rb;
    private bool isPickedUp = false;
    private PlayerControllerDNA holder;
    private Vector3 originalScale;
    private bool isThrown = false;
    private float throwTime = 0f;
    private int lastThrowerPlayer = -1; // opcional para saber quién lanzó

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;

        foreach (var col in GetComponents<Collider2D>())
        {
            if (col.isTrigger) triggerCol = col;
            else physicsCol = col;
        }
    }

    public void SpawnDNA()
    {
        if (spawnPoints.Length == 0) return;
        int index = Random.Range(0, spawnPoints.Length);
        transform.position = spawnPoints[index].transform.position;
        transform.rotation = Quaternion.identity;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        isPickedUp = false;
        holder = null;
        sr.enabled = true;
        triggerCol.enabled = true;
        physicsCol.enabled = false;
        isThrown = false;
        lastThrowerPlayer = -1;
    }

    public void RespawnAfterDelay()
    {
        sr.enabled = false;
        triggerCol.enabled = false;
        physicsCol.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(SpawnDNA), respawnDelay);
    }

    // Llamado desde PlayerControllerDNA cuando el jugador agarra el DNA
    public void PickUp(PlayerControllerDNA newHolder)
    {
        if (isPickedUp) return;
        holder = newHolder;
        isPickedUp = true;
        sr.enabled = true;
        triggerCol.enabled = false;
        physicsCol.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        lastThrowerPlayer = -1;
    }

    // Llamado desde PlayerControllerDNA cuando suelta el DNA (por depósito o pérdida)
    public void Drop()
    {
        if (!isPickedUp) return;
        holder = null;
        isPickedUp = false;
        transform.SetParent(null);
        sr.enabled = true;
        triggerCol.enabled = true;
        physicsCol.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        // No desactivar el gameObject
    }

    // Lanzamiento voluntario (desde el jugador que lo tiene)
    public void Throw(Vector2 direction, float playerSpeed, int throwerPlayer)
    {
        if (!isPickedUp) return;
        holder = null;
        isPickedUp = false;
        isThrown = true;
        throwTime = Time.time;
        lastThrowerPlayer = throwerPlayer;

        transform.SetParent(null);
        sr.enabled = true;

        float t = Mathf.Clamp01(playerSpeed / 10f);
        float force = Mathf.Lerp(throwBaseForce, throwRunningForce, t);
        Vector2 throwForce = new Vector2(direction.x * force, throwUpward);

        rb.bodyType = RigidbodyType2D.Dynamic;
        physicsCol.enabled = true;
        triggerCol.enabled = false; // evitar re-agarre inmediato
        rb.AddForce(throwForce, ForceMode2D.Impulse);
        rb.angularVelocity = Random.Range(-360f, 360f);

        Invoke(nameof(EnableTrigger), 0.3f);
    }

    private void EnableTrigger()
    {
        if (!isPickedUp) triggerCol.enabled = true;
    }

    // Lanzamiento forzado (cuando el jugador que lo lleva recibe un golpe)
    public void ThrowByHit(Vector2 direction, int throwerPlayer)
    {
        if (isPickedUp && holder != null)
        {
            holder.DropDNA();
        }
        isPickedUp = false;
        holder = null;
        isThrown = true;
        throwTime = Time.time;
        lastThrowerPlayer = throwerPlayer;   //  asignar quién causó el lanzamiento

        transform.SetParent(null);
        sr.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        physicsCol.enabled = true;
        triggerCol.enabled = false;
        rb.AddForce(direction * new Vector2(hitThrowForceX, hitThrowForceY), ForceMode2D.Impulse);
        rb.angularVelocity = Random.Range(-360f, 360f);
        Invoke(nameof(EnableTrigger), 0.5f);
    }

    public void SetSpinEffect()
    {
        rb.angularVelocity = Random.Range(200f, 400f) * (Random.value > 0.5f ? 1f : -1f);
    }

    private void LateUpdate()
    {
        if (isPickedUp && holder != null && holder.GetDNAHoldPoint() != null)
        {
            Transform holdPoint = holder.GetDNAHoldPoint();
            transform.position = holdPoint.position;
            transform.rotation = holdPoint.rotation;
            transform.localScale = originalScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPickedUp) return;
        if (isThrown && Time.time - throwTime < 0.2f) return;

        PlayerControllerDNA controller = collision.GetComponent<PlayerControllerDNA>();
        if (controller == null || controller.HasDNA()) return;

        if (controller.IsCarryingSomething() && controller.HasDNA() == false) return;

        controller.PickDNA(this);
        PickUp(controller);
    }

    // Getters
    public bool IsThrown() => isThrown;
    public PlayerControllerDNA GetHolder() => holder;
    public int GetLastThrower() => lastThrowerPlayer;
}