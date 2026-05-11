using UnityEngine;

public class Buttons : MonoBehaviour
{
    [SerializeField] private GameObject wall;
    [SerializeField] private Collider2D wallCol;
    [SerializeField] private SpriteRenderer wallSr;

    private bool wallActive = false;

    private void Start()
    {
        wallCol = wall.GetComponent<Collider2D>();
        wallSr = wall.GetComponent<SpriteRenderer>();
        DeactivateWall();
    }

    private void OnEnable()
    {
        Deposit.OnAnyDeposit += DeactivateWall;
    }

    private void OnDisable()
    {
        Deposit.OnAnyDeposit -= DeactivateWall;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player1") && !collision.CompareTag("Player2")) return;
        if (wallActive) return; // si ya está activa no hacer nada

        ActivateWall();
    }

    private void ActivateWall()
    {
        wallActive = true;
        wallCol.enabled = true;
        wallSr.enabled = true;
    }

    private void DeactivateWall()
    {
        wallActive = false;
        wallCol.enabled = false;
        wallSr.enabled = false;
    }
}
