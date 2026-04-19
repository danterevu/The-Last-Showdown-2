using System.Collections.Generic;
using UnityEngine;

public class DNA : MonoBehaviour
{
    private SpriteRenderer sr;
    private Collider2D col;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlatformPlayerController controller = collision.GetComponent<PlatformPlayerController>(); // agarra el script con el que
                                                                                                  // colisiona, no hace falta decir quien

        if (controller != null && !controller.HasDNA())
        {
            controller.PickDNA();
            controller.ApplyMoveDebuff(0.5f);
            Debug.Log("Jugador agarró DNA");

            // ocultar DNA
            sr.enabled = false;
            col.enabled = false;
        }
    }
}
