using UnityEngine;

/// <summary>
/// Poner este script en un GameObject vacío en la escena (uno solo).
/// Asignar los 5 prefabs de esfera en el Inspector, en el mismo orden que el enum:
///   [0] Hook
///   [1] HeavyGravity
///   [2] MirrorControl
///   [3] Jetpack
///   [4] Crusher
/// </summary>
public class DroppedPowerUpSpawner : MonoBehaviour
{
    [Tooltip("Un prefab por tipo, mismo orden que PowerUpPickup.PowerUpType")]
    [SerializeField] private GameObject[] prefabsByType;

    public GameObject[] Prefabs => prefabsByType;

    public void SpawnDropped(PowerUpPickup.PowerUpType type, Vector3 position, Vector2 launchDirection)
    {
        DroppedPowerUp.Spawn(prefabsByType, type, position, launchDirection);
    }
}