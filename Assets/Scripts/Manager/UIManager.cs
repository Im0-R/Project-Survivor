using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Base UI")]
    [SerializeField] private Transform generalCanvasParent;
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private GameObject inventoryCanvas;

    [Header("Spell Reward UI")]
    [SerializeField] private GameObject spellsRewardCanvas;
    [SerializeField] private GameObject spellChoicePrefab;

    private RewardSpellsCanvas currentRewardCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        ShowGameUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryCanvas != null && inventoryCanvas.activeSelf)
                HideInventoryUI();
            else
                ShowInventoryUI();
        }
    }

    public void ShowLoadingUI()
    {
        if (gameUICanvas != null)
            gameUICanvas.SetActive(false);
    }

    public void ShowGameUI()
    {
        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);
    }

    public void ShowSpellsRewardUI(string[] spellNames, int level)
    {
        if (currentRewardCanvas != null)
            return;

        if (spellsRewardCanvas == null)
        {
            Debug.LogError("[UIManager] spellsRewardCanvas is not assigned.");
            return;
        }

        if (spellChoicePrefab == null)
        {
            Debug.LogError("[UIManager] spellChoicePrefab is not assigned.");
            return;
        }

        GameObject ui = Instantiate(spellsRewardCanvas, generalCanvasParent);
        currentRewardCanvas = ui.GetComponent<RewardSpellsCanvas>();

        if (currentRewardCanvas == null)
        {
            Debug.LogError("[UIManager] RewardSpellsCanvas component missing on spellsRewardCanvas prefab.");
            Destroy(ui);
            return;
        }

        currentRewardCanvas.Init(spellNames, level, spellChoicePrefab);

        if (gameUICanvas != null)
            gameUICanvas.SetActive(false);

        PlayerPauseController.Local?.RequestPause();
    }

    public void HideSpellsRewardUI()
    {
        if (currentRewardCanvas != null)
        {
            Destroy(currentRewardCanvas.gameObject);
            currentRewardCanvas = null;
        }

        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);

        PlayerPauseController.Local?.RequestResume();
    }

    public void ShowInventoryUI()
    {
        if (inventoryCanvas == null) return;
        inventoryCanvas.SetActive(true);
    }

    public void HideInventoryUI()
    {
        if (inventoryCanvas == null) return;

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();

        inventoryCanvas.SetActive(false);
    }
}