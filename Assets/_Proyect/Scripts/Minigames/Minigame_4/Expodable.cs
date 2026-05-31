using UnityEngine;
using System.Collections;


public class Explodable : MonoBehaviour
{
    [Header("Piezas")]
    [SerializeField] private GameObject[] piecePrefabs; // prefabs de los fragmentos

    [Header("Fuerza de explosion")]
    [SerializeField] private float explosionForce = 3f;
    [SerializeField] private float torqueForce = 200f;

    [Header("Fade de piezas")]
    [SerializeField] private float piecesLifetime = 2f;

    [Header("Debug")]
    [SerializeField] private KeyCode debugKey = KeyCode.K; // K = matar nave en debug

    private SpaceShipController shipController;
    private bool isDead = false;

    private void Awake()
    {
        shipController = GetComponent<SpaceShipController>();
    }

    private void Update()
    {
        // tecla debug para forzar la muerte
        if (Input.GetKeyDown(debugKey) && !isDead)
        {
            Debug.Log("[Explodable] Debug: muerte forzada con tecla " + debugKey);
            Explode();
        }
    }

    public void Explode()
    {
        if (isDead) return; // evitar doble llamada
        isDead = true;

        // detener la nave completamente
        shipController?.ForceStop();
        shipController?.HideAllParticles();

        // detener el efecto de estela (after image)
        AfterImageEffect afterImage = GetComponentInChildren<AfterImageEffect>();
        afterImage?.StopEffect();

        // desactivar el controlador de armas para que no siga disparando
        WeaponController weaponController = GetComponentInChildren<WeaponController>();
        if(weaponController != null)
            weaponController.enabled = false;

        // instanciar cada pieza
        foreach (GameObject piecePrefab in piecePrefabs)
        {
            if (piecePrefab == null) continue;

            GameObject piece = Instantiate(piecePrefab, transform.position, transform.rotation);
            Rigidbody2D rb = piece.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // impulso random en todas direcciones
                for (int i = 0; i < piecePrefabs.Length; i++)
                {
                    // angulo base distribuido uniformemente en 360 grados
                    float baseAngle = (360f / piecePrefabs.Length) * i;
                    // peque�o random para que no sea perfecto
                    float randomAngle = baseAngle + Random.Range(-30f, 30f);
                    float rad = randomAngle * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                    rb.AddForce(dir * explosionForce, ForceMode2D.Impulse);
                }
                // rotacion random para que se vea natural
                rb.AddTorque(Random.Range(-torqueForce, torqueForce));
            }

            // fade out y destruccion de la pieza
            DebrisPiece debris = piece.GetComponent<DebrisPiece>();
            if (debris != null)
                debris.SetLifetime(piecesLifetime);
        }

        // ocultar la nave inmediatamente
        StartCoroutine(HideAndDestroy());
    }

    private IEnumerator HideAndDestroy()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        yield return new WaitForSeconds(piecesLifetime);

        // Reactivar en vez de destruir
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = true;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        // Reactivar el controlador de armas
        WeaponController weaponController = GetComponentInChildren<WeaponController>();
        if (weaponController != null)
        {
            weaponController.enabled = true;
            weaponController.ResetToDefault();
        }

        isDead = false;
    }
}