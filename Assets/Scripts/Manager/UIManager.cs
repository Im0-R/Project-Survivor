using Unity.Netcode;
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

        // Listen for network events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void Update()
    {
        // Check for ESC key press while in-game
        if (gameUICanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            DisconnectAndReturnToLogin();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed - Showing Spells Reward UI");
            ShowSpellsRewardUI();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only switch UI if this is the local player
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            //connect the UI to the player entity
            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<PlayerEntity>();

            ShowGameUI();
            PlayerUI.Instance.SetPlayer(player);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Only reset UI if this is the local player
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            ShowLoginUI();
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
    public void DisconnectAndReturnToLogin()
    {
        // Shut down network session
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Reset UI back to login
        ShowLoginUI();
    }
}
