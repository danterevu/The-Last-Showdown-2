using System.Collections;
using UnityEngine;

public class MeteorStrike : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private GameObject meteorPrefab;

    [Header("Configuración")]
    [SerializeField] private float warningDuration = 2f;

    public void SpawnMeteor(Vector2 targetPosition)
    {
        StartCoroutine(MeteorRoutine(targetPosition));
    }

    private IEnumerator MeteorRoutine(Vector2 targetPosition)
    {
        // Spawn del warning
        GameObject warning =
            Instantiate(
                warningPrefab,
                targetPosition,
                Quaternion.identity
            );

        // Espera antes del impacto
        yield return new WaitForSeconds(warningDuration);

        // Destruir warning
        Destroy(warning);

        // Spawn meteorito
        Vector2 spawnPos =
     targetPosition +
     new Vector2(-10f, 10f);

        GameObject meteor =
            Instantiate(
                meteorPrefab,
                spawnPos,
                Quaternion.identity
            );

        meteor
            .GetComponent<MeteorMovement>()
            .Initialize(targetPosition);
    }
}