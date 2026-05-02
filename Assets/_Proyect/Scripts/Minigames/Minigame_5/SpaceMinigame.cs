using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Manager del minijuego de naves espaciales.
/// Reutiliza la lógica de cambio de zona y flash de KingOfHill,
/// adaptada para el contexto de naves con respawn por salir del área.
///
/// SETUP en Unity:
///   - 5 GameObjects de zona, cada uno con BoxCollider2D trigger + SpaceZoneBoundary
///   - 2 spawns por zona (arrays player1Spawns y player2Spawns, misma longitud que zones)
///   - Una cámara con ZoneCameraController (reutilizada de Minigame02)
///   - HUDManager en escena para el timer
public class SpaceMinigame : MonoBehaviour
{
    
    //  INSPECTOR
    

    [Header("Duración")]
    [SerializeField] private float gameDuration = 90f;
    [SerializeField] private float zoneChangeDuration = 25f;

    [Header("Zonas (5 zonas)")]
    [SerializeField] private SpaceZoneBoundary[] zones;          // los 5 boundary objects
    [SerializeField] private Transform[] zoneCenters;            // Transform de cada zona (para la cámara)

    [Header("Spawns (mismo orden que zones)")]
    [SerializeField] private Transform[] player1Spawns;
    [SerializeField] private Transform[] player2Spawns;

    [Header("Jugadores")]
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;

    [Header("Cámara")]
    [SerializeField] private ZoneCameraController zoneCamera;    // reutilizada de KOH

    [Header("UI")]
    [SerializeField] private Image flashImage;                   // panel negro full-screen para transición
    [SerializeField] private float flashDuration = 0.8f;
    [SerializeField] private HUDManager hudManager;

    [Header("Respawn")]
    [Tooltip("Segundos de invulnerabilidad + parpadeo al reaparecer")]
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private float invincibleTime = 2f;

    [Header("Debug")]
    [SerializeField] private int currentZoneIndex;
    [SerializeField] private float gameTimer;
    [SerializeField] private float zoneTimer;
    [SerializeField] private bool gameRunning;

   
    //  ESTADO INTERNO
   

    private SpaceShipController p1Controller;
    private SpaceShipController p2Controller;

  
    private bool p1Respawning;
    private bool p2Respawning;

   

    private void Start()
    {
       
        if (GameManager.Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
            Debug.LogWarning("[SpaceMinigame] GameManager creado en escena para testing.");
        }

        p1Controller = player1.GetComponent<SpaceShipController>();
        p2Controller = player2.GetComponent<SpaceShipController>();

        // Elegir zona inicial aleatoria antes de que los jugadores se inicialicen
        currentZoneIndex = Random.Range(0, zones.Length);

        // Teleport inmediato a los spawns de la zona elegida
        TeleportToSpawns(currentZoneIndex);

        
        foreach (var zone in zones)
            zone.OnShipExited += HandleShipExited;

        StartMinigame();
    }

    private void OnDestroy()
    {
        // Limpiar suscripciones para evitar null references
        foreach (var zone in zones)
            if (zone != null)
                zone.OnShipExited -= HandleShipExited;
    }

    private void Update()
    {
        if (!gameRunning) return;

        gameTimer -= Time.deltaTime;
        zoneTimer -= Time.deltaTime;

        // Actualizar el timer del HUD
        hudManager?.UpdateTimer(gameTimer);

        if (zoneTimer <= 0f)
        {
            StartCoroutine(ChangeZone());
            zoneTimer = zoneChangeDuration;
        }

        if (gameTimer <= 0f)
        {
            gameTimer = 0f;
            EndMinigame();
        }
    }

    //  INICIO
   

    private void StartMinigame()
    {
        gameTimer = gameDuration;
        zoneTimer = zoneChangeDuration;
        gameRunning = true;

        ActivateZone(currentZoneIndex);
    }

    
    //  SISTEMA DE ZONAS  (reutilizado de KingOfHill)
    

    
 
    private IEnumerator ChangeZone()
    {
        gameRunning = false;

        // Detener las naves durante la transición para que no sigan moviéndose
        p1Controller.ForceStop();
        p2Controller.ForceStop();

        yield return StartCoroutine(Flash());

        // Elegir zona distinta a la actual 
        int newZone;
        do { newZone = Random.Range(0, zones.Length); }
        while (newZone == currentZoneIndex && zones.Length > 1);

        currentZoneIndex = newZone;

        TeleportToSpawns(currentZoneIndex);
        ActivateZone(currentZoneIndex);

        // Resetear flags de respawn al cambiar zona
        p1Respawning = false;
        p2Respawning = false;

        gameRunning = true;
    }

    
    /// Apunta la cámara al centro de la zona activa.
    
