using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject generalCanvasParent;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private GameObject spellsRewardCanvas;

    private void Awake()
    {
        // Ensure there is only one instance of UIManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Start with login UI visible
        ShowLoginUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed - Showing Spells Reward UI");
            ShowSpellsRewardUI();
        }
    }

    public void ShowLoginUI()
    {
        loginCanvas.SetActive(true);
        gameUICanvas.SetActive(false);
    }

    public void ShowLoadingUI()
    {
        loginCanvas.SetActive(false);
        gameUICanvas.SetActive(false);
    }

    public void ShowGameUI()
    {
        loginCanvas.SetActive(false);
        gameUICanvas.SetActive(true);
    }
    public void ShowSpellsRewardUI()
    {
        Instantiate(spellsRewardCanvas, generalCanvasParent.transform);
        gameUICanvas.SetActive(false);
        TimeManager.Instance.PauseGame();
    }
    public void HideSpellsRewardUI()
    {
        var rewardCanvas = FindFirstObjectByType<RewardSpellsCanvas>();
        if (rewardCanvas != null)
        {
            Destroy(rewardCanvas.gameObject);
        }
        gameUICanvas.SetActive(true);
        TimeManager.Instance.ResumeGame();
    }
}
