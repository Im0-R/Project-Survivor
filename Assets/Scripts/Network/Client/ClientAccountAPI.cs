using AuthMessages;
using kcp2k;
using Mirror;
using UnityEngine;

public class ClientAccountAPI : MonoBehaviour
{
    public static ClientAccountAPI Instance { get; private set; }

    private bool handlersRegistered = false;

    // Redirect pending data
    private bool pendingRedirect = false;
    private string pendingIp;
    private ushort pendingPort;
    private string pendingToken; // optionnel si tu actives le token

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterHandlersOnce();
    }

    private void OnDestroy()
    {
        // Important: si jamais l'objet est détruit (ex: stop play), clean event
        if (handlersRegistered)
            NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
    }

    private void RegisterHandlersOnce()
    {
        if (handlersRegistered) return;
        handlersRegistered = true;

        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponse);
        NetworkClient.RegisterHandler<RedirectMessage>(OnRedirect);

        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
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
            Debug.Log("[ClientAccountAPI] Authentication successful — waiting for redirection...");
        else
            Debug.LogWarning($"[ClientAccountAPI] Login/Register failed: {msg.message}");
    }

    // ==========================================================
    // =============== REDIRECTION VERS HUB =====================
    // ==========================================================

    private void OnRedirect(RedirectMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] Redirect received → connecting to HUB {msg.ip}:{msg.port}");

        pendingRedirect = true;
        pendingIp = msg.ip;
        pendingPort = (ushort)msg.port;

        // Si encore connecté au master, on attend la vraie déconnexion
        if (NetworkClient.isConnected)
        {
            Debug.Log("[ClientAccountAPI] Disconnecting from master...");
            NetworkClient.Disconnect();
            return;
        }

        // Si déjà déconnecté, on connect tout de suite
        ConnectToHubNow();
    }

    private void OnClientDisconnected()
    {
        if (!pendingRedirect) return;

        Debug.Log("[ClientAccountAPI] Disconnected from master → applying redirect now...");
        ConnectToHubNow();
    }

    private void ConnectToHubNow()
    {
        pendingRedirect = false;

        NetworkManager manager = NetworkManager.singleton;
        KcpTransport kcp = manager.GetComponent<KcpTransport>();

        manager.networkAddress = pendingIp;
        kcp.Port = pendingPort;

        // Optionnel: stocker le token quelque part pour l’envoyer au hub après connexion
        // Exemple: HubAuthToken.Current = pendingToken;

        Debug.Log($"[ClientAccountAPI] Connecting to hub instance... ({pendingIp}:{pendingPort})");
        manager.StartClient();
    }
}