    private void ActivateZone(int index)
    {
        zoneCamera.SetZoneCenter(zoneCenters[index].position, index);
    }

   
    /// Mueve ambos jugadores a sus spawns de la zona dada.
    
    private void TeleportToSpawns(int index)
    {
        player1.transform.position = player1Spawns[index].position;
        player2.transform.position = player2Spawns[index].position;
    }

  
    private IEnumerator Flash()
    {
        flashImage.gameObject.SetActive(true);
        float half = flashDuration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / half));
            yield return null;
        }

        flashImage.color = Color.black;
        yield return new WaitForSeconds(0.1f);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(0, 0, 0, Mathf.Clamp01(1f - t / half));
            yield return null;
        }

        flashImage.gameObject.SetActive(false);
    }

    
    //  RESPAWN
   

    
    
    /// Solo reacciona si la zona que disparó el evento es la zona activa.
   
    private void HandleShipExited(GameObject ship, int playerNumber)
    {
       

        if (!gameRunning) return;

        if (playerNumber == 1 && !p1Respawning)
            StartCoroutine(RespawnPlayer(player1, playerNumber, player1Spawns[currentZoneIndex]));
        else if (playerNumber == 2 && !p2Respawning)
            StartCoroutine(RespawnPlayer(player2, playerNumber, player2Spawns[currentZoneIndex]));
    }

    /// Maneja el respawn completo: desactiva la nave, espera, reaparece con invulnerabilidad.
    
    private IEnumerator RespawnPlayer(GameObject shipObj, int playerNumber, Transform spawn)
    {
        // Marcar como "respawneando" para evitar múltiples triggers simultáneos
        if (playerNumber == 1) p1Respawning = true;
        else p2Respawning = true;

        SpaceShipController controller = shipObj.GetComponent<SpaceShipController>();
        SpriteRenderer spriteRenderer = shipObj.GetComponentInChildren<SpriteRenderer>();

        // 1. Detener nave y ocultarla 
        controller.ForceStop();
        shipObj.SetActive(false);

        // Pequeña penalización de puntos por salir de la zona
        // (ajustar o eliminar según diseño del juego)
        GameManager.Instance.RemovePoints(playerNumber, 5);

        // 2. Esperar antes de reaparecer 
        yield return new WaitForSeconds(respawnDelay);

        //  3. Teletransportar al spawn y activar 
        shipObj.transform.position = spawn.position;
        controller.ForceStop(); // asegurar que no queda velocidad residual
        shipObj.SetActive(true);

        //  4. Parpadeo de invulnerabilidad
        // El parpadeo indica visualmente al rival que no puede hacer daño
        // (si tu juego tiene daño entre naves; si no, es solo feedback visual)
        if (spriteRenderer != null)
            yield return StartCoroutine(BlinkSprite(spriteRenderer, invincibleTime));

        //  5. Limpiar flag
        if (playerNumber == 1) p1Respawning = false;
        else p2Respawning = false;
    }

    
    /// Parpadea un sprite alternando alpha
    
    private IEnumerator BlinkSprite(SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        float blinkRate = 0.12f; // segundos entre cada toggle
        bool visible = true;

        while (elapsed < duration)
        {
            visible = !visible;
            sr.color = visible
                ? Color.white
                : new Color(1f, 1f, 1f, 0.25f);

            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }

        // Asegurar que queda visible al terminar
        sr.color = Color.white;
    }

   
    //  FIN DEL MINIJUEGO  (stub — se completa en la siguiente iteración)
    

    public void EndMinigame()
    {
        if (!gameRunning) return; // evitar doble llamada
        gameRunning = false;

        hudManager?.StopTimerPulse();

        // Detener ambas naves
        p1Controller?.ForceStop();
        p2Controller?.ForceStop();

        // Transferir puntos de ronda al score global
        var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
        GameManager.Instance.EndRound(3); // id del minijuego de naves 

        // Guardar para la pantalla de resultados
        PlayerPrefs.SetInt("LastRoundP1", p1Round);
        PlayerPrefs.SetInt("LastRoundP2", p2Round);

        SceneLoader.Instance.LoadResults();
    }

    
    

    
    public bool IsRunning => gameRunning;


    public int CurrentZoneIndex => currentZoneIndex;
}
