using UnityEngine;
using UnityEngine.InputSystem;


public class PowerUpHolder : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private bool isPlayer1 = true;
    [SerializeField] private int playerIndex = 0; // 0 = P1, 1 = P2

    [Header("Referencia a la nave rival")]
    [SerializeField] private Transform rivalTransform;

    [Header("Fire point (para proyectiles)")]
    [SerializeField] private Transform firePoint;

    public SpacePowerUpType? heldPowerUp = null;
    private bool hasPowerUp = false;

    private InputAction interactAction;

    public System.Action<SpacePowerUpType?> OnPowerUpChanged;

    private void Awake()
    {
        SetupInput();
    }

    private void SetupInput()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError($"[PowerUpHolder] {gameObject.name}: inputActionAsset es NULL. Asignalo en el Inspector.");
            return;
        }

        string mapName = isPlayer1 ? "Player1_TopDown" : "Player2_TopDown";
        var map = inputActionAsset.FindActionMap(mapName);

        if (map == null)
        {
            Debug.LogError($"[PowerUpHolder] {gameObject.name}: No se encontro el mapa '{mapName}'. Verific� que el nombre sea exacto en el InputActionAsset.");
            return;
        }

        interactAction = map.FindAction("Interact");

        if (interactAction == null)
        {
            Debug.LogError($"[PowerUpHolder] {gameObject.name}: No se encontro la accion 'Interact' en el mapa '{mapName}'. Verific� que exista y se llame exactamente 'Interact'.");
            return;
        }

        map.Enable();
        Debug.Log($"[PowerUpHolder] {gameObject.name}: Input configurado correctamente. Mapa='{mapName}', Accion='Interact' encontrada.");
    }

    private void Update()
    {
        if (interactAction == null) return;

        bool keyboardPressed = interactAction.WasPressedThisFrame();
        bool gamepadPressed = false;

        Gamepad gp = InputAssigner.GetGamepadForPlayer(playerIndex);
        if (gp != null)
            gamepadPressed = gp.buttonEast.wasPressedThisFrame; // c�rculo en PlayStation

        if (keyboardPressed || gamepadPressed)
        {
            if (!hasPowerUp)
                Debug.Log($"[PowerUpHolder] {gameObject.name}: Se presiono Interact pero NO hay power up.");
            else
                ActivatePowerUp();
        }
    }

    public void ReceivePowerUp(SpacePowerUpType type)
    {
        heldPowerUp = type;
        hasPowerUp = true;
        OnPowerUpChanged?.Invoke(heldPowerUp);
        Debug.Log($"[PowerUpHolder] {gameObject.name}: Recibio power up  {type}. Presiona {(isPlayer1 ? "E" : "L")} para activarlo.");
    }

    public bool HasPowerUp => hasPowerUp;

    private void ActivatePowerUp()
    {
        if (!heldPowerUp.HasValue) return;

        SpacePowerUpType type = heldPowerUp.Value;
        Debug.Log($"[PowerUpHolder] {gameObject.name}: Activando power up  {type}");

        heldPowerUp = null;
        hasPowerUp = false;
        OnPowerUpChanged?.Invoke(null);

        switch (type)
        {
            case SpacePowerUpType.SlowGrande:
                Debug.Log($"[PowerUpHolder] Llamando LaunchSlowGrande. SpacePowerUpManager.Instance={SpacePowerUpManager.Instance}");
                ActivateSlowGrande();
                break;
            case SpacePowerUpType.RocketSabotage:
                Debug.Log($"[PowerUpHolder] Llamando ApplyRocketSabotage. rivalTransform={rivalTransform}");
                ActivateRocketSabotage();
                break;
            case SpacePowerUpType.MeteorStrike:
                Debug.Log($"[PowerUpHolder] Llamando LaunchMeteorStrike. rivalTransform={rivalTransform}");
                ActivateMeteorStrike();
                break;
            case SpacePowerUpType.HomingMissile:
                Debug.Log($"[PowerUpHolder] Llamando LaunchHomingMissile. rivalTransform={rivalTransform}");
                ActivateHomingMissile();
                break;
            case SpacePowerUpType.Repulsion:
                Debug.Log($"[PowerUpHolder] Llamando ActivateRepulsion.");
                ActivateRepulsion();
                break;
            default:
                Debug.LogWarning($"[PowerUpHolder] Tipo de power up no manejado: {type}");
                break;
        }
    }

    private void ActivateSlowGrande()
    {
        if (SpacePowerUpManager.Instance == null)
        {
            Debug.LogError("[PowerUpHolder] SpacePowerUpManager.Instance es NULL.");
            return;
        }

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 shootDir = transform.right; // la direcci�n en que mira la nave
        int player = isPlayer1 ? 1 : 2;

        Debug.Log($"[PowerUpHolder] SlowGrande: origin={origin}, direction={shootDir}, ownerPlayer={player}");
        SpacePowerUpManager.Instance.LaunchSlowGrande(origin, shootDir, player);
    }

    private void ActivateRocketSabotage()
    {
        if (rivalTransform == null)
        {
            Debug.LogError("[PowerUpHolder] rivalTransform es NULL. Asigna la nave rival en el Inspector.");
            return;
        }

        SpaceShipController rivalShip = rivalTransform.GetComponent<SpaceShipController>();
        if (rivalShip == null)
        {
            Debug.LogError($"[PowerUpHolder] La nave rival '{rivalTransform.name}' no tiene SpaceShipController.");
            return;
        }

        if (SpacePowerUpManager.Instance == null)
        {
            Debug.LogError("[PowerUpHolder] SpacePowerUpManager.Instance es NULL.");
            return;
        }

        SpacePowerUpManager.Instance.ApplyRocketSabotage(rivalShip);
    }

    private void ActivateMeteorStrike()
    {
        if (rivalTransform == null)
        {
            Debug.LogError("[PowerUpHolder] rivalTransform es NULL. Asigna la nave rival en el Inspector.");
            return;
        }

        if (SpacePowerUpManager.Instance == null)
        {
            Debug.LogError("[PowerUpHolder] SpacePowerUpManager.Instance es NULL.");
            return;
        }

        SpacePowerUpManager.Instance.LaunchMeteorStrike(rivalTransform.position, isPlayer1 ? 1 : 2);
    }

    private void ActivateHomingMissile()
    {
        if (rivalTransform == null)
        {
            Debug.LogError("[PowerUpHolder] rivalTransform es NULL. Asigna la nave rival en el Inspector.");
            return;
        }

        if (SpacePowerUpManager.Instance == null)
        {
            Debug.LogError("[PowerUpHolder] SpacePowerUpManager.Instance es NULL.");
            return;
        }

        Vector2 origin = firePoint != null ? firePoint.position : transform.position;
        SpacePowerUpManager.Instance.LaunchHomingMissile(origin, rivalTransform, isPlayer1 ? 1 : 2);
    }

    private void ActivateRepulsion()
    {
        if (SpacePowerUpManager.Instance == null)
        {
            Debug.LogError("[PowerUpHolder] SpacePowerUpManager.Instance es NULL.");
            return;
        }

        SpacePowerUpManager.Instance.ActivateRepulsion(transform, isPlayer1 ? 1 : 2);
    }
}