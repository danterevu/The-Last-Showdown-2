using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using System.Collections;

public class CharacterSelectionSceneAnimator : MonoBehaviour
{
    [Header("Intro Elements")]
    public CanvasGroup blackScreen;
    public GameObject leftPillar;
    public GameObject rightPillar;
    public GameObject promptSign;
    public CanvasGroup extraFadeImage;

    [Header("Selection Phase Elements")]
    public GameObject[] lights;
    public GameObject backToMenuButtonObj;
    public GameObject startGameButtonObj;

    [Header("Positions (World Space)")]
    public Vector3 leftPillarStartPos;
    public Vector3 leftPillarEndPos;
    public Vector3 rightPillarStartPos;
    public Vector3 rightPillarEndPos;

    [Header("Positions (Canvas - Anchored Position)")]
    public Vector2 promptSignStartAnchored;
    public Vector2 promptSignEndAnchored;
    public Vector2 promptSignExitAnchored;

    [Header("Timing")]
    public float introDelay = 0.5f;
    public float fadeOutDuration = 1f;
    public float pillarRiseDuration = 1f;
    public float promptSignFallDuration = 1f;
    public float promptSignExitDuration = 0.5f;
    public float extraFadeDuration = 0.8f;
    public float lightsAppearDelay = 0.2f;
    public float buttonAppearDelay = 0.2f;

    [Header("References")]
    public InputAssigner inputAssigner;

    private bool _introPlayed;
    private bool _waitingForInput;
    private Sequence _introSequence;
    private Sequence _selectionSequence;
    private RectTransform _promptSignRectTransform;

    private void Start()
    {
        if (promptSign != null)
        {
            _promptSignRectTransform = promptSign.GetComponent<RectTransform>();
        }

        InitializeScene();
        PlayIntro();
    }

    private void Update()
    {
        if (_waitingForInput && !_introPlayed)
        {
            CheckForAnyInput();
        }
    }

    private void InitializeScene()
    {
        // --- PANTALLA NEGRA ---
        if (blackScreen != null)
        {
            blackScreen.alpha = 1f;
            blackScreen.gameObject.SetActive(true);
        }

        // --- IMAGEN EXTRA DE FADE ---
        if (extraFadeImage != null)
        {
            extraFadeImage.alpha = 0f;
            extraFadeImage.gameObject.SetActive(false);
        }

        // --- PILARES (empezar abajo) ---
        if (leftPillar != null)
            leftPillar.transform.position = leftPillarStartPos;

        if (rightPillar != null)
            rightPillar.transform.position = rightPillarStartPos;

        // --- PROMPT SIGN ---
        if (promptSign != null)
        {
            if (_promptSignRectTransform != null)
            {
                _promptSignRectTransform.anchoredPosition = promptSignStartAnchored;
            }
            else
            {
                promptSign.transform.position = new Vector3(promptSignStartAnchored.x, promptSignStartAnchored.y, 0f);
            }
            promptSign.SetActive(false);
        }

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

    private void PlayIntro()
    {
        _introSequence = DOTween.Sequence();
        _introSequence.AppendInterval(introDelay);

        // 1. Fade out black screen
        if (blackScreen != null)
        {
            _introSequence.Append(blackScreen.DOFade(0f, fadeOutDuration).SetEase(Ease.OutQuad));
        }

        // 2. Rise pillars
        if (leftPillar != null)
        {
            _introSequence.Join(leftPillar.transform.DOMove(leftPillarEndPos, pillarRiseDuration).SetEase(Ease.OutBounce));
        }

        if (rightPillar != null)
        {
            _introSequence.Join(rightPillar.transform.DOMove(rightPillarEndPos, pillarRiseDuration).SetEase(Ease.OutBounce));
        }

        // 3. Show and fall prompt sign
        if (promptSign != null)
        {
            _introSequence.AppendCallback(() => promptSign.SetActive(true));
            
            if (_promptSignRectTransform != null)
            {
                _introSequence.Append(_promptSignRectTransform.DOAnchorPos(promptSignEndAnchored, promptSignFallDuration).SetEase(Ease.OutBounce));
            }
            else
            {
                Vector3 endPos = new Vector3(promptSignEndAnchored.x, promptSignEndAnchored.y, 0f);
                _introSequence.Append(promptSign.transform.DOMove(endPos, promptSignFallDuration).SetEase(Ease.OutBounce));
            }
        }

        _introSequence.AppendCallback(() =>
        {
            _introPlayed = true;
            _waitingForInput = true;
            Debug.Log("✅ LISTO: Esperando input del jugador... Presiona CUALQUIER tecla o botón!");
        });

        _introSequence.Play();
    }

    private void CheckForAnyInput()
    {
        bool anyInput = false;

        // --- DETECTAR CUALQUIER TECLA DEL TECLADO (incluyendo espacio, enter, flechas, etc.)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                anyInput = true;
                Debug.Log("✅ Input detectado: TECLADO");
            }
        }

        // --- DETECTAR CUALQUIER MANDO
        if (!anyInput && Gamepad.all.Count > 0)
        {
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad != null)
                {
                    bool gamepadPressed = false;
                    gamepadPressed |= gamepad.buttonSouth.wasPressedThisFrame;
                    gamepadPressed |= gamepad.buttonEast.wasPressedThisFrame;
                    gamepadPressed |= gamepad.buttonWest.wasPressedThisFrame;
                    gamepadPressed |= gamepad.buttonNorth.wasPressedThisFrame;
                    gamepadPressed |= gamepad.dpad.up.wasPressedThisFrame;
                    gamepadPressed |= gamepad.dpad.down.wasPressedThisFrame;
                    gamepadPressed |= gamepad.dpad.left.wasPressedThisFrame;
                    gamepadPressed |= gamepad.dpad.right.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftStick.up.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftStick.down.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftStick.left.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftStick.right.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightStick.up.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightStick.down.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightStick.left.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightStick.right.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftShoulder.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightShoulder.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftTrigger.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightTrigger.wasPressedThisFrame;
                    gamepadPressed |= gamepad.startButton.wasPressedThisFrame;
                    gamepadPressed |= gamepad.selectButton.wasPressedThisFrame;
                    gamepadPressed |= gamepad.leftStickButton.wasPressedThisFrame;
                    gamepadPressed |= gamepad.rightStickButton.wasPressedThisFrame;

                    if (gamepadPressed)
                    {
                        anyInput = true;
                        Debug.Log("✅ Input detectado: MANDO");
                        break;
                    }
                }
            }
        }

        if (anyInput)
        {
            Debug.Log("🚀 Iniciando fase de selección...");
            StartSelectionPhase();
        }
    }

    private void StartSelectionPhase()
    {
        _waitingForInput = false;

        _selectionSequence = DOTween.Sequence();

        // 1. Exit prompt sign
        if (promptSign != null)
        {
            if (_promptSignRectTransform != null)
            {
                _selectionSequence.Append(_promptSignRectTransform.DOAnchorPos(promptSignExitAnchored, promptSignExitDuration).SetEase(Ease.InQuad));
            }
            else
            {
                Vector3 exitPos = new Vector3(promptSignExitAnchored.x, promptSignExitAnchored.y, 0f);
                _selectionSequence.Append(promptSign.transform.DOMove(exitPos, promptSignExitDuration).SetEase(Ease.InQuad));
            }
            _selectionSequence.AppendCallback(() => 
            {
                promptSign.SetActive(false);
                Debug.Log("✅ Prompt sign desactivado!");
            });
        }

        // 2. Show and fade out extra image
        if (extraFadeImage != null)
        {
            _selectionSequence.AppendCallback(() => 
            {
                extraFadeImage.gameObject.SetActive(true);
                extraFadeImage.alpha = 1f;
            });
            _selectionSequence.Append(extraFadeImage.DOFade(0f, extraFadeDuration).SetEase(Ease.OutQuad));
            _selectionSequence.AppendCallback(() => extraFadeImage.gameObject.SetActive(false));
        }

        // 3. Activar luces
        if (lights != null && lights.Length > 0)
        {
            _selectionSequence.AppendInterval(lightsAppearDelay);
            _selectionSequence.AppendCallback(() =>
            {
                Debug.Log("💡 Activando luces! Cantidad: " + lights.Length);
                foreach (var light in lights)
                {
                    if (light != null)
                    {
                        light.SetActive(true);
                        Debug.Log("✅ Luz activada: " + light.name);
                    }
                }
            });
        }

        // 4. Show buttons and activate InputAssigner
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
                Debug.Log("✅ InputAssigner activado! Ahora puedes elegir personajes!");
            }
        });

        _selectionSequence.Play();
    }

    private void OnDestroy()
    {
        _introSequence?.Kill();
        _selectionSequence?.Kill();
    }
}
