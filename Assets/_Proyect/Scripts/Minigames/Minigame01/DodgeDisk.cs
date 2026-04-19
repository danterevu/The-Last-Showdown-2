    using UnityEngine;
    using TMPro;
    using System.Collections;

    public class DodgeDisk : MonoBehaviour
    {
        [Header("Minigame Settings")]
        [SerializeField] private float gameDuration = 90f;
        [SerializeField] private float pointInterval = 5f;
        [SerializeField] private float invulnerableTime = 3f;

        [Header("References")]
        [SerializeField] private DiskMovement diskMovement;
        [SerializeField] private Collider2D diskCollider;
        [SerializeField] private Transform diskSpawnPoint;
        [SerializeField] private GameObject player1;
        [SerializeField] private GameObject player2;
        [SerializeField] private Collider2D player1Collider;
        [SerializeField] private Collider2D player2Collider;
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI player1ScoreText;
        [SerializeField] private TextMeshProUGUI player2ScoreText;

        [Header("Debug")]
        [SerializeField] private float gameTimer;
        [SerializeField] private float pointTimer;
        [SerializeField] private bool player1Invulnerable;
        [SerializeField] private bool player2Invulnerable;
        [SerializeField] private float invulnTimer1;
        [SerializeField] private float invulnTimer2;
        [SerializeField] private bool gameRunning;

    [Header("Death")]
    [SerializeField] private Animator player1Animator;
    [SerializeField] private Animator player2Animator;
    [SerializeField] private float deathAnimDuration = 0.4f;
    
    
        private bool player1UsedPowerUp = false;
        private bool player2UsedPowerUp = false;
        private float powerUpKillTimer1 = 0f;
        private float powerUpKillTimer2 = 0f;
        private const float POWER_UP_KILL_WINDOW = 5f; // segundos de ventana

        private void Start()
        {
            StartMinigame();
        }

        private void StartMinigame()
        {
            gameTimer = gameDuration;
            pointTimer = pointInterval;
            gameRunning = true;

            player1.transform.position = player1SpawnPoint.position;
            player2.transform.position = player2SpawnPoint.position;

            diskMovement.transform.position = diskSpawnPoint.position;
            diskMovement.Launch();
            UpdateUI();
        }

        private void Update()
        {
            if (!gameRunning) return;
            UpdateTimers();
            UpdateUI();
        }

        private void UpdateTimers()
        {
            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f)
            {
                gameTimer = 0f;
                EndMinigame();
                return;
            }

            pointTimer -= Time.deltaTime;
            if (pointTimer <= 0f)
            {
                GivePointsToBothPlayers();
                pointTimer = pointInterval;
            }

            if (player1Invulnerable)
            {
                invulnTimer1 -= Time.deltaTime;
                if (invulnTimer1 <= 0f)
                {
                    player1Invulnerable = false;
                    Physics2D.IgnoreCollision(diskCollider, player1Collider, false);
                }
            }

            if (player2Invulnerable)
            {
                invulnTimer2 -= Time.deltaTime;
                if (invulnTimer2 <= 0f)
                {
                    player2Invulnerable = false;
                    Physics2D.IgnoreCollision(diskCollider, player2Collider, false);
                }
            }
        }

    private void RespawnPlayer(int player)
    {
        if (player == 1)
        {
            player1Invulnerable = true;
            invulnTimer1 = invulnerableTime;
            Physics2D.IgnoreCollision(diskCollider, player1Collider, true);
            StartCoroutine(DeathSequence(
                player1, player1Animator,
                player1SpawnPoint, player1Collider));
        }
        else
        {
            player2Invulnerable = true;
            invulnTimer2 = invulnerableTime;
            Physics2D.IgnoreCollision(diskCollider, player2Collider, true);
            StartCoroutine(DeathSequence(
                player2, player2Animator,
                player2SpawnPoint, player2Collider));
        }
    }

    private IEnumerator DeathSequence(GameObject player, Animator anim,
        Transform spawnPoint, Collider2D col)
    {
        // congelar y deshabilitar colision
        PlayerController controller = player.GetComponent<PlayerController>();
        controller.SetFrozen(true);
        col.enabled = false;

        // animacion de muerte
        anim.SetTrigger("Die");
        yield return new WaitForSeconds(deathAnimDuration);

        // teletransportar
        player.transform.position = spawnPoint.position;


        // rehabilitar
        col.enabled = true;
        controller.SetFrozen(false);

        // flash de invulnerabilidad
        yield return StartCoroutine(FlashPlayer(player));
    }

    private void GivePointsToBothPlayers()
        {
            GameManager.Instance.AddResult(1, true);
            GameManager.Instance.AddResult(2, true);
        }  
        private void UpdateUI() //genera la ui del minijuego por codigo 
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(gameTimer / 60f);
                int seconds = Mathf.FloorToInt(gameTimer % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            if (player1ScoreText != null) player1ScoreText.text = "P1: " + GameManager.Instance.player1Score;
            if (player2ScoreText != null) player2ScoreText.text = "P2: " + GameManager.Instance.player2Score;
        }    

        private IEnumerator FlashPlayer(GameObject player)
        {
            SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
            float elapsed = 0f;
            bool visible = true;

            while (elapsed < invulnerableTime)
            {
                visible = !visible;
                sr.enabled = visible;
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }

            sr.enabled = true;
        }

        // llamar esto desde PowerUpManager cuando un jugador usa un power up
        public void NotifyPowerUpUsed(int player)
        {
            if (player == 1) { player1UsedPowerUp = true; powerUpKillTimer1 = POWER_UP_KILL_WINDOW; }
            else { player2UsedPowerUp = true; powerUpKillTimer2 = POWER_UP_KILL_WINDOW; }
        }

        // en Update, agregar esto al final de UpdateTimers:
        private void UpdatePowerUpKillTimers()
        {
            if (player1UsedPowerUp)
            {
                powerUpKillTimer1 -= Time.deltaTime;
                if (powerUpKillTimer1 <= 0f) player1UsedPowerUp = false;
            }
            if (player2UsedPowerUp)
            {
                powerUpKillTimer2 -= Time.deltaTime;
                if (powerUpKillTimer2 <= 0f) player2UsedPowerUp = false;
            }
        }

        // TryHitPlayer actualizado con los modificadores
        public void TryHitPlayer(int player)
        {
            if (player == 1 && !player1Invulnerable)
            {
                // modificador: morir da puntos al rival
                if (ModifierManager.Instance != null &&
                    ModifierManager.Instance.activeDDModifier == ModifierManager.DodgeDiskModifier.DeathGivesPoints)
                    GameManager.Instance.AddPoints(2, ModifierManager.Instance.deathGivesPoints);

                // modificador: power up kill bonus (el que golpeo era p2)
                if (ModifierManager.Instance != null &&
                    ModifierManager.Instance.activeDDModifier == ModifierManager.DodgeDiskModifier.PowerUpKillBonus
                    && player2UsedPowerUp)
                {
                    GameManager.Instance.AddPoints(2, ModifierManager.Instance.powerUpKillBonusPoints);
                    player2UsedPowerUp = false;
                }

                GameManager.Instance.RemovePoints(1, 10);
                RespawnPlayer(1);
            }
            else if (player == 2 && !player2Invulnerable)
            {
                if (ModifierManager.Instance != null &&
                    ModifierManager.Instance.activeDDModifier == ModifierManager.DodgeDiskModifier.DeathGivesPoints)
                    GameManager.Instance.AddPoints(1, ModifierManager.Instance.deathGivesPoints);

                if (ModifierManager.Instance != null &&
                    ModifierManager.Instance.activeDDModifier == ModifierManager.DodgeDiskModifier.PowerUpKillBonus
                    && player1UsedPowerUp)
                {
                    GameManager.Instance.AddPoints(1, ModifierManager.Instance.powerUpKillBonusPoints);
                    player1UsedPowerUp = false;
                }

                GameManager.Instance.RemovePoints(2, 10);
                RespawnPlayer(2);
            }
        }

        // EndMinigame actualizado
        public void EndMinigame()
        {
            gameRunning = false;
            diskMovement.Stop();

            // modificador winner bonus
            if (ModifierManager.Instance != null &&
                ModifierManager.Instance.activeDDModifier == ModifierManager.DodgeDiskModifier.WinnerBonus)
            {
                int winner = GameManager.Instance.player1RoundPoints >= GameManager.Instance.player2RoundPoints ? 1 : 2;
                GameManager.Instance.AddPoints(winner, ModifierManager.Instance.winnerBonusPoints);
            }

            var (p1Round, p2Round) = GameManager.Instance.FinishMinigame();
            GameManager.Instance.EndRound(1);
            PlayerPrefs.SetInt("LastRoundP1", p1Round);
            PlayerPrefs.SetInt("LastRoundP2", p2Round);
            SceneLoader.Instance.LoadResults();
        }
    }
