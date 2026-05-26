using UnityEngine;
using System.Collections.Generic;

public class MinigameSpinner : MonoBehaviour
{
    [Header("Configuración de Minijuegos")]
    [SerializeField] private int totalMinigames = 4;

    [Header("Configuración Visual")]
    [SerializeField] private float minSpinPower = 40f;
    [SerializeField] private float maxSpinPower = 80f;
    [SerializeField] private float stopPower = 2f;

    [Header("Secciones ya jugadas (X)")]
    [SerializeField] private GameObject[] playedOverlays;

    [Header("Referencias")]
    [SerializeField] private ModifiersSpinner modifiersSpinner;

    // Offset centralizado — cambiá solo este valor si ajustás la ruleta
    private const float ANGLE_OFFSET = 270f;

    private Rigidbody2D rb;
    private bool hasSpun = false;
    private float stoppedTimer = 0f;

    private bool isCorreecting = false;
    private float targetAngle = 0f;
    [SerializeField] private float correctionSpeed = 90f;

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
        if (isCorreecting)
        {
            float currentAngle = transform.rotation.eulerAngles.z;
            float diff = Mathf.DeltaAngle(currentAngle, targetAngle);

            if (Mathf.Abs(diff) < 0.5f)
            {
                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
                isCorreecting = false;
                ConfirmSelection();
            }
            else
            {
                float step = correctionSpeed * Time.deltaTime;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);
                transform.rotation = Quaternion.Euler(0, 0, newAngle);
            }
            return;
        }

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

    private int GetWinnerIdFromAngle(float rawAngle)
    {
        float normalizedAngle = (rawAngle + ANGLE_OFFSET) % 360f;
        float degreesPerSlice = 360f / totalMinigames;
        int sectionIndex = Mathf.FloorToInt(normalizedAngle / degreesPerSlice);
        sectionIndex = Mathf.Clamp(sectionIndex, 0, totalMinigames - 1);
        return sectionIndex + 1;
    }

    private void SelectedMinigame()
    {
        List<int> available = GameManager.Instance.GetAvailableMinigames();
        if (available.Count == 0)
        {
            Debug.Log("No quedan minijuegos, yendo a pantalla final");
            SceneLoader.Instance.LoadFinalScreen();
            return;
        }

        float rawAngle = transform.rotation.eulerAngles.z;
        int winnerId = GetWinnerIdFromAngle(rawAngle);

        Debug.Log($"Angulo raw: {rawAngle:F1}° | Minijuego: {winnerId}");

        if (GameManager.Instance.IsMinigameAvailable(winnerId))
            ConfirmSelection();
        else
        {
            Debug.Log($"Minijuego {winnerId} ya jugado  corrigiendo...");
            MoveToNearestAvailable(rawAngle, available);
        }
    }

    private void MoveToNearestAvailable(float currentRawAngle, List<int> available)
    {
        float degreesPerSlice = 360f / totalMinigames;
        float bestAngle = 0f;
        float bestDistance = float.MaxValue;

        foreach (int id in available)
        {
            float sectionCenter = (id - 1) * degreesPerSlice + degreesPerSlice * 0.5f;
            float rawTarget = (sectionCenter - ANGLE_OFFSET + 360f) % 360f;
            float dist = Mathf.Abs(Mathf.DeltaAngle(currentRawAngle, rawTarget));

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestAngle = rawTarget;
            }
        }

        targetAngle = bestAngle;
        isCorreecting = true;
        rb.angularVelocity = 0f;
    }

    private void ConfirmSelection()
    {
        float rawAngle = transform.rotation.eulerAngles.z;
        int winnerId = GetWinnerIdFromAngle(rawAngle); // 0 - 90 = M1 Dodge Disk
                                                       // 90 - 180 = M2 King Of The Hill
                                                       // 180 - 270 = M3 Mutant DNA
                                                       // 270 - 360 = M4 Space Ships

        Debug.Log($" Confirmado: Minijuego {winnerId}");
        //GameManager.Instance.EndRound(winnerId); 
        //NO BORRAR HASTA build final
        RefreshOverlays();

        PlayerPrefs.SetInt("SelectedMinigame", winnerId);
        SceneLoader.Instance.LoadSelectModifier();
    }

    private void RefreshOverlays()
    {
        if (playedOverlays == null || playedOverlays.Length == 0) return;
        List<int> available = GameManager.Instance.GetAvailableMinigames();

        for (int i = 0; i < playedOverlays.Length; i++)
        {
            if (playedOverlays[i] == null) continue;
            playedOverlays[i].SetActive(!available.Contains(i + 1));
        }
    }
}