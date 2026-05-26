using UnityEngine;

public class RemoteControl : MonoBehaviour
{
    [SerializeField] private GameObject[] walls;

    private Collider2D[] wallCols;
    private SpriteRenderer[] wallSrs;

    private void Start()
    {
        wallCols = new Collider2D[walls.Length];
        wallSrs = new SpriteRenderer[walls.Length];

        for (int i = 0; i < walls.Length; i++)
        {
            wallCols[i] = walls[i].GetComponent<Collider2D>();
            wallSrs[i] = walls[i].GetComponent<SpriteRenderer>();
        }

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

    public void ActivateWall()
    {
        for (int i = 0; i < walls.Length; i++)
        {
            if (wallCols[i] != null) wallCols[i].enabled = true;
            if (wallSrs[i] != null) wallSrs[i].enabled = true;
        }
    }

    public void DeactivateWall()
    {
        if (wallCols == null) return; // protección si se llama antes de Start
        for (int i = 0; i < walls.Length; i++)
        {
            if (wallCols[i] != null) wallCols[i].enabled = false;
            if (wallSrs[i] != null) wallSrs[i].enabled = false;
        }
    }
}
