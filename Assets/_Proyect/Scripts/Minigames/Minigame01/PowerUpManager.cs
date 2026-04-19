using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    public enum PowerUpType { Swap, Freeze, Wall, Magnet } //los 4 estados/power-ups

    [Header("References")]
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private DiskMovement diskMovement; //referencias para poder manipularlos 
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private PowerUpMovement powerUpPickup;

    [Header("Settings")]
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private float wallDuration = 5f;
    [SerializeField] private float magnetDuration = 3f;
    private float magnetStrength = 3f;

    [Header("Debug")]
    [SerializeField] private PowerUpType player1PowerUp;
    [SerializeField] private PowerUpType player2PowerUp; //booleanos para saber que power-up tiene cada uno en ese momento
    [SerializeField] private bool player1HasPowerUp;
    [SerializeField] private bool player2HasPowerUp; //booleanos para saber si tienen un power-up agarrado o no
    [SerializeField] private bool player1Frozen;
    [SerializeField] private bool player2Frozen;
    [SerializeField] private bool wallActive;
    [SerializeField] private bool magnetActive;

    [Header("HUD")]
    [SerializeField] private PowerUpHUDMinigame1 player1HUD;
    [SerializeField] private PowerUpHUDMinigame1 player2HUD;

    private GameObject activeWall;
    private PlayerController player1Controller;
    private PlayerController player2Controller;
    private Rigidbody2D rb;
    [SerializeField] private GameObject Disk;

    private void Awake()
    {
        player1Controller = player1.GetComponent<PlayerController>();
        player2Controller = player2.GetComponent<PlayerController>(); //obtengo su script
        rb = Disk.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // player 1 activa power up
        if (player1HasPowerUp && player1Controller.GetInteractPressed()) //si tiene un power-up y aprieta la tecla asignada, se activa
            ActivatePowerUp(1); //1 seria el PJ 1

        // player 2 activa power up
        if (player2HasPowerUp && player2Controller.GetInteractPressed())
            ActivatePowerUp(2);
    }

    // PICKUP 
    public void OnPlayerPickup(int player) //se llama en el trigger del PowerUpMovement, y se le para 1 o 2
    {

        if (player == 1 && !player1HasPowerUp) //chequea que player es (1 o 2) y si tiene ya un power-up o no
        {
            player1PowerUp = GetRandomPowerUp(); //se otorga un power-up de forma random
            player1HasPowerUp = true; //TRUE tiene un power-up

            player1HUD?.ShowIcon(1, player1PowerUp);
            powerUpPickup.gameObject.SetActive(false); //se desactiva
            Invoke(nameof(RespawnPickup), 2f); //se llama a la funcion RespawnPickup, que lo activa de nuevo en un lugar random
        }
        else if (player == 2 && !player2HasPowerUp)
        {
            player2PowerUp = GetRandomPowerUp();
            player2HasPowerUp = true;
            player2HUD?.ShowIcon(2, player2PowerUp);
            powerUpPickup.gameObject.SetActive(false);
            Invoke(nameof(RespawnPickup), 2f);
        }
    }

    private void RespawnPickup()
    {
        powerUpPickup.Reposition();
    }

    private PowerUpType GetRandomPowerUp() //genera un numero random que es el power-up
    {
        int r = Random.Range(0, 4);
        return (PowerUpType)r;
    }

    // ACTIVATE
    private void ActivatePowerUp(int player)
    {
        DodgeDisk dodgeDisk = FindFirstObjectByType<DodgeDisk>();
        if (dodgeDisk != null)
            dodgeDisk.NotifyPowerUpUsed(player);

        PowerUpType type = player == 1 ? player1PowerUp : player2PowerUp;

        // consumir el power up
        if (player == 1)
        {
            player1HasPowerUp = false;
                player1HUD?.HideIcon(1);

        }  
        // ya no tiene el power-up
        else
        {
            player2HasPowerUp = false;
            player2HUD?.HideIcon(2);
        }
        int opponent = player == 1 ? 2 : 1; //determina quien es el oponente

        switch (type) //se evaluan los casos y se ejecutan
        {
            case PowerUpType.Swap:
                ActivateSwap(player, opponent);
                break;
            case PowerUpType.Freeze:
                if (!player1Frozen && !player2Frozen)
                    StartCoroutine(ActivateFreeze(opponent));
                break;
            case PowerUpType.Wall:
                if (!wallActive)
                    StartCoroutine(ActivateWall());
                break;
            case PowerUpType.Magnet:
                if (!magnetActive)
                    StartCoroutine(ActivateMagnet(opponent));
                break;
        }
    }

    // SWAP
    private void ActivateSwap(int player, int opponent)
    {
        GameObject p1 = player == 1 ? player1 : player2;
        GameObject p2 = player == 1 ? player2 : player1;

        Vector3 temp = p1.transform.position;
        p1.transform.position = p2.transform.position;
        p2.transform.position = temp;
    }

    // FREEZE 
    private IEnumerator ActivateFreeze(int opponent) //IEnumerator permite lógica en el tiempo
    {
        PlayerController opponentController = opponent == 1 ? player1Controller : player2Controller;

        if (opponent == 1) player1Frozen = true;
        else player2Frozen = true;

        opponentController.SetFrozen(true);

        yield return new WaitForSeconds(freezeDuration);

        opponentController.SetFrozen(false);

        if (opponent == 1) player1Frozen = false;
        else player2Frozen = false;
    }

    //  WALL 
    private IEnumerator ActivateWall()
    {
        wallActive = true;
        activeWall = Instantiate(wallPrefab, Vector3.zero, Quaternion.identity);

        yield return new WaitForSeconds(wallDuration);

        Destroy(activeWall);
        wallActive = false;
    }

    // MAGNET 
    private IEnumerator ActivateMagnet(int opponent)
    {
        magnetActive = true;
        GameObject target = opponent == 1 ? player1 : player2;

        float elapsed = 0f;
        while (elapsed < magnetDuration) //Loop de 3 segundos
        {
            Vector2 dirToTarget = (target.transform.position - diskMovement.transform.position).normalized;
            Vector2 currentDir = rb.linearVelocity.normalized;
            Vector2 newDir = Vector2.Lerp(currentDir, dirToTarget, magnetStrength * Time.deltaTime);
            diskMovement.SetDirection(newDir.normalized);
            elapsed += Time.deltaTime;
            yield return null; //Espera al siguiente frame
        }

        magnetActive = false;
    }
}