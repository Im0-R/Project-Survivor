using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject loginCanvas;
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
            ShowGameUI();
            //connect the UI to the player entity
            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<PlayerEntity>();

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
