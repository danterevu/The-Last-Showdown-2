using UnityEngine;

public class HardPoint : MonoBehaviour
{
    // el manager le pregunta esto cada frame
    public bool IsPlayer1Inside { get; private set; }
    public bool IsPlayer2Inside { get; private set; }

    [Header("Visual")]
    [SerializeField] private SpriteRenderer zoneSprite;

    [Header("Particles")]
    [SerializeField] private ParticleSystem pointParticles;

    [Header("Colores")]
    [SerializeField] private Color neutralColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color player1Color = new Color(0.7f, 0f, 1f, 0.6f); // violeta
    [SerializeField] private Color player2Color = new Color(1f, 0.5f, 0f, 0.6f); // naranja
    [SerializeField] private Color disputedColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Particle Speed")]
    [SerializeField] private float neutralSpeed = 1f;
    [SerializeField] private float captureSpeed = 3f;
    [SerializeField] private float disputedSpeed = 0.4f;

    private ParticleSystem.MainModule particleMain;
    private KingOfHill _kingOfHill;

    private void Awake()
    {
        if (pointParticles != null)
            particleMain = pointParticles.main;
        _kingOfHill = FindFirstObjectByType<KingOfHill>();
    }

    private void Update()
    {
        
        if (IsPlayer1Inside && IsPlayer2Inside)
        {
            zoneSprite.color = disputedColor;

            particleMain.startColor = disputedColor;
            particleMain.simulationSpeed = disputedSpeed;
        }

  
        else if (IsPlayer1Inside)
        {
            zoneSprite.color = player1Color;

            particleMain.startColor = player1Color;
            particleMain.simulationSpeed = captureSpeed;
        }

    
        else if (IsPlayer2Inside)
        {
            zoneSprite.color = player2Color;

            particleMain.startColor = player2Color;
            particleMain.simulationSpeed = captureSpeed;
        }

    
        else
        {
            zoneSprite.color = neutralColor;

            particleMain.startColor = neutralColor;
            particleMain.simulationSpeed = neutralSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
        {
            IsPlayer1Inside = true;
            if (_kingOfHill != null) _kingOfHill.OnPlayerEnteredZone(1);
        }
        else if (other.CompareTag("Player2"))
        {
            IsPlayer2Inside = true;
            if (_kingOfHill != null) _kingOfHill.OnPlayerEnteredZone(2);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
            IsPlayer1Inside = false;

        else if (other.CompareTag("Player2"))
            IsPlayer2Inside = false;
    }
}