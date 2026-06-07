using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Adjuntá este script al mismo GameObject que tiene el Slider.
/// Cuando el slider está seleccionado (el UINavigator lo seleccionó),
/// el stick horizontal izquierdo del mando lo mueve.
///
/// También funciona con las flechas izquierda/derecha del teclado.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderGamepadControl : MonoBehaviour
{
    [Tooltip("Qué tan rápido se mueve el slider con el stick/flechas")]
    [SerializeField] private float moveSpeed = 0.5f;

    private Slider slider;
    private bool isSelected = false;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Update()
    {
        // Solo mueve si este slider está seleccionado en el EventSystem
        if (EventSystem.current == null) return;
        isSelected = EventSystem.current.currentSelectedGameObject == gameObject;
        if (!isSelected) return;

        float input = ReadHorizontalInput();
        if (Mathf.Abs(input) > 0.1f)
        {
            slider.value += input * moveSpeed * Time.unscaledDeltaTime;
            slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
        }
    }

    private float ReadHorizontalInput()
    {
        // Teclado
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) return -1f;
            if (Keyboard.current.rightArrowKey.isPressed) return 1f;
        }

        // Cualquier gamepad
        foreach (var gp in Gamepad.all)
        {
            float stick = gp.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > 0.3f) return stick;

            if (gp.dpad.left.isPressed) return -1f;
            if (gp.dpad.right.isPressed) return 1f;
        }

        return 0f;
    }
}
