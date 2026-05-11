
using UnityEngine;

public class DNA : MonoBehaviour
{
    private SpriteRenderer sr;
    private Collider2D triggerCol;   // detecta jugadores
    private Collider2D physicsCol;   // rebota con paredes
    private Rigidbody2D rb;

    [SerializeField] private GameObject[] spawnPoints;
    [SerializeField] private float respawnDelay = 1f;

    [Header("Física del golpe")]
    [SerializeField] private float throwForceX = 6f;
    [SerializeField] private float throwForceY = 8f;

    private bool isPickedUp = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

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
        Vector3 spawnPos = spawnPoints[index].transform.position;
        spawnPos.z = 0f;
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity; //  rotación siempre en 0

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        isPickedUp = false;
        sr.enabled = true;
        triggerCol.enabled = true;
        physicsCol.enabled = false;
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

    // Se llama desde PunchHitbox cuando golpea a alguien que tiene el DNA
    public void ThrowDNA(Vector2 direction)
    {
        isPickedUp = false;
        sr.enabled = true;

        triggerCol.enabled = false; // mientras vuela no se puede agarrar inmediatamente
        physicsCol.enabled = true;  // activa física para rebotar

        rb.bodyType = RigidbodyType2D.Dynamic;
        Vector2 force = new Vector2(direction.x * throwForceX, throwForceY);
        rb.AddForce(force, ForceMode2D.Impulse);

        // Después de un pequeño delay, habilitar el trigger para que se pueda agarrar
        Invoke(nameof(EnableTriggerAfterThrow), 0.5f);
    }

    private void EnableTriggerAfterThrow()
    {
        triggerCol.enabled = true;
        physicsCol.enabled = true; // una vez que puede ser agarrado, desactivar física
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
    }
    public void SetSpinEffect()
    {
        rb.angularVelocity = Random.Range(200f, 400f) * (Random.value > 0.5f ? 1f : -1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPickedUp) return;

        PlayerControllerDNA controller = collision.GetComponent<PlayerControllerDNA>();
        if (controller == null || controller.HasDNA()) return;

        controller.PickDNA(this);
        isPickedUp = true;

        sr.enabled = false;
        triggerCol.enabled = false;
        physicsCol.enabled = false; // asegurarse que no quede activo
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("Jugador agarró DNA");
    }
}