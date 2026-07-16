using UnityEngine;
using UnityEngine.InputSystem;

public enum UIWindowId
{
    None,
    Inventory,
    Stats,
    Stash,
    Trade,
    Arcana,
    MapDifficulty,
    Options
}

public enum UIWindowMode
{
    // Opening this window closes the other Exclusive windows.
    Exclusive,

    // Can remain open at the same time as another window.
    Additive,

    // Blocks regular windows until it is closed.
    Modal
}
[DisallowMultipleComponent]
public class UIWindow : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private UIWindowId id;
    [SerializeField] private UIWindowMode mode = UIWindowMode.Exclusive;

    [Header("Behaviour")]
    [SerializeField] private bool closeOnEscape = true;

    [Header("Optional shortcut")]
    [Tooltip("Leave empty when the window is opened by gameplay, like a stash NPC.")]
    [SerializeField] private InputActionReference toggleAction;

    public UIWindowId Id => id;
    public UIWindowMode Mode => mode;
    public bool CloseOnEscape => closeOnEscape;
    public InputAction ToggleAction =>
        toggleAction != null ? toggleAction.action : null;
    public bool IsOpen => gameObject.activeSelf;

    public void RequestOpen()
    {
        UIManager.Instance?.OpenWindow(id);
    }

    public void RequestClose()
    {
        UIManager.Instance?.CloseWindow(id);
    }

    public void RequestToggle()
    {
        UIManager.Instance?.ToggleWindow(id);
    }

    internal void SetVisible(bool visible, bool notify)
    {
        if (gameObject.activeSelf == visible)
            return;

        if (!visible && notify)
            OnBeforeClosed();

        gameObject.SetActive(visible);

        if (!notify)
            return;

        if (visible)
            OnOpened();
        else
            OnClosed();
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnBeforeClosed()
    {
    }

    protected virtual void OnClosed()
    {
    }
}