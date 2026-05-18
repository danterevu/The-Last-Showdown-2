using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    // InvertControls agregado desde Minigame02
    public enum PowerUpType { Swap, Freeze, Wall, Magnet, InvertControls }

    [Header("References")]
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private DiskMovement diskMovement;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private PowerUpMovement powerUpPickup;

    [Header("Settings")]
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private float wallDuration = 5f;
    [SerializeField] private float magnetDuration = 3f;
    [SerializeField] private float invertDuration = 4f;
    private float magnetStrength = 3f;

    [Header("Efectos Magnet")]
    [SerializeField] private GameObject magnetEffectPlayer1; // hijo del jugador 1
    [SerializeField] private GameObject magnetEffectPlayer2; // hijo del jugador 2

    [Header("Efectos InvertControls")]
    [SerializeField] private GameObject invertEffectPlayer1; // hijo del jugador 1
    [SerializeField] private GameObject invertEffectPlayer2; // hijo del jugador 2

    [Header("Debug")]
    [SerializeField] private PowerUpType player1PowerUp;
    [SerializeField] private PowerUpType player2PowerUp;
    [SerializeField] private bool player1HasPowerUp;
    [SerializeField] private bool player2HasPowerUp;
    [SerializeField] private bool player1Frozen;
    [SerializeField] private bool player2Frozen;
    [SerializeField] private bool wallActive;
    [SerializeField] private bool magnetActive;
    [SerializeField] private bool invertActive;

    [Header("HUD")]
    [SerializeField] private PowerUpHUDMinigame1 player1HUD;
    [SerializeField] private PowerUpHUDMinigame1 player2HUD;

    private GameObject activeWall;
    private PlayerController player1Controller;
    private PlayerController player2Controller;
    private Animator player1Animator;
    private Animator player2Animator;
    private Rigidbody2D rb;
    [SerializeField] private GameObject Disk;

    private void Awake()
    {
        player1Controller = player1.GetComponent<PlayerController>();
        player2Controller = player2.GetComponent<PlayerController>();
        player1Animator = player1.GetComponent<Animator>();
        player2Animator = player2.GetComponent<Animator>();
        rb = Disk.GetComponent<Rigidbody2D>();

        // asegurarse que los efectos arrancan desactivados
        magnetEffectPlayer1?.SetActive(false);
        magnetEffectPlayer2?.SetActive(false);
        invertEffectPlayer1?.SetActive(false);
        invertEffectPlayer2?.SetActive(false);
    }

    private void Update()
    {
        if (player1HasPowerUp && player1Controller.GetInteractPressed())
            ActivatePowerUp(1);

        if (player2HasPowerUp && player2Controller.GetInteractPressed())
            ActivatePowerUp(2);
    }

    // PICKUP 
    public void OnPlayerPickup(int player)
    {
        if (player == 1 && !player1HasPowerUp)
        {
            player1PowerUp = GetRandomPowerUp();
            player1HasPowerUp = true;
            player1HUD?.ShowIcon(1, player1PowerUp);
            powerUpPickup.gameObject.SetActive(false);
            Invoke(nameof(RespawnPickup), 2f);
            AudioManager.Instance?.PlaySFX(SoundID.PUPickup);
        }
        else if (player == 2 && !player2HasPowerUp)
        {
            player2PowerUp = GetRandomPowerUp();
            player2HasPowerUp = true;
            player2HUD?.ShowIcon(2, player2PowerUp);
            powerUpPickup.gameObject.SetActive(false);
            Invoke(nameof(RespawnPickup), 2f);
            AudioManager.Instance?.PlaySFX(SoundID.PUPickup);
        }
    }

    private void RespawnPickup()
    {
        powerUpPickup.Reposition();
    }

    private PowerUpType GetRandomPowerUp()
    {
        int r = Random.Range(0, System.Enum.GetValues(typeof(PowerUpType)).Length);
        return (PowerUpType)r;
    }

    // ACTIVATE
    private void ActivatePowerUp(int player)
    {
        DodgeDisk dodgeDisk = FindFirstObjectByType<DodgeDisk>();
        if (dodgeDisk != null)
            dodgeDisk.NotifyPowerUpUsed(player);

        PowerUpType type = player == 1 ? player1PowerUp : player2PowerUp;

        if (player == 1) { player1HasPowerUp = false; player1HUD?.HideIcon(1); }
        else { player2HasPowerUp = false; player2HUD?.HideIcon(2); }

        int opponent = player == 1 ? 2 : 1;

        Animator userAnim = player == 1 ? player1Animator : player2Animator;
        Animator opponentAnim = player == 1 ? player2Animator : player1Animator;

        switch (type)
        {
            case PowerUpType.Swap:
                userAnim?.SetTrigger("Swap");
                opponentAnim?.SetTrigger("Swap");
                ActivateSwap(player, opponent);
                AudioManager.Instance?.PlaySFX(SoundID.PUTp);
                break;

            case PowerUpType.Freeze:
                if (!player1Frozen && !player2Frozen)
                {
                    opponentAnim?.SetTrigger("Frozen");
                    StartCoroutine(ActivateFreeze(opponent));
                    AudioManager.Instance?.PlaySFX(SoundID.PUHielo);
                }
                break;

            case PowerUpType.Wall:
                if (!wallActive)
                {
                    userAnim?.SetTrigger("Wall");
                    StartCoroutine(ActivateWall());
                    AudioManager.Instance?.PlaySFX(SoundID.PUPared);
                }
                break;

            case PowerUpType.Magnet:
                if (!magnetActive)
                {
                    userAnim?.SetTrigger("Magnet");
                    StartCoroutine(ActivateMagnet(opponent));

                }
                break;

            case PowerUpType.InvertControls:
                if (!invertActive)
                {
                    opponentAnim?.SetTrigger("Inverted");
                    StartCoroutine(ActivateInvertControls(opponent));
                    AudioManager.Instance?.PlaySFX(SoundID.PUConfusion);
                }
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
    private IEnumerator ActivateFreeze(int opponent)
    {
        PlayerController opponentController = opponent == 1 ? player1Controller : player2Controller;
        Animator opponentAnim = opponent == 1 ? player1Animator : player2Animator;

        if (opponent == 1) player1Frozen = true;
        else player2Frozen = true;

        opponentController.SetFrozen(true);
        yield return new WaitForSeconds(freezeDuration);
        opponentController.SetFrozen(false);

        opponentAnim?.SetTrigger("Unfreeze");

        if (opponent == 1) player1Frozen = false;
        else player2Frozen = false;
    }

    // WALL 
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

        // activar efecto visual sobre el oponente (el que es atraído)
        GameObject opponentObj = opponent == 1 ? player1 : player2;
        GameObject magnetEffect = opponent == 1 ? magnetEffectPlayer1 : magnetEffectPlayer2;
        magnetEffect?.SetActive(true);

        float elapsed = 0f;
        while (elapsed < magnetDuration)
        {
            Vector2 dirToTarget = (opponentObj.transform.position - diskMovement.transform.position).normalized;
            Vector2 currentDir = rb.linearVelocity.normalized;
            Vector2 newDir = Vector2.Lerp(currentDir, dirToTarget, magnetStrength * Time.deltaTime);
            diskMovement.SetDirection(newDir.normalized);
            elapsed += Time.deltaTime;
            yield return null;
        }

        magnetEffect?.SetActive(false);
        magnetActive = false;
    }

    // INVERT CONTROLS
    private IEnumerator ActivateInvertControls(int opponent)
    {
        invertActive = true;
        PlayerController opponentController = opponent == 1 ? player1Controller : player2Controller;
        Animator opponentAnim = opponent == 1 ? player1Animator : player2Animator;

        // activar efecto visual sobre el oponente
        GameObject invertEffect = opponent == 1 ? invertEffectPlayer1 : invertEffectPlayer2;
        invertEffect?.SetActive(true);

        opponentController.SetInvertControls(true);
        yield return new WaitForSeconds(invertDuration);
        opponentController.SetInvertControls(false);

        invertEffect?.SetActive(false);
        opponentAnim?.SetTrigger("Uninverted");

        invertActive = false;
    }

    // solo se usa en modo debug — da el power up directamente sin pickup
    public void DebugGivePowerUp(int player, PowerUpType type)
    {
        if (!DebugManager.IsDebugMode) return;

        if (player == 1)
        {
            player1PowerUp = type;
            player1HasPowerUp = true;
            player1HUD?.ShowIcon(1, type);
        }
        else
        {
            player2PowerUp = type;
            player2HasPowerUp = true;
            player2HUD?.ShowIcon(2, type);
        }
    }
}