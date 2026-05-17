using UnityEngine;

public class RemoteControl : MonoBehaviour
{
    [SerializeField] private GameObject[] wall;
    private Collider2D wallCol;
    private SpriteRenderer wallSr;

    private bool wallActive = false;

    private void Start()
    {
        for(int i = 0;  i < wall.Length; i++)
        {
            wallCol = wall[i].GetComponent<Collider2D>();
            wallSr = wall[i].GetComponent<SpriteRenderer>();
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
