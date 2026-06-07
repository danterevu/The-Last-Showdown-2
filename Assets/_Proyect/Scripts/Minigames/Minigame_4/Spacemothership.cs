using UnityEngine;
using System.Collections;

/// SpaceMotherShip
///
/// SETUP del prefab:
///   - SpriteRenderer con el sprite de la nave nodriza
///   - Sorting Order alto (ej: 100) para que se vea encima de todo
///   - Este script
///   - Sin Rigidbody ni Collider (es puramente visual/narrativo)

public class SpaceMotherShip : MonoBehaviour
{
    [Header("Movimiento hacia el landing point")]
    [Tooltip("Velocidad de vuelo hacia el punto de aterrizaje")]
    [SerializeField] private float flySpeed = 5f;
    [Tooltip("Amplitud del movimiento sinusoidal lateral (cartoon)")]
    [SerializeField] private float sineAmplitude = 0.4f;
    [Tooltip("Frecuencia del movimiento sinusoidal")]
    [SerializeField] private float sineFrequency = 2f;

    [Header("Escala")]
    [Tooltip("Escala inicial de la nave (lejos, grande por perspectiva)")]
    [SerializeField] private float startScale = 2.5f;
    [Tooltip("Escala minima al llegar al landing point")]
    [SerializeField] private float landedScale = 0.8f;
    [Tooltip("Segundos que espera en el landing point antes de irse")]
    [SerializeField] private float waitAtLanding = 0f;

    [Header("Salida")]
    [Tooltip("Velocidad de salida al irse (puede ser mayor que la de entrada)")]
    [SerializeField] private float departSpeed = 8f;
    [Tooltip("Punto de salida. Si es null, sale por donde entro.")]
    [SerializeField] private Transform departurePoint;

    [Header("Rotacion del sprite")]
    [Tooltip("Offset de rotacion. 0 = apunta a la derecha, -90 = apunta arriba")]
    [SerializeField] private float spriteRotationOffset = -90f;

    public bool HasLanded { get; private set; } = false;

    private Vector3 targetPosition;
    private Vector3 originPosition;
    private Coroutine flyCoroutine;

    // -------------------------------------------------------------------------

    public void FlyTo(Vector3 destination)
    {
        originPosition = transform.position;
        targetPosition = destination;
        HasLanded = false;

        transform.localScale = Vector3.one * startScale;

        if (flyCoroutine != null)
            StopCoroutine(flyCoroutine);

        flyCoroutine = StartCoroutine(FlyToTarget());
    }

    public void Depart()
    {
        if (flyCoroutine != null)
            StopCoroutine(flyCoroutine);

        flyCoroutine = StartCoroutine(DepartCoroutine());
    }

    // -------------------------------------------------------------------------

    private IEnumerator FlyToTarget()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        // Distancia total para calcular progreso
        float totalDist = Vector3.Distance(startPos, targetPosition);

        while (true)
        {
            float dist = Vector3.Distance(transform.position, targetPosition);

            if (dist < 0.1f)
                break;

            elapsed += Time.deltaTime;

            // Direccion principal
            Vector3 dir = (targetPosition - transform.position).normalized;

            // Desplazamiento sinusoidal perpendicular (efecto cartoon)
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
            float sineOffset = Mathf.Sin(elapsed * sineFrequency) * sineAmplitude;
            Vector3 move = (dir + perp * sineOffset) * flySpeed * Time.deltaTime;

            transform.position += move;

            // Rotacion para que la nave apunte hacia donde va
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteRotationOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Escala: reduce progresivamente con la distancia
            float progress = 1f - Mathf.Clamp01(dist / totalDist);
            float currentScale = Mathf.Lerp(startScale, landedScale, progress);
            transform.localScale = Vector3.one * currentScale;

            yield return null;
        }

        // Llegamos
        transform.position = targetPosition;
        transform.localScale = Vector3.one * landedScale;

        if (waitAtLanding > 0f)
            yield return new WaitForSeconds(waitAtLanding);

        HasLanded = true;
        Debug.Log("[SpaceMotherShip] Ha aterrizado en el landing point.");
    }

    private IEnumerator DepartCoroutine()
    {
        Vector3 exitTarget;

        if (departurePoint != null)
            exitTarget = departurePoint.position;
        else
            exitTarget = originPosition;

        float startingScale = transform.localScale.x;

        while (true)
        {
            float dist = Vector3.Distance(transform.position, exitTarget);
            if (dist < 0.2f)
                break;

            Vector3 dir = (exitTarget - transform.position).normalized;
            transform.position += dir * departSpeed * Time.deltaTime;

            // Rotar hacia donde va
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteRotationOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Escala crece al alejarse (perspectiva inversa)
            float progress = 1f - Mathf.Clamp01(dist / Vector3.Distance(originPosition, exitTarget));
            transform.localScale = Vector3.one * Mathf.Lerp(startingScale, startScale, progress);

            yield return null;
        }

        Debug.Log("[SpaceMotherShip] Nave nodriza se fue. Destruyendose.");
        Destroy(gameObject);
    }
}