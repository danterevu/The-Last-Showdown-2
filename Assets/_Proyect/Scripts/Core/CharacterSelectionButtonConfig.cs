using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CharacterSelectionButtonConfig : MonoBehaviour
{
    [Header("Start Button")]
    public Button startButton;
    public Image startButtonImage;
    public Color disabledColor = Color.gray;
    public Color enabledColor = Color.yellow;
    public float pulseScale = 1.1f;
    public float pulseDuration = 0.5f;
    public Ease pulseEase = Ease.InOutSine;

    [Header("Other Buttons")]
    public Button backToMenuButton;
    public Button openControlsButton;
    public Button closeControlsButton;

    [Header("References")]
    public InputAssigner inputAssigner;

    private Vector3 _startButtonOriginalScale;
    private Color _startButtonOriginalColor;
    private Sequence _startButtonPulseSequence;
    private bool _bothPlayersReady;

    private void Awake()
    {
        if (startButton != null)
        {
            _startButtonOriginalScale = startButton.transform.localScale;
            if (startButtonImage != null)
            {
                _startButtonOriginalColor = startButtonImage.color;
            }
        }
    }

    private void Start()
    {
        InitializeButtons();
        UpdateButtonStates();
    }

    private void Update()
    {
        CheckPlayersReady();
    }

    private void InitializeButtons()
    {
        // Start Button
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
        }

        // Back To Menu Button
        if (backToMenuButton != null && inputAssigner != null)
        {
            backToMenuButton.onClick.AddListener(inputAssigner.BackToMenu);
        }

   

      
    }

    private void CheckPlayersReady()
    {
        if (inputAssigner == null) return;

        bool bothReady = InputAssigner.GetPlayerData(0)?.inputType != InputAssigner.InputType.None &&
                         InputAssigner.GetPlayerData(1)?.inputType != InputAssigner.InputType.None;

        if (bothReady != _bothPlayersReady)
        {
            _bothPlayersReady = bothReady;
            UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        UpdateStartButton();
    }

    private void UpdateStartButton()
    {
        if (startButton == null) return;

        startButton.gameObject.SetActive(true);
        startButton.interactable = _bothPlayersReady;

        if (startButtonImage != null)
        {
            startButtonImage.DOColor(_bothPlayersReady ? enabledColor : disabledColor, 0.2f).SetEase(Ease.OutQuad);
        }

        if (_bothPlayersReady)
        {
            StartPulseAnimation();
        }
        else
        {
            StopPulseAnimation();
        }
    }

    private void StartPulseAnimation()
    {
        if (_startButtonPulseSequence != null && _startButtonPulseSequence.IsActive()) return;

        _startButtonPulseSequence = DOTween.Sequence();
        _startButtonPulseSequence.Append(startButton.transform.DOScale(_startButtonOriginalScale * pulseScale, pulseDuration).SetEase(pulseEase));
        _startButtonPulseSequence.Append(startButton.transform.DOScale(_startButtonOriginalScale, pulseDuration).SetEase(pulseEase));
        _startButtonPulseSequence.SetLoops(-1, LoopType.Restart);
        _startButtonPulseSequence.Play();
    }

    private void StopPulseAnimation()
    {
        if (_startButtonPulseSequence != null && _startButtonPulseSequence.IsActive())
        {
            _startButtonPulseSequence.Kill();
            startButton.transform.DOScale(_startButtonOriginalScale, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    private void OnStartButtonClick()
    {
        if (inputAssigner != null)
        {
            inputAssigner.LoadNextScene();
        }
    }

    private void OnDestroy()
    {
        _startButtonPulseSequence?.Kill();
    }
}
