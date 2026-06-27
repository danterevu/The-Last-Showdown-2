using UnityEngine;

public class DNA : MonoBehaviour
{
    [Header("Spawn (opcional - se sobreescribe desde Manager)")]
    [SerializeField] private GameObject[] defaultSpawnPoints;
    private GameObject[] currentSpawnPoints;

    [Header("Configuración")]
    [SerializeField] private float throwBaseForce = 3f;
    [SerializeField] private float throwRunningForce = 10f;
    [SerializeField] private float throwUpward = 2f;
    [SerializeField] private GameObject[] spawnPoints;
    [SerializeField] private float respawnDelay = 1f;
    private bool isRespawning = false;

    [Header("Física del golpe (cuando es lanzado por golpe enemigo)")]
    [SerializeField] private float hitThrowForceX = 6f;
    [SerializeField] private float hitThrowForceY = 8f;

    [Header("Luz / Brillo")]
    [Tooltip("Arrastrá acá el GO hijo que tiene la luz")]
    [SerializeField] private GameObject glowLight;

    [Header("Bobbing (flotación cuando está libre)")]
    [SerializeField] private float bobbingSpeed = 1.5f;      // velocidad de subida/bajada
    [SerializeField] private float bobbingDistance = 0.15f;  // cuánto sube y baja

    private SpriteRenderer sr;
    private Collider2D triggerCol;
    private Collider2D physicsCol;
    private Rigidbody2D rb;
    private bool isPickedUp = false;
    private PlayerControllerDNA holder;
    private Vector3 originalScale;
    private bool isThrown = false;
    private float throwTime = 0f;
    private int lastThrowerPlayer = -1;

    // Bobbing
    private bool isBobbing = false;
    private Vector3 bobbingOrigin;  // posición base desde donde flota

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

        if (defaultSpawnPoints != null && defaultSpawnPoints.Length > 0)
            currentSpawnPoints = defaultSpawnPoints;
    }

    // ── Luz ─────────────────────────────────────────────────────────
    private void SetGlow(bool active)
    {
        if (glowLight != null)
            glowLight.SetActive(active);
    }

    // ── Bobbing ─────────────────────────────────────────────────────
    private void StartBobbing()
    {
        bobbingOrigin = transform.position;
        isBobbing = true;
    }

    private void StopBobbing()
    {
        isBobbing = false;
        // Restaurar posición exacta al origen para que no quede flotando a medias
        if (!isPickedUp && !isThrown)
            transform.position = bobbingOrigin;
    }

    private void Update()
    {
        if (!isBobbing) return;

        float offset = Mathf.Sin(Time.time * bobbingSpeed * Mathf.PI * 2f) * bobbingDistance;
        transform.position = bobbingOrigin + new Vector3(0f, offset, 0f);
    }

    // ── API pública ─────────────────────────────────────────────────
    public void SetSpawnPoints(GameObject[] newSpawnPoints)
    {
        currentSpawnPoints = newSpawnPoints;
    }

    public void SpawnDNA()
    {
        if (currentSpawnPoints == null || currentSpawnPoints.Length == 0)
            return;

        int index = Random.Range(0, currentSpawnPoints.Length);
        Vector3 spawnPos = currentSpawnPoints[index].transform.position;
        spawnPos.z = 0f;
        transform.position = spawnPos;
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

        SetGlow(true);
        StartBobbing(); // empieza a flotar al spawnear
        isRespawning = false;
    }

    public void RespawnAfterDelay()
    {
        isRespawning = true;
        StopBobbing();
        sr.enabled = false;
        triggerCol.enabled = false;
        physicsCol.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        SetGlow(false);
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

        StopBobbing(); // deja de flotar al ser agarrado
        SetGlow(false);
    }

    // Llamado desde PlayerControllerDNA cuando suelta el DNA
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

        SetGlow(true);
        StartBobbing(); // vuelve a flotar al soltarse
    }

    // Lanzamiento voluntario
    public void Throw(Vector2 direction, float playerSpeed, int throwerPlayer)
    {
        if (!isPickedUp) return;
        holder = null;
        isPickedUp = false;
        isThrown = true;
        throwTime = Time.time;
        lastThrowerPlayer = throwerPlayer;

        StopBobbing(); // sin bobbing mientras vuela

        transform.SetParent(null);
        sr.enabled = true;

        float t = Mathf.Clamp01(playerSpeed / 10f);
        float force = Mathf.Lerp(throwBaseForce, throwRunningForce, t);
        Vector2 throwForce = new Vector2(direction.x * force, throwUpward);

        rb.bodyType = RigidbodyType2D.Dynamic;
        physicsCol.enabled = true;
        triggerCol.enabled = false;
        rb.AddForce(throwForce, ForceMode2D.Impulse);
        rb.angularVelocity = Random.Range(-360f, 360f);

        SetGlow(true);
        Invoke(nameof(EnableTrigger), 0.3f);
    }

    private void EnableTrigger()
    {
        if (!isPickedUp) triggerCol.enabled = true;
    }

    // Lanzamiento forzado (golpe recibido)
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
        lastThrowerPlayer = throwerPlayer;

        StopBobbing(); // sin bobbing mientras vuela

        transform.SetParent(null);
        sr.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        physicsCol.enabled = true;
        triggerCol.enabled = false;
        rb.AddForce(direction * new Vector2(hitThrowForceX, hitThrowForceY), ForceMode2D.Impulse);
        rb.angularVelocity = Random.Range(-360f, 360f);

        SetGlow(true);
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
        if (isPickedUp || isRespawning) return;
        if (isPickedUp) return;
        if (isThrown && Time.time - throwTime < 0.2f) return;

        PlayerControllerDNA controller = collision.GetComponent<PlayerControllerDNA>();
        if (controller == null || controller.HasDNA()) return;

        if (controller.IsCarryingSomething() && controller.HasDNA() == false) return;

        controller.PickDNA(this);
        PickUp(controller);
    }

    public void ForceSpawnNow()
    {
        // Si está agarrado, soltarlo de su holder
        if (holder != null && holder.HasDNA())
            holder.DropDNA();
        // Forzar respawn
        SpawnDNA();
    }

    // Getters
    public bool IsThrown() => isThrown;
    public PlayerControllerDNA GetHolder() => holder;
    public int GetLastThrower() => lastThrowerPlayer;
}