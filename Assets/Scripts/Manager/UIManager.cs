using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject generalCanvasParent;
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private GameObject spellsRewardCanvas;
    [SerializeField] private GameObject inventoryCanvas;

    private void Awake()
    {
        // Ensure there is only one instance of UIManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ShowGameUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed - Showing Spells Reward UI");
            ShowSpellsRewardUI();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryCanvas.activeSelf)
                HideInventoryUI();
            else
                ShowInventoryUI();
        }
    }

    public void ShowLoadingUI()
    {
        gameUICanvas.SetActive(false);
    }

    public void ShowGameUI()
    {
        gameUICanvas.SetActive(true);
    }
    public void ShowSpellsRewardUI()
    {
        if (FindFirstObjectByType<RewardSpellsCanvas>() != null) return;

        Debug.Log("Showing Spells Reward UI");

        Instantiate(spellsRewardCanvas, generalCanvasParent.transform);
        gameUICanvas.SetActive(false);

        PlayerPauseController.Local?.RequestPause();

    }

    public void HideSpellsRewardUI()
    {
        var rewardCanvas = FindFirstObjectByType<RewardSpellsCanvas>();
        if (rewardCanvas != null) Destroy(rewardCanvas.gameObject);

        gameUICanvas.SetActive(true);

        PlayerPauseController.Local?.RequestResume();
    }



    // Management for Inventory UI
    public void ShowInventoryUI()
    {
        if (inventoryCanvas == null) return;

        inventoryCanvas.SetActive(true);
    }

    public void HideInventoryUI()
    {
        if (inventoryCanvas == null) return;

        inventoryCanvas.SetActive(false);
    }
}
