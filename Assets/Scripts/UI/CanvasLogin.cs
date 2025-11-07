using Mirror;
using UnityEngine;
using TMPro;

public class CanvasLogin : MonoBehaviour
{
    private NetworkManager manager;

    [Header("UI Fields")]
    [SerializeField] TMP_InputField IF_username;
    [SerializeField] TMP_InputField IF_password;

    private bool isConnecting = false;
    private string pendingUsername;
    private string pendingPassword;

    void Start()
    {
        manager = FindAnyObjectByType<NetworkManager>();
        if (manager == null)
            Debug.LogError("[CanvasLogin] No NetworkManager found in scene!");
    }

    // ==========================================================
    // =============== LOGIN / REGISTER ==========================
    // ==========================================================

    public void TryLogin()
    {
        string username = IF_username.text.Trim();
        string password = IF_password.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[CanvasLogin] Username or password cannot be empty.");
            return;
        }

        // already connected then just try login
        if (NetworkClient.isConnected)
        {
            ClientAccountAPI.Instance.TryLogin(username, password);
            return;
        }

        if (isConnecting)
        {
            Debug.Log("[CanvasLogin] Already connecting...");
            return;
        }

        Debug.Log("[CanvasLogin] Connecting to server first...");
        isConnecting = true;

        pendingUsername = username;
        pendingPassword = password;

        manager.StartClient();

        NetworkClient.OnConnectedEvent += OnConnectedLogin;
        NetworkClient.OnDisconnectedEvent += OnDisconnectedLogin;
    }

    public void TryRegister()
    {
        string username = IF_username.text.Trim();
        string password = IF_password.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[CanvasLogin] Username or password cannot be empty.");
            return;
        }

        if (NetworkClient.isConnected)
        {
            ClientAccountAPI.Instance.TryRegister(username, password);
            return;
        }

        if (isConnecting)
        {
            Debug.Log("[CanvasLogin] Already connecting...");
            return;
        }

        Debug.Log("[CanvasLogin] Connecting to server first...");
        isConnecting = true;

        pendingUsername = username;
        pendingPassword = password;

        manager.StartClient();

        NetworkClient.OnConnectedEvent += OnConnectedRegister;
        NetworkClient.OnDisconnectedEvent += OnDisconnectedRegister;
    }

    // ==========================================================
    // =============== CALLBACKS ================================
    // ==========================================================

    private void OnConnectedLogin()
    {
        Debug.Log("[CanvasLogin] Connected! Sending login request...");
        ClientAccountAPI.Instance.TryLogin(pendingUsername, pendingPassword);
        CleanupCallbacks();
    }

    private void OnDisconnectedLogin()
    {
        Debug.LogWarning("[CanvasLogin] Failed to connect (login).");
        CleanupCallbacks();
    }

    private void OnConnectedRegister()
    {
        Debug.Log("[CanvasLogin] Connected! Sending register request...");
        ClientAccountAPI.Instance.TryRegister(pendingUsername, pendingPassword);
        CleanupCallbacks();
    }

    private void OnDisconnectedRegister()
    {
        Debug.LogWarning("[CanvasLogin] Failed to connect (register).");
        CleanupCallbacks();
    }

    private void CleanupCallbacks()
    {
        NetworkClient.OnConnectedEvent -= OnConnectedLogin;
        NetworkClient.OnConnectedEvent -= OnConnectedRegister;
        NetworkClient.OnDisconnectedEvent -= OnDisconnectedLogin;
        NetworkClient.OnDisconnectedEvent -= OnDisconnectedRegister;

        pendingUsername = null;
        pendingPassword = null;
        isConnecting = false;
    }
}
