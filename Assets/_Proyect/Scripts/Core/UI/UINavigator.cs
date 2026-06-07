using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UINavigator : MonoBehaviour
{
    [Header("Elementos navegables (en orden de navegación)")]
    [SerializeField] private List<Selectable> navigableElements = new List<Selectable>();

    [Header("Comportamiento")]
    [SerializeField] private Button backButton;
    [SerializeField] private bool autoSelectOnEnable = true;
    [SerializeField] private int defaultSelectedIndex = 0;
    [SerializeField] private bool wrapAround = true;

    private int currentIndex = -1;
    private float navCooldown = 0f;
    private const float NAV_DELAY = 0.2f;
    private bool inputEnabled = false; // controlado por UINavigatorStack

    // ─── Ciclo de vida ────────────────────────────────────────────────

    private void OnEnable()
    {
        // Se registra en el stack — el stack decide si tiene input o no
        UINavigatorStack.Push(this);

        if (autoSelectOnEnable)
            SelectElement(defaultSelectedIndex);
    }

    private void OnDisable()
    {
        ApplyHoverExit(currentIndex);
        UINavigatorStack.Pop(this);
        currentIndex = -1;
    }

    /// <summary>Llamado por UINavigatorStack para activar/desactivar el input</summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        // Si pierde el foco, quita el hover visual
        if (!enabled)
            ApplyHoverExit(currentIndex);
        // Si lo recupera, vuelve a mostrar el hover
        else if (currentIndex >= 0)
            ApplyHoverEnter(currentIndex);
    }

    // ─── Update ───────────────────────────────────────────────────────

    private void Update()
    {
        if (!inputEnabled) return;

        navCooldown -= Time.unscaledDeltaTime;

        Vector2 nav = ReadNavigationInput();

        if (navCooldown <= 0f)
        {
            if (nav.y > 0.5f) { Navigate(-1); navCooldown = NAV_DELAY; }
            if (nav.y < -0.5f) { Navigate(+1); navCooldown = NAV_DELAY; }
        }

        if (PressedConfirm()) InvokeCurrentElement();
        if (PressedBack()) InvokeBack();
    }

    // ─── Navegación ───────────────────────────────────────────────────

    private void Navigate(int direction)
    {
        if (navigableElements == null || navigableElements.Count == 0) return;

        int next = currentIndex + direction;

        if (wrapAround)
            next = (next + navigableElements.Count) % navigableElements.Count;
        else
            next = Mathf.Clamp(next, 0, navigableElements.Count - 1);

        SelectElement(next);
    }

    public void SelectElement(int index)
    {
        if (navigableElements == null || navigableElements.Count == 0) return;
        index = Mathf.Clamp(index, 0, navigableElements.Count - 1);

        ApplyHoverExit(currentIndex);
        currentIndex = index;

        var element = navigableElements[currentIndex];
        if (element == null) return;

        EventSystem.current?.SetSelectedGameObject(element.gameObject);
        ApplyHoverEnter(currentIndex);
    }

    private void InvokeCurrentElement()
    {
        if (!IsValidIndex(currentIndex)) return;
        var element = navigableElements[currentIndex];

        if (element is Button btn)
        {
            FindMenuButton(currentIndex)?.DoClick();
            btn.onClick.Invoke();
        }
        else if (element is Toggle toggle)
        {
            toggle.isOn = !toggle.isOn;
        }
    }

    private void InvokeBack()
    {
        if (backButton != null)
            backButton.onClick.Invoke();
    }

    // ─── Hover ────────────────────────────────────────────────────────

    private void ApplyHoverEnter(int index)
        => FindMenuButton(index)?.DoHoverEnter();

    private void ApplyHoverExit(int index)
        => FindMenuButton(index)?.DoHoverExit();

    private MenuButton FindMenuButton(int index)
    {
        if (!IsValidIndex(index)) return null;
        var go = navigableElements[index].gameObject;

        var mb = go.GetComponent<MenuButton>();
        if (mb != null) return mb;

        if (go.transform.parent != null)
        {
            mb = go.transform.parent.GetComponent<MenuButton>();
            if (mb != null) return mb;
        }

        mb = go.GetComponentInChildren<MenuButton>();
        return mb;
    }

    // ─── Input ────────────────────────────────────────────────────────

    private Vector2 ReadNavigationInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
                return Vector2.up;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                return Vector2.down;
        }

        foreach (var gp in Gamepad.all)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (stick.magnitude > 0.4f) return stick;
            if (gp.dpad.up.isPressed) return Vector2.up;
            if (gp.dpad.down.isPressed) return Vector2.down;
        }

        return Vector2.zero;
    }

    private bool PressedConfirm()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame) return true;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        }
        foreach (var gp in Gamepad.all)
            if (gp.buttonSouth.wasPressedThisFrame) return true;
        return false;
    }

    private bool PressedBack()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        foreach (var gp in Gamepad.all)
            if (gp.buttonEast.wasPressedThisFrame) return true;
        return false;
    }

    private bool IsValidIndex(int index)
        => navigableElements != null
           && index >= 0
           && index < navigableElements.Count
           && navigableElements[index] != null;
}