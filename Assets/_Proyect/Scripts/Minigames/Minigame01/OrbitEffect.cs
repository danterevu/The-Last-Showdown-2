using UnityEngine;

public class OrbitEffect : MonoBehaviour
{
    [Header("Configuracion de la sierra")]
    [SerializeField] private float orbitRadius = 0.55f;
    [SerializeField] private float orbitSpeed = 220f;
    [SerializeField] private float angleOffset = 0f;

    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float offsetY = 0f;

    [Header("Propia rotacion de la sierra")]
    [SerializeField] private float selfRotationSpeed = 360f;

    private float currentAngle;
    
    void Start()
    {
        currentAngle = angleOffset;    
    }


    void Update()
    {
        currentAngle += orbitSpeed * Time.deltaTime;
        float rad = currentAngle * Mathf.Deg2Rad;


        transform.Rotate(0f, 0f, selfRotationSpeed * Time.deltaTime, Space.Self);
    }
    
}
