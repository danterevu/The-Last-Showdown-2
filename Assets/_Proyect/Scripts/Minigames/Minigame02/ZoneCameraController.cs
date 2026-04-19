using UnityEngine;

public class ZoneCameraController : MonoBehaviour
{
    [Header("Jugadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Zoom")]
    [SerializeField] private float minSize = 4f;
    [SerializeField] private float maxSize = 8f;
    [SerializeField] private float zoomSpeed = 3f;

    [Header("Seguimiento")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float padding = 1.5f;

    [Header("Limites por zona - arrastra las 4 paredes de cada zona")]
    [SerializeField] private Transform[] wallsUp;
    [SerializeField] private Transform[] wallsDown;
    [SerializeField] private Transform[] wallsLeft;
    [SerializeField] private Transform[] wallsRight;

    [Header("Debug")]
    [SerializeField] private float currentDistance;

    private Camera cam;
    private bool zoneSet = false;
    private int currentZoneIndex = 0;

    // limites calculados de la zona actual
    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetZoneCenter(Vector3 center, int zoneIndex)
    {
        currentZoneIndex = zoneIndex;
        zoneSet = true;

        // calcular limites desde las paredes de esta zona
        CalculateBounds(zoneIndex);

        // teleport al centro calculado
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;
        transform.position = new Vector3(centerX, centerY, transform.position.z);

        cam.orthographicSize = maxSize;
    }

    private void CalculateBounds(int index)
    {
        // los limites son las posiciones de las paredes
        maxY = wallsUp[index].position.y;
        minY = wallsDown[index].position.y;
        minX = wallsLeft[index].position.x;
        maxX = wallsRight[index].position.x;
    }

    private void LateUpdate()
    {
        if (!zoneSet || player1 == null || player2 == null) return;

        Vector3 midPoint = (player1.position + player2.position) / 2f;

        // zoom
        currentDistance = Vector3.Distance(player1.position, player2.position);
        float targetSize = Mathf.Clamp(currentDistance / 2f + padding, minSize, maxSize);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);

        // limites reales considerando el tamaño actual de la camara
        float camH = cam.orthographicSize;
        float camW = camH * cam.aspect;

        float clampedX = Mathf.Clamp(midPoint.x, minX + camW, maxX - camW);
        float clampedY = Mathf.Clamp(midPoint.y, minY + camH, maxY - camH);

        Vector3 targetPos = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}