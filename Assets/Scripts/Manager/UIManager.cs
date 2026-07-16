using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Scene UI")]
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private Transform windowsParent;

    [Header("Navigation Input")]
    [SerializeField] private InputActionReference backAction;

    [Header("Spell Reward Modal")]
    [FormerlySerializedAs("generalCanvasParent")]
    [SerializeField] private Transform modalParent;

    [FormerlySerializedAs("spellsRewardCanvas")]
    [SerializeField] private GameObject spellsRewardCanvasPrefab;
    [SerializeField] private GameObject spellChoicePrefab;
    [SerializeField] private bool rewardCanCloseWithEscape = true;

    private readonly Dictionary<UIWindowId, UIWindow> windows = new();
    private readonly List<UIWindow> openWindows = new();
    private readonly Dictionary<InputAction, UIWindow> windowsByAction = new();

    private RewardSpellsCanvas currentRewardCanvas;
    private bool inputsSubscribed;

    public bool IsModalOpen
    {
        get
        {
            if (currentRewardCanvas != null)
                return true;

            foreach (UIWindow window in openWindows)
            {
                if (window != null &&
                    window.IsOpen &&
                    window.Mode == UIWindowMode.Modal)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RegisterSceneWindows();
    }

    private void Start()
    {
        // Start is intentionally used here so child windows can run Awake first.
        InitializeWindowsClosed();
        ShowGameUI();
    }

    private void OnEnable()
    {
        SubscribeInputs();
    }

    private void OnDisable()
    {
        UnsubscribeInputs();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void RegisterSceneWindows()
    {
        windows.Clear();

        Transform searchRoot = windowsParent != null
            ? windowsParent
            : transform;

        UIWindow[] foundWindows =
            searchRoot.GetComponentsInChildren<UIWindow>(true);

        foreach (UIWindow window in foundWindows)
        {
            if (window == null)
                continue;

            if (window.Id == UIWindowId.None)
            {
                Debug.LogError(
                    "[UIManager] A UIWindow has no ID.",
                    window
                );
                continue;
            }

            if (!windows.TryAdd(window.Id, window))
            {
                Debug.LogError(
                    $"[UIManager] Duplicate UIWindowId: {window.Id}.",
                    window
                );
            }
        }
    }

    private void SubscribeInputs()
    {
        if (inputsSubscribed)
            return;

        windowsByAction.Clear();

        InputAction back = GetAction(backAction);

        if (back != null)
        {
            back.performed += OnBackPerformed;
            back.Enable();
        }

        foreach (UIWindow window in windows.Values)
        {
            InputAction toggle = window.ToggleAction;

            if (toggle == null)
                continue;

            if (toggle == back)
            {
                Debug.LogError(
                    $"[UIManager] {window.Id} uses the Back action as its toggle action.",
                    window
                );
                continue;
            }

            if (!windowsByAction.TryAdd(toggle, window))
            {
                Debug.LogError(
                    $"[UIManager] Input action '{toggle.name}' is assigned to multiple windows.",
                    window
                );
                continue;
            }

            toggle.performed += OnWindowTogglePerformed;
            toggle.Enable();
        }

        inputsSubscribed = true;
    }

    private void UnsubscribeInputs()
    {
        if (!inputsSubscribed)
            return;

        InputAction back = GetAction(backAction);

        if (back != null)
        {
            back.performed -= OnBackPerformed;
            back.Disable();
        }

        foreach (InputAction action in windowsByAction.Keys)
        {
            if (action == null)
                continue;

            action.performed -= OnWindowTogglePerformed;
            action.Disable();
        }

        windowsByAction.Clear();
        inputsSubscribed = false;
    }

    private void OnBackPerformed(InputAction.CallbackContext context)
    {
        HandleBack();
    }

    private void OnWindowTogglePerformed(
        InputAction.CallbackContext context)
    {
        if (!windowsByAction.TryGetValue(
                context.action,
                out UIWindow window))
        {
            return;
        }

        // The currently opened generic modal may close itself with its shortcut.
        // Every other shortcut is blocked while a modal is visible.
        if (IsModalOpen && !window.IsOpen)
            return;

        ToggleWindow(window.Id);
    }

    private static InputAction GetAction(
        InputActionReference reference)
    {
        return reference != null ? reference.action : null;
    }

    private void InitializeWindowsClosed()
    {
        openWindows.Clear();

        foreach (UIWindow window in windows.Values)
            window.SetVisible(false, false);
    }

    public void ToggleWindow(UIWindowId id)
    {
        if (!TryGetWindow(id, out UIWindow window))
            return;

        if (window.IsOpen)
            CloseWindow(id);
        else
            OpenWindow(id);
    }

    public void OpenWindow(UIWindowId id)
    {
        if (!TryGetWindow(id, out UIWindow target))
            return;

        if (target.IsOpen)
            return;

        // A regular window cannot open over an existing modal.
        if (IsModalOpen && target.Mode != UIWindowMode.Modal)
            return;

        if (target.Mode == UIWindowMode.Exclusive)
            CloseWindowsOfMode(UIWindowMode.Exclusive);

        if (target.Mode == UIWindowMode.Modal)
            CloseWindowsOfMode(UIWindowMode.Modal);

        target.SetVisible(true, true);
        openWindows.Remove(target);
        openWindows.Add(target);
    }

    public void CloseWindow(UIWindowId id)
    {
        if (!TryGetWindow(id, out UIWindow target))
            return;

        CloseWindow(target);
    }

    // Compatibility helpers for existing buttons and scripts.
    public void ShowInventoryUI()
    {
        OpenWindow(UIWindowId.Inventory);
    }

    public void HideInventoryUI()
    {
        CloseWindow(UIWindowId.Inventory);
    }

    public void CloseAllWindows()
    {
        for (int i = openWindows.Count - 1; i >= 0; i--)
        {
            UIWindow window = openWindows[i];

            if (window != null && window.IsOpen)
                window.SetVisible(false, true);
        }

        openWindows.Clear();
    }

    public void HandleBack()
    {
        if (currentRewardCanvas != null)
        {
            if (rewardCanCloseWithEscape)
                HideSpellsRewardUI();

            return;
        }

        for (int i = openWindows.Count - 1; i >= 0; i--)
        {
            UIWindow window = openWindows[i];

            if (window == null || !window.IsOpen)
            {
                openWindows.RemoveAt(i);
                continue;
            }

            if (!window.CloseOnEscape)
                return;

            CloseWindow(window);
            return;
        }
    }

    private void CloseWindowsOfMode(UIWindowMode mode)
    {
        for (int i = openWindows.Count - 1; i >= 0; i--)
        {
            UIWindow window = openWindows[i];

            if (window == null)
            {
                openWindows.RemoveAt(i);
                continue;
            }

            if (window.Mode == mode)
                CloseWindow(window);
        }
    }

    private void CloseWindow(UIWindow window)
    {
        if (window == null)
            return;

        if (window.IsOpen)
            window.SetVisible(false, true);

        openWindows.Remove(window);
    }

    private bool TryGetWindow(UIWindowId id, out UIWindow window)
    {
        if (windows.TryGetValue(id, out window) && window != null)
            return true;

        Debug.LogWarning($"[UIManager] Window not registered: {id}.", this);
        return false;
    }

    public void ShowLoadingUI()
    {
        CloseAllWindows();

        if (gameUICanvas != null)
            gameUICanvas.SetActive(false);

        if (loadingCanvas != null)
            loadingCanvas.SetActive(true);
    }

    public void ShowGameUI()
    {
        if (loadingCanvas != null)
            loadingCanvas.SetActive(false);

        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);
    }

    public void ShowSpellsRewardUI(string[] spellNames, int level)
    {
        if (currentRewardCanvas != null)
            return;

        if (spellsRewardCanvasPrefab == null || spellChoicePrefab == null)
        {
            Debug.LogError(
                "[UIManager] Reward prefabs are not fully assigned.",
                this
            );
            return;
        }

        CloseAllWindows();

        Transform parent = modalParent != null
            ? modalParent
            : transform;

        GameObject instance = Instantiate(
            spellsRewardCanvasPrefab,
            parent
        );

        currentRewardCanvas =
            instance.GetComponent<RewardSpellsCanvas>();

        if (currentRewardCanvas == null)
        {
            Debug.LogError(
                "[UIManager] RewardSpellsCanvas component is missing.",
                instance
            );

            Destroy(instance);
            return;
        }

        currentRewardCanvas.Init(
            spellNames,
            level,
            spellChoicePrefab
        );

        if (gameUICanvas != null)
            gameUICanvas.SetActive(false);

        PlayerPauseController.Local?.RequestPause();
    }

    public void HideSpellsRewardUI()
    {
        if (currentRewardCanvas == null)
            return;

        Destroy(currentRewardCanvas.gameObject);
        currentRewardCanvas = null;

        ShowGameUI();
        PlayerPauseController.Local?.RequestResume();
    }
}
