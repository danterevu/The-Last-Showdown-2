using UnityEngine;

public class HeadCollider : MonoBehaviour
{
    private PlatformPlayerController owner;

    private void Awake()
    {
        owner = GetComponentInParent<PlatformPlayerController>();
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