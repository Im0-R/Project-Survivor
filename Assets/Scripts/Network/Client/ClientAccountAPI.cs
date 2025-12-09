using AuthMessages;
using kcp2k;
using Mirror;
using UnityEngine;

public class ClientAccountAPI : MonoBehaviour
{
    public static ClientAccountAPI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Messages reçus depuis le serveur principal
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponse);
        NetworkClient.RegisterHandler<RedirectMessage>(OnRedirect);
    }

    // ==========================================================
    // =============== LOGIN / REGISTER ==========================
    // ==========================================================

    public void TryRegister(string username, string password)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        Debug.Log($"[ClientAccountAPI] Sending RegisterMessage for {username}");
        NetworkClient.Send(new RegisterMessage { username = username, password = password });
    }

    public void TryLogin(string username, string password)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        Debug.Log($"[ClientAccountAPI] Sending LoginMessage for {username}");
        NetworkClient.Send(new LoginMessage { username = username, password = password });
    }

    // ==========================================================
    // =============== AUTH RESPONSE ============================
    // ==========================================================

    private void OnAuthResponse(AuthResponseMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] AuthResponse success={msg.success}, message={msg.message}");

        if (msg.success)
        {
            Debug.Log("[ClientAccountAPI] Authentication successful — waiting for redirection...");
        }
        else
        {
            Debug.LogWarning($"[ClientAccountAPI] Login/Register failed: {msg.message}");
        }
    }

    // ==========================================================
    // =============== REDIRECTION VERS HUB =====================
    // ==========================================================

    private void OnRedirect(RedirectMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] Redirect received → connecting to HUB {msg.ip}:{msg.port}");

        // Deconnect from master server and connect to hub instance
        if (NetworkClient.isConnected)
        {
            Debug.Log("[ClientAccountAPI] Disconnecting from master...");
            NetworkClient.Disconnect();
        }

        // Change network address and port
        NetworkManager manager = NetworkManager.singleton;
        KcpTransport kcp = manager.GetComponent<KcpTransport>();

        manager.networkAddress = msg.ip;
        kcp.Port = (ushort)msg.port;


        // connect to the hub instance
        Debug.Log("[ClientAccountAPI] Connecting to hub instance...");
        manager.StartClient();
    }
}
