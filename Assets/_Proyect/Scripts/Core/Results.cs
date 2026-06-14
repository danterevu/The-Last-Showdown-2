using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Results : MonoBehaviour
{
    [System.Serializable]
    public enum AnimationState
    {
        Idle,
        Winner,
        Loser,
        PotentialLosing
    }

    [Header("Tema visual")]
    [SerializeField] private HUDTheme theme;
    [SerializeField] private GameObject[] themeColorObjects;

    [Header("Debug (Test desde escena Results)")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private int debugP1RoundScore = 10;
    [SerializeField] private int debugP2RoundScore = 5;
    [SerializeField] private int debugP1TotalScore = 20;
    [SerializeField] private int debugP2TotalScore = 15;
    [SerializeField] private HUDTheme debugTheme;

    [Header("Secuencia inicial")]
    [SerializeField] private UnityEngine.UI.Image blackScreen;
    [SerializeField] private TextMeshProUGUI typewriterText;
    [SerializeField] private string typewriterMessage = "Minijuego Terminado";
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float typewriterDelayAfterText = 1f;
    [SerializeField] private float typewriterFadeDuration = 0.5f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Monitor")]
    [SerializeField] private GameObject monitorObject;
    [SerializeField] private Vector3 monitorStartOffset = new Vector3(0, 10, 0);
    [SerializeField] private float monitorMoveDuration = 1f;
    [SerializeField] private Ease monitorEase = Ease.OutBack;
    private Vector3 monitorOriginalPos;

    [Header("Personajes")]
    [SerializeField] private Animator player1Animator;
    [SerializeField] private Animator player2Animator;
    [SerializeField] private string idleAnimParam = "State";

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI player1RoundText;
    [SerializeField] private TextMeshProUGUI player2RoundText;
    [SerializeField] private TextMeshProUGUI player1TotalText;
    [SerializeField] private TextMeshProUGUI player2TotalText;
    [SerializeField] private float countDuration = 0.4f;
    [SerializeField] private float punchScale = 1.4f;
    [SerializeField] private float punchDuration = 0.2f;

    [Header("Panel de resultados")]
    [SerializeField] private CanvasGroup resultsPanel;

    [SerializeField] private float resultsFadeDuration = 0.5f;
    [SerializeField] private float bigDifferenceThreshold = 50f;
    
    private int p1PreviousScore;
    private int p2PreviousScore;
    private int p1RoundScore;
    private int p2RoundScore;
    private int p1TargetScore;
    private int p2TargetScore;

    private void Start()
    {
        if (resultsPanel != null)
        {
            resultsPanel.alpha = 0f;
            resultsPanel.interactable = false;
            resultsPanel.blocksRaycasts = false;
        }
        // Inicializar el texto de la máquina de escribir a invisible
        if (typewriterText != null)
        {
            typewriterText.text = "";
            Color c = typewriterText.color;
            c.a = 0f;
            typewriterText.color = c;
        }
        
        // Si es modo debug, usar valores de debug
        if (debugMode)
        {
            Debug.Log("[Results] Iniciando en Modo Debug");
            theme = debugTheme;
            p1RoundScore = debugP1RoundScore;
            p2RoundScore = debugP2RoundScore;
            p1PreviousScore = debugP1TotalScore - p1RoundScore;
            p2PreviousScore = debugP2TotalScore - p2RoundScore;
            p1TargetScore = debugP1TotalScore;
            p2TargetScore = debugP2TotalScore;
        }
        else
        {
            // Solo usar GameManager si existe
            if (GameManager.Instance != null)
            {
                // Cargar el HUDTheme del minijuego seleccionado
                LoadThemeFromMinigame();
                
                // Guardar puntaje previo
                p1RoundScore = PlayerPrefs.GetInt("LastRoundP1", 0);
                p2RoundScore = PlayerPrefs.GetInt("LastRoundP2", 0);
                
                p1PreviousScore = GameManager.Instance.player1Score - p1RoundScore;
                p2PreviousScore = GameManager.Instance.player2Score - p2RoundScore;
                
                p1TargetScore = GameManager.Instance.player1Score;
                p2TargetScore = GameManager.Instance.player2Score;
            }
            else
            {
                Debug.LogWarning("[Results] GameManager no encontrado, usando valores por defecto!");
            }
        }
        
        ApplyTheme();

        player1RoundText.text = "Puntos obtenidos: " + p1RoundScore;
        player2RoundText.text = "Puntos obtenidos: " + p2RoundScore;
        player1TotalText.text = "Puntos totales: " + p1PreviousScore;
        player2TotalText.text = "Puntos totales: " + p2PreviousScore;

        // Guardar posición inicial del monitor
        if (monitorObject != null)
        {
            monitorOriginalPos = monitorObject.transform.position;
            monitorObject.transform.position = monitorOriginalPos + monitorStartOffset;
        }

        StartCoroutine(PlaySequence());
    }

    private void LoadThemeFromMinigame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Results] GameManager no está presente, no se puede cargar el tema!");
            return;
        }
        
        int selectedIdInt;
        
        // Obtener último minijuego jugado desde playedMinigames
        var played = GameManager.Instance.GetPlayedMinigames();
        if (played != null && played.Count > 0)
        {
            selectedIdInt = played[played.Count - 1];
        }
        else
        {
            // Si no hay playedMinigames, intentar con PlayerPrefs
            selectedIdInt = PlayerPrefs.GetInt("LastPlayedMinigame", 1); // Default a 1 si no hay nada
        }
        
        Debug.Log($"[Results] Buscando tema para Minigame ID: {selectedIdInt}");

        // Cargar el HUDTheme directamente por nombre (Minigame1, Minigame2, etc.)
        string themeName = "Minigame" + selectedIdInt;
        theme = Resources.Load<HUDTheme>(themeName);

        if (theme != null)
        {
            Debug.Log($"[Results] Cargado tema para minijuego {selectedIdInt}: {theme.name}");
        }
        else
        {
            Debug.LogWarning($"[Results] No se encontró HUDTheme llamado '{themeName}' en Resources!");
        }
    }

    private void ApplyTheme()
    {
        if (theme == null) return;

        foreach (var obj in themeColorObjects)
        {
            if (obj == null) continue;

            // Intentar con Image (UI)
            UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = theme.hudColor;
                continue;
            }

            // Intentar con SpriteRenderer (2D)
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = theme.hudColor;
            }
        }

        if (player1RoundText != null) player1RoundText.color = theme.player1TextColor;
        if (player2RoundText != null) player2RoundText.color = theme.player2TextColor;
        if (player1TotalText != null) player1TotalText.color = theme.player1TextColor;
        if (player2TotalText != null) player2TotalText.color = theme.player2TextColor;
    }

    private IEnumerator PlaySequence()
    {
        // Pantalla negra inicial
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            Color c = blackScreen.color;
            c.a = 1f;
            blackScreen.color = c;
        }
        
        // Fade out de la pantalla negra para revelar la escena
        if (blackScreen != null)
        {
            blackScreen.DOFade(0f, fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
        }

        // Mover el monitor a su posición original
        if (monitorObject != null)
        {
            monitorObject.transform.DOMove(monitorOriginalPos, monitorMoveDuration).SetEase(monitorEase);
            yield return new WaitForSeconds(monitorMoveDuration);
            if (resultsPanel != null)
            {
                resultsPanel.DOFade(1f, resultsFadeDuration)
                    .OnComplete(() =>
                    {
                        resultsPanel.interactable = true;
                        resultsPanel.blocksRaycasts = true;
                    });
            }
        }

        // Texto de máquina de escribir con fade-in
        if (typewriterText != null)
        {
            typewriterText.text = "";
            
            // Fade in del texto
            Color c = typewriterText.color;
            c.a = 0f;
            typewriterText.color = c;
            typewriterText.DOFade(1f, typewriterFadeDuration);
            yield return new WaitForSeconds(typewriterFadeDuration);
            
            // Escribir el texto caracter por caracter
            foreach (char letter in typewriterMessage)
            {
                typewriterText.text += letter;
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(typewriterDelayAfterText);

        // Contar los puntos
        StartCoroutine(CountScoreCoroutine());
    }

    private IEnumerator CountScoreCoroutine()
    {
        float timer = 0f;
        while (timer < countDuration)
        {
            timer += Time.deltaTime;
            float t = timer / countDuration;

            player1RoundText.text = "Puntos obtenidos: " + Mathf.RoundToInt(Mathf.Lerp(p1RoundScore, 0, t));
            player2RoundText.text = "Puntos obtenidos: " + Mathf.RoundToInt(Mathf.Lerp(p2RoundScore, 0, t));

            player1TotalText.text = "Puntos totales: " + Mathf.RoundToInt(Mathf.Lerp(p1PreviousScore, p1TargetScore, t));
            player2TotalText.text = "Puntos totales: " + Mathf.RoundToInt(Mathf.Lerp(p2PreviousScore, p2TargetScore, t));
            yield return null;
        }

        player1RoundText.text = "Puntos obtenidos: 0";
        player2RoundText.text = "Puntos obtenidos: 0";

        player1TotalText.text = "Puntos totales: " + p1TargetScore;
        player2TotalText.text = "Puntos totales: " + p2TargetScore;

        // Punch de los textos
        if (player1TotalText != null)
        {
            player1TotalText.transform.DOScale(Vector3.one * punchScale, punchDuration).SetEase(Ease.OutBack)
                .OnComplete(() => player1TotalText.transform.DOScale(Vector3.one, punchDuration));
        }
        if (player2TotalText != null)
        {
            player2TotalText.transform.DOScale(Vector3.one * punchScale, punchDuration).SetEase(Ease.OutBack)
                .OnComplete(() => player2TotalText.transform.DOScale(Vector3.one, punchDuration));
        }

        // Aplicar animaciones a los personajes
        SetCharacterAnimations(p1TargetScore, p2TargetScore);
    }

    private void SetCharacterAnimations(int p1Score, int p2Score)
    {
        int diff = Mathf.Abs(p1Score - p2Score);

        AnimationState p1State = AnimationState.Idle;
        AnimationState p2State = AnimationState.Idle;

        if (p1Score > p2Score)
        {
            p1State = AnimationState.Winner;
            p2State = (diff >= bigDifferenceThreshold) ? AnimationState.PotentialLosing : AnimationState.Loser;
        }
        else if (p2Score > p1Score)
        {
            p2State = AnimationState.Winner;
            p1State = (diff >= bigDifferenceThreshold) ? AnimationState.PotentialLosing : AnimationState.Loser;
        }

        if (player1Animator != null)
            player1Animator.SetInteger(idleAnimParam, (int)p1State);
        if (player2Animator != null)
            player2Animator.SetInteger(idleAnimParam, (int)p2State);
    }

    public void OnContinuarButton()
    {
        if (debugMode)
        {
            Debug.Log("[Results] Modo Debug: OnContinuarButton presionado (no se cargan managers/escenas)");
            return;
        }

        Debug.Log($"currentRound: {GameManager.Instance.currentRound} | TOTAL_ROUNDS: {GameManager.TOTAL_ROUNDS} | IsGameOver: {GameManager.Instance.IsGameOver()}");
        if (GameManager.Instance.IsGameOver())
            SceneLoader.Instance.LoadFinalScreen();
        else
            SceneLoader.Instance.LoadRuleta();
    }

    public void OnSalirButton()
    {
        Application.Quit();
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void LoadRoulette()
    {
        SceneManager.LoadScene("Select_Minigame");
    }

    private void OnDisable()
    {
        // Detener todos los Tweens
        DOTween.Kill(this);
        if (resultsPanel != null) resultsPanel.DOKill();
        if (monitorObject != null) monitorObject.transform.DOKill();
        if (blackScreen != null) blackScreen.DOKill();
        if (player1TotalText != null) player1TotalText.transform.DOKill();
        if (player2TotalText != null) player2TotalText.transform.DOKill();
    }
}
