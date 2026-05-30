using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class LogoIntroSequence : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image blackScreen;
    [SerializeField] private Image logoImage;
    [SerializeField] private AudioClip logoSound;
    [SerializeField] private TextMeshProUGUI topText;
    [SerializeField] private TextMeshProUGUI bottomText;

    [Header("Configuración de la secuencia")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float moveToCenterDuration = 1.5f;
    [SerializeField] private float rotateDuration = 1.5f;
    [SerializeField] private float scaleUpDuration = 0.5f;
    [SerializeField] private float stayDuration = 1f;
    [SerializeField] private float exitDuration = 1.5f;
    [SerializeField] private float fadeOutBlackScreenDuration = 0.5f;
    [SerializeField] private float flashDelay = 0.3f;
    [SerializeField] private Vector3 startPosition = new Vector3(1000, 0, 0);
    [SerializeField] private Vector3 exitPosition = new Vector3(-1000, 0, 0);
    [SerializeField] private float initialScale = 0.5f;
    [SerializeField] private float scaleUpAmount = 1.1f;
    [SerializeField] private float rotationAmount = 360f;
    [SerializeField] private float topTextFadeInDuration = 1f;
    [SerializeField] private float bottomTextFadeInDuration = 0.8f;
    [SerializeField] private float textFadeOutDuration = 0.8f;

    [Header("Menú Principal")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private Menu menu;
    [SerializeField] private float menuMusicOffset = 0f;

    private Sequence introSequence;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayIntroSequence()
    {
        if (blackScreen == null || logoImage == null)
        {
            Debug.LogError("Faltan referencias en LogoIntroSequence");
            ShowMainMenu();
            return;
        }

        blackScreen.gameObject.SetActive(true);
        logoImage.gameObject.SetActive(true);
        logoImage.rectTransform.localPosition = startPosition;
        logoImage.rectTransform.localRotation = Quaternion.identity;
        logoImage.rectTransform.localScale = Vector3.one * initialScale;
        blackScreen.color = Color.black;

        if (topText != null)
        {
            topText.gameObject.SetActive(true);
            topText.color = new Color(topText.color.r, topText.color.g, topText.color.b, 0);
        }

        if (bottomText != null)
        {
            bottomText.gameObject.SetActive(true);
            bottomText.color = new Color(bottomText.color.r, bottomText.color.g, bottomText.color.b, 0);
        }

        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
        }

        introSequence = DOTween.Sequence();

        float currentTime = 0f;

        introSequence.AppendInterval(initialDelay);
        currentTime += initialDelay;

        introSequence.Append(logoImage.rectTransform.DOLocalMove(Vector3.zero, moveToCenterDuration).SetEase(Ease.OutBack));
        introSequence.Join(logoImage.rectTransform.DORotate(new Vector3(0, 0, rotationAmount), rotateDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        currentTime += moveToCenterDuration;
        
        if (topText != null)
        {
            introSequence.Join(topText.DOFade(1, topTextFadeInDuration).SetEase(Ease.InOutQuad).SetDelay(moveToCenterDuration * 0.5f));
        }
        
        introSequence.AppendCallback(() => PlayLogoSound());
        introSequence.Append(logoImage.rectTransform.DOScale(Vector3.one * scaleUpAmount, scaleUpDuration).SetEase(Ease.OutElastic));
        currentTime += scaleUpDuration;
        
        if (bottomText != null)
        {
            introSequence.Join(bottomText.DOFade(1, bottomTextFadeInDuration).SetEase(Ease.InOutQuad));
        }
        
        introSequence.Append(logoImage.rectTransform.DOScale(Vector3.one * initialScale, scaleUpDuration * 0.8f).SetEase(Ease.InQuad));
        currentTime += scaleUpDuration * 0.8f;
        
        introSequence.AppendInterval(stayDuration);
        currentTime += stayDuration;
        
        if (topText != null)
        {
            introSequence.Join(topText.DOFade(0, textFadeOutDuration).SetEase(Ease.InQuad));
        }
        
        if (bottomText != null)
        {
            introSequence.Join(bottomText.DOFade(0, textFadeOutDuration).SetEase(Ease.InQuad));
        }
        
        introSequence.AppendCallback(() => {
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.SetActive(true);
            }
        });
        introSequence.Append(logoImage.rectTransform.DOLocalMove(exitPosition, exitDuration).SetEase(Ease.InQuad));
        introSequence.Join(logoImage.rectTransform.DORotate(new Vector3(0, 0, rotationAmount * 2), exitDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
        
        float flashTime = currentTime + flashDelay;
        introSequence.Insert(flashTime, blackScreen.DOColor(Color.white, 0.1f).SetEase(Ease.OutQuad));
        introSequence.Insert(flashTime + 0.1f, blackScreen.DOFade(0, fadeOutBlackScreenDuration).SetEase(Ease.OutQuad));
        
        float musicTime = currentTime + menuMusicOffset;
        introSequence.InsertCallback(musicTime, () => AudioManager.Instance?.PlayMusic(SoundID.MenuMusic));
        
        introSequence.AppendCallback(() => ShowMainMenu());

        introSequence.Play();
    }

    private void PlayLogoSound()
    {
        if (logoSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(logoSound);
        }
    }

    private void ShowMainMenu()
    {
        blackScreen.gameObject.SetActive(false);
        logoImage.gameObject.SetActive(false);
        
        if (topText != null)
        {
            topText.gameObject.SetActive(false);
        }
        
        if (bottomText != null)
        {
            bottomText.gameObject.SetActive(false);
        }

        if (menu != null)
        {
            menu.OnIntroComplete();
        }
    }

    private void OnDestroy()
    {
        if (introSequence != null && introSequence.IsActive())
        {
            introSequence.Kill();
        }
    }
}
