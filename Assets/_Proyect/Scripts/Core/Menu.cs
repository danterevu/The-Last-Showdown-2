using UnityEngine;
using DG.Tweening;
using TMPro;

public class Menu : MonoBehaviour
{
    private static bool hasPlayedIntro = false;
    
    [Header("Intro Settings")]
    [SerializeField] private LogoIntroSequence logoIntroSequence;
    [SerializeField] private bool playIntroOnStart = true;

    [Header("Botón de Auto-Tipo")]
    [SerializeField] private TextMeshProUGUI autoTypeButtonLabel;

    [Header("Menu Elements")]
    [SerializeField] private RectTransform menuOptionsContainer;
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private Transform audienceContainer;
    [SerializeField] private RectTransform centralObject;
    [SerializeField] private RectTransform[] lightImages;
    [SerializeField] private Transform cameramanContainer;

    [Header("Animation Settings")]
    [SerializeField] private float menuOptionsDelay = 0f;
    [SerializeField] private float menuOptionsMoveDuration = 0.5f;
    [SerializeField] private Vector3 menuOptionsOffscreenPosition;
    [SerializeField] private float titleDelay = 0f;
    [SerializeField] private float titlePopDuration = 0.4f;
    [SerializeField] private float audienceScaleAmount = 1.1f;
    [SerializeField] private float audienceScaleDuration = 2f;
    [SerializeField] private float centralStretchAmount = 30f;
    [SerializeField] private float centralStretchDuration = 2f;
    [SerializeField] private float lightsMoveAmount = 50f;
    [SerializeField] private float lightsMoveDuration = 2f;
    [SerializeField] private float cameramanMoveAmount = 10f;
    [SerializeField] private float cameramanMoveDuration = 2.5f;

    private Sequence menuAnimationSequence;
    private Vector3 titleOriginalScale;
    private Vector2 centralOriginalSize;
    private Vector3 menuOptionsOriginalPosition;

    void Awake()
    {
        if (titleTransform != null)
        {
            titleOriginalScale = titleTransform.localScale;
            titleTransform.localScale = Vector3.zero;
            titleTransform.gameObject.SetActive(false);
        }
        if (menuOptionsContainer != null)
        {
            menuOptionsOriginalPosition = menuOptionsContainer.localPosition;
            menuOptionsContainer.localPosition = menuOptionsOffscreenPosition;
            menuOptionsContainer.gameObject.SetActive(false);
        }
        if (centralObject != null)
        {
            centralOriginalSize = centralObject.sizeDelta;
        }
    }

    void Start()
    {
        UpdateAutoTypeButtonLabel();
        
        if (logoIntroSequence != null && playIntroOnStart && !hasPlayedIntro)
        {
            hasPlayedIntro = true;
            logoIntroSequence.PlayIntroSequence();
        }
        else
        {
            AudioManager.Instance?.PlayMusic(SoundID.MenuMusic);
            PlayMenuAnimations();
        }
    }

    public void ToggleAutoType()
    {
        SettingsManager.ToggleAutoType();
        UpdateAutoTypeButtonLabel();
    }

    private void UpdateAutoTypeButtonLabel()
    {
        if (autoTypeButtonLabel != null)
        {
            autoTypeButtonLabel.text = SettingsManager.AutoTypeEnabled ? "AUTO-TIPO: ON" : "AUTO-TIPO: OFF";
        }
    }
    
    public void MenuButton()
    {
        GameManager.Instance?.ResetGame();
        KillAllTweens();
        AudioManager.Instance?.StopMusic();
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadCharacterSelection();
        }
        else
        {
            Debug.LogWarning("SceneLoader.Instance es null, cargando escena directamente");
            UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelection");
        }
    }

    private void KillAllTweens()
    {
        menuAnimationSequence?.Kill();
        if (audienceContainer != null)
        {
            foreach (Transform audienceMember in audienceContainer)
            {
                audienceMember.DOKill();
            }
        }
        if (centralObject != null)
        {
            centralObject.DOKill();
        }
        if (lightImages != null)
        {
            foreach (RectTransform light in lightImages)
            {
                light.DOKill();
            }
        }
        if (cameramanContainer != null)
        {
            foreach (Transform cameraman in cameramanContainer)
            {
                cameraman.DOKill();
            }
        }
        DOTween.KillAll();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void OnIntroComplete()
    {
        PlayMenuAnimations();
    }

    private void PlayMenuAnimations()
    {
        Debug.Log("PlayMenuAnimations() ejecutándose");
        menuAnimationSequence = DOTween.Sequence();

        if (menuOptionsContainer != null)
        {
            menuAnimationSequence.AppendInterval(menuOptionsDelay);
            menuAnimationSequence.AppendCallback(() => menuOptionsContainer.gameObject.SetActive(true));
            menuAnimationSequence.Append(menuOptionsContainer.DOLocalMove(menuOptionsOriginalPosition, menuOptionsMoveDuration).SetEase(Ease.OutQuart));
        }

        if (titleTransform != null)
        {
            menuAnimationSequence.AppendInterval(titleDelay);
            menuAnimationSequence.AppendCallback(() => titleTransform.gameObject.SetActive(true));
            menuAnimationSequence.Join(titleTransform.DOScale(titleOriginalScale, titlePopDuration).SetEase(Ease.OutBack));
        }

        if (audienceContainer != null)
        {
            if (audienceContainer.childCount > 0)
            {
                Debug.Log($"AudienceContainer tiene {audienceContainer.childCount} hijos");
                AnimateAudience();
            }
            else
            {
                Debug.Log("AudienceContainer está vacío, no hay nada que animar");
            }
        }
        else
        {
            Debug.LogWarning("AudienceContainer no está asignado en el Inspector");
        }

        if (centralObject != null)
        {
            AnimateCentralObject();
        }

        if (lightImages != null && lightImages.Length > 0)
        {
            
            AnimateLightImages();
        }

        if (cameramanContainer != null)
        {
            if (cameramanContainer.childCount > 0)
            {
               
                AnimateCameramen();
            }
            else
            {
                Debug.Log("CameramanContainer está vacío, no hay nada que animar");
            }
        }
        else
        {
            Debug.LogWarning("CameramanContainer no está asignado en el Inspector");
        }

        menuAnimationSequence.Play();
    }

    private void AnimateAudience()
    {
        int index = 0;
        foreach (Transform audienceMember in audienceContainer)
        {
            Vector3 originalScale = audienceMember.localScale;
            Vector3 targetScale = originalScale * audienceScaleAmount;
            
            float delay = index * 0.1f;
            
            Sequence scaleSequence = DOTween.Sequence();
            scaleSequence.Append(audienceMember.DOScale(targetScale, audienceScaleDuration / 2f).SetEase(Ease.InOutSine).SetDelay(delay));
            scaleSequence.Append(audienceMember.DOScale(originalScale, audienceScaleDuration / 2f).SetEase(Ease.InOutSine));
            scaleSequence.SetLoops(-1, LoopType.Restart);
            
            index++;
        }
    }

    private void AnimateCentralObject()
    {
        Vector2 stretchedSize = new Vector2(centralOriginalSize.x, centralOriginalSize.y + centralStretchAmount);
        
        Sequence stretchSequence = DOTween.Sequence();
        stretchSequence.Append(centralObject.DOSizeDelta(stretchedSize, centralStretchDuration / 2f).SetEase(Ease.InOutSine));
        stretchSequence.Append(centralObject.DOSizeDelta(centralOriginalSize, centralStretchDuration / 2f).SetEase(Ease.InOutSine));
        stretchSequence.SetLoops(-1, LoopType.Restart);
    }

    private void AnimateLightImages()
    {
        for (int i = 0; i < lightImages.Length; i++)
        {
            RectTransform light = lightImages[i];
            Vector3 originalPos = light.localPosition;
            float direction = i % 2 == 0 ? 1f : -1f;
            
            Sequence lightSequence = DOTween.Sequence();
            lightSequence.Append(light.DOLocalMove(originalPos + Vector3.right * lightsMoveAmount * direction, lightsMoveDuration / 2f).SetEase(Ease.InOutSine).SetDelay(i * 0.2f));
            lightSequence.Append(light.DOLocalMove(originalPos + Vector3.left * lightsMoveAmount * direction, lightsMoveDuration).SetEase(Ease.InOutSine));
            lightSequence.Append(light.DOLocalMove(originalPos, lightsMoveDuration / 2f).SetEase(Ease.InOutSine));
            lightSequence.SetLoops(-1, LoopType.Restart);
        }
    }

    private void AnimateCameramen()
    {
        int index = 0;
        foreach (Transform cameraman in cameramanContainer)
        {
            Vector3 originalPos = cameraman.localPosition;
            Vector3 targetPos = originalPos + Vector3.up * cameramanMoveAmount;
            
            float delay = index * 0.15f;
            
            cameraman.DOLocalMove(targetPos, cameramanMoveDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);
            
            index++;
        }
    }

    private void OnDestroy()
    {
        menuAnimationSequence?.Kill();
        if (audienceContainer != null)
        {
            foreach (Transform audienceMember in audienceContainer)
            {
                audienceMember.DOKill();
            }
        }
        if (centralObject != null)
        {
            centralObject.DOKill();
        }
        if (lightImages != null)
        {
            foreach (RectTransform light in lightImages)
            {
                light.DOKill();
            }
        }
        if (cameramanContainer != null)
        {
            foreach (Transform cameraman in cameramanContainer)
            {
                cameraman.DOKill();
            }
        }
    }
}
