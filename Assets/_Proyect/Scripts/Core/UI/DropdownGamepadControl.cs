using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Adjuntá al mismo GameObject que tiene el TMP_Dropdown.
/// Cuando está seleccionado:
///   - A (Xbox) / X (PS): abre/cierra el dropdown
///   - Stick up/down o D-Pad: navega entre opciones (cuando está abierto)
/// </summary>
[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownGamepadControl : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    private bool isSelected = false;
    private float navCooldown = 0f;
    private const float NAV_DELAY = 0.2f;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        // Chequea si este objeto está seleccionado
        isSelected = EventSystem.current.currentSelectedGameObject == gameObject;
        if (!isSelected) return;

        navCooldown -= Time.unscaledDeltaTime;

        // A: abrir/cerrar
        if (PressedConfirm())
        {
            // TMP_Dropdown maneja su propio estado interno
            // Simulamos un click para toggle
            dropdown.Show(); // Si ya está abierto, no pasa nada
        }

        // Navegación vertical (cuando el dropdown está abierto Unity lo maneja
        // automáticamente vía EventSystem, pero agregamos soporte manual también)
        if (navCooldown <= 0f)
        {
            float vert = ReadVerticalInput();
            if (vert > 0.5f)
            {
                dropdown.value = Mathf.Max(0, dropdown.value - 1);
                navCooldown = NAV_DELAY;
            }
            else if (vert < -0.5f)
            {
                dropdown.value = Mathf.Min(dropdown.options.Count - 1, dropdown.value + 1);
                navCooldown = NAV_DELAY;
            }
        }
    }

    private bool PressedConfirm()
    {
        foreach (var gp in Gamepad.all)
            if (gp.buttonSouth.wasPressedThisFrame) return true;
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) return true;
        return false;
    }

    private float ReadVerticalInput()
    {
        foreach (var gp in Gamepad.all)
        {
            float stick = gp.leftStick.y.ReadValue();
            if (Mathf.Abs(stick) > 0.4f) return stick;
            if (gp.dpad.up.isPressed) return 1f;
            if (gp.dpad.down.isPressed) return -1f;
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed) return 1f;
            if (Keyboard.current.downArrowKey.isPressed) return -1f;
        }
        return 0f;
    }
}
