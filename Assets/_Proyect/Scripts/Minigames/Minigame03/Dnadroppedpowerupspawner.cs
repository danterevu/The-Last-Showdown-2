using UnityEngine;

/// <summary>
/// Poner en un GameObject vacío en la escena del minijuego DNA (uno solo).
/// Asignar los 5 prefabs de esfera en el Inspector en este orden:
///   [0] Shrink
///   [1] Mine
///   [2] Berserk
///   [3] SlimeShot
///   [4] Shield
/// RemoteControl NO tiene esfera (se ignora automáticamente).
/// </summary>
public class DNADroppedPowerUpSpawner : MonoBehaviour
{
    [Tooltip("5 prefabs, uno por tipo (sin RemoteControl):\n[0] Shrink\n[1] Mine\n[2] Berserk\n[3] SlimeShot\n[4] Shield")]
    [SerializeField] private GameObject[] prefabsByType;

    public GameObject[] Prefabs => prefabsByType;

    public void SpawnDropped(DNAPowerUpPickup.DNAPowerUpType type, Vector3 position, Vector2 launchDirection,
                             PlayerControllerDNA owner = null)
    {
        DNADroppedPowerUp.Spawn(prefabsByType, type, position, launchDirection, owner);
    }
}