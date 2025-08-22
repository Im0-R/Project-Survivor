using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private GameObject gameUICanvas;

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
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only switch UI if this is the local player
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            ShowLoadingUI();
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
        loadingCanvas.SetActive(false);
        gameUICanvas.SetActive(false);
    }

    public void ShowLoadingUI()
    {
        loginCanvas.SetActive(false);
        loadingCanvas.SetActive(true);
        gameUICanvas.SetActive(false);
    }

    public void ShowGameUI()
    {
        loginCanvas.SetActive(false);
        loadingCanvas.SetActive(false);
        gameUICanvas.SetActive(true);
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
