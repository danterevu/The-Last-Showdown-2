using UnityEngine;

public class ZoneCameraTeleport : MonoBehaviour
{
    // Si la cámara no es la principal, puedes asignarla manualmente
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void MoveToCenter(Vector3 center)
    {
        if (targetCamera == null) return;
        // Mantener la misma Z que tiene la cámara (para 2D, suele ser -10)
        Vector3 newPos = center;
        newPos.z = targetCamera.transform.position.z;
        targetCamera.transform.position = newPos;
    }
}