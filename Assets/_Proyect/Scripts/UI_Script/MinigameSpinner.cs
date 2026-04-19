using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class MinigameSpinner : MonoBehaviour
{
    [Header("Configuración de Minijuegos")]
    [SerializeField] private int totalMinigames = 2;

    [Header("Configuración Visual")]
    [SerializeField] private float minSpinPower = 40f;
    [SerializeField] private float maxSpinPower = 80f;
    [SerializeField] private float stopPower = 2f;

    [Header("Secciones ya jugadas (X)")]
    [SerializeField] private GameObject[] playedOverlays;

    [Header("Referencias")]
    [SerializeField] private ModifiersSpinner modifiersSpinner;

    private Rigidbody2D rb;
    private bool hasSpun = false;
    private float stoppedTimer = 0f;

    // Para la corrección suave
    private bool isCorreecting = false;
    private float targetAngle = 0f;
    [SerializeField] private float correctionSpeed = 90f; // grados por segundo

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.angularDamping = 0.2f;
        RefreshOverlays();
        SpinIt();
    }

    public void SpinIt()
    {
        float randomPower = Random.Range(minSpinPower, maxSpinPower);
        rb.AddTorque(randomPower, ForceMode2D.Impulse);
        hasSpun = true;
        stoppedTimer = 0f;
        isCorreecting = false;
    }

    private void Update()
    {
        // Si estamos corrigiendo hacia una sección válida
        if (isCorreecting)
        {
            float currentAngle = transform.rotation.eulerAngles.z;

            // Calculamos la diferencia más corta entre ángulo actual y destino
            float diff = Mathf.DeltaAngle(currentAngle, targetAngle);

            if (Mathf.Abs(diff) < 0.5f)
            {
                // Llegamos al destino
                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
                isCorreecting = false;
                ConfirmSelection();
            }
            else
            {
                // Rotamos suavemente hacia el target
                float step = correctionSpeed * Time.deltaTime;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);
                transform.rotation = Quaternion.Euler(0, 0, newAngle);
            }
            return;
        }

        // Frenado normal
        if (rb.angularVelocity > 0)
        {
            rb.angularVelocity -= stopPower * Time.deltaTime;
            if (rb.angularVelocity < 0) rb.angularVelocity = 0;
        }

        if (hasSpun && rb.angularVelocity <= 0)
        {
            stoppedTimer += Time.deltaTime;
            if (stoppedTimer >= 0.5f)
            {
                hasSpun = false;
                stoppedTimer = 0f;
                SelectedMinigame();
            }
        }
    }

    private void SelectedMinigame()
    {
        List<int> available = GameManager.Instance.GetAvailableMinigames();
        if (available.Count == 0) return;

        float rawAngle = transform.rotation.eulerAngles.z;
        float angleOffset = 90f;
        float normalizedAngle = (rawAngle + angleOffset) % 360f;
        float degreesPerSlice = 360f / totalMinigames;

        int sectionIndex = Mathf.FloorToInt(normalizedAngle / degreesPerSlice);
        sectionIndex = Mathf.Clamp(sectionIndex, 0, totalMinigames - 1);
        int winnerId = sectionIndex + 1;

        Debug.Log($"Ángulo: {normalizedAngle:F1}° | Sección: {sectionIndex} | Minijuego: {winnerId}");

        if (GameManager.Instance.IsMinigameAvailable(winnerId))
        {
            ConfirmSelection();
        }
        else
        {
            Debug.Log($"Minijuego {winnerId} ya jugado  corrigiendo hacia sección válida...");
            MoveToNearestAvailable(rawAngle, available);
        }
    }

    private void MoveToNearestAvailable(float currentRawAngle, List<int> available)
    {
        float degreesPerSlice = 360f / totalMinigames;
        float angleOffset = 90f;
        float bestAngle = 0f;
        float bestDistance = float.MaxValue;

        foreach (int id in available)
        {
            // Centro de la sección de este minijuego en ángulo normalizado
            float sectionCenter = (id - 1) * degreesPerSlice + degreesPerSlice * 0.5f;

            // Convertimos de vuelta a rawAngle (invertimos el offset)
            float rawTarget = (sectionCenter - angleOffset + 360f) % 360f;

            // Distancia más corta desde el ángulo actual
            float dist = Mathf.Abs(Mathf.DeltaAngle(currentRawAngle, rawTarget));

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestAngle = rawTarget;
            }
        }

        targetAngle = bestAngle;
        isCorreecting = true;
        rb.angularVelocity = 0f; // Paramos el Rigidbody para tomar control manual
    }

    private void ConfirmSelection()
    {
        List<int> available = GameManager.Instance.GetAvailableMinigames();

        float rawAngle = transform.rotation.eulerAngles.z;
        float normalizedAngle = (rawAngle + 90f) % 360f;
        float degreesPerSlice = 360f / totalMinigames;

        int sectionIndex = Mathf.FloorToInt(normalizedAngle / degreesPerSlice);
        sectionIndex = Mathf.Clamp(sectionIndex, 0, totalMinigames - 1);
        int winnerId = sectionIndex + 1;

        Debug.Log($"Confirmado: Minijuego {winnerId}");
        RefreshOverlays();

        // Guardar qué minijuego tocó para que ModifiersSpinner lo lea
        PlayerPrefs.SetInt("SelectedMinigame", winnerId);

        // Ir a la ruleta de modificadores
        SceneLoader.Instance.LoadSelectModifier();
    }

    private void RefreshOverlays()
    {
        if (playedOverlays == null || playedOverlays.Length == 0) return;
        List<int> available = GameManager.Instance.GetAvailableMinigames();

        for (int i = 0; i < playedOverlays.Length; i++)
        {
            if (playedOverlays[i] == null) continue;
            bool alreadyPlayed = !available.Contains(i + 1);
            playedOverlays[i].SetActive(alreadyPlayed);
        }
        
    }
}