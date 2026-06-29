using UnityEngine;
using DG.Tweening;

public class CharacterSelectionSceneAnimator : MonoBehaviour
{
    [Header("Selection Phase Elements")]
    public GameObject[] lights;
    public GameObject backToMenuButtonObj;
    public GameObject startGameButtonObj;

    [Header("Timing")]
    public float lightsAppearDelay = 0.2f;
    public float buttonAppearDelay = 0.2f;

    [Header("References")]
    public InputAssigner inputAssigner;

    private Sequence _selectionSequence;

    private void Start()
    {
        AudioManager.Instance?.ResumeMusic(SoundID.SelectionMusic);

        InitializeScene();
        StartSelectionPhase();
    }

    private void InitializeScene()
    {
        // --- LUCES (empezar APAGADAS) ---
        if (lights != null)
        {
            foreach (var light in lights)
            {
                light.SetActive(false);
            }
        }

        // --- BOTONES ---
        if (backToMenuButtonObj != null)
            backToMenuButtonObj.SetActive(false);

        if (startGameButtonObj != null)
            startGameButtonObj.SetActive(false);
    }

    private void StartSelectionPhase()
    {
        _selectionSequence = DOTween.Sequence();
        _selectionSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        // 1. Activar luces
        if (lights != null && lights.Length > 0)
        {
            _selectionSequence.AppendInterval(lightsAppearDelay);
            _selectionSequence.AppendCallback(() =>
            {
                foreach (var light in lights)
                {
                    if (light != null)
                    {
                        light.SetActive(true);
                    }
                }
            });
        }

        // 2. Show buttons and activate InputAssigner
        _selectionSequence.AppendInterval(buttonAppearDelay);
        _selectionSequence.AppendCallback(() =>
        {
            if (backToMenuButtonObj != null)
                backToMenuButtonObj.SetActive(true);

            if (startGameButtonObj != null)
                startGameButtonObj.SetActive(true);

            if (inputAssigner != null)
            {
                inputAssigner.enabled = true;
            }
        });

        _selectionSequence.Play();
    }

    private void OnDestroy()
    {
        _selectionSequence?.Kill();
    }
}