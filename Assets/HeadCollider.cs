using UnityEngine;

public interface IPlayerController
{
    void SetCrushed(bool crushed);
}

public class HeadCollider : MonoBehaviour
{
    private IPlayerController owner;

    private void Awake()
    {
        owner = GetComponentInParent<IPlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root == transform.root) return; // ignorar el propio jugador

        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
            owner.SetCrushed(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root == transform.root) return;

        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
            owner.SetCrushed(false);
    }
}