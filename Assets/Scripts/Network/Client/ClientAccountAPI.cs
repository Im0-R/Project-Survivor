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

    private void RegisterHandlersOnce()
    {
        if (handlersRegistered) return;
        handlersRegistered = true;

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
            Debug.Log("[ClientAccountAPI] Authentication successful — waiting for redirection...");
        else
            Debug.LogWarning($"[ClientAccountAPI] Login/Register failed: {msg.message}");
    }

    // ==========================================================
    // =============== HUB MANAGEMENT =====================
    // ==========================================================

    private void OnRedirect(RedirectMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] Redirect received → {msg.ip}:{msg.port}");
        StartCoroutine(RedirectRoutine(msg.ip, (ushort)msg.port));
    }

    private System.Collections.IEnumerator RedirectRoutine(string ip, ushort port)
    {
        var manager = NetworkManager.singleton;
        var kcp = manager.GetComponent<KcpTransport>();

        // stop master connection cleanly
        if (NetworkClient.isConnected)
        {
            Debug.Log("[ClientAccountAPI] StopClient (master) ...");
            manager.StopClient();
        }

        // wait one frame so Mirror disposes transport properly
        yield return null;

        manager.networkAddress = ip;
        kcp.Port = port;

        Debug.Log($"[ClientAccountAPI] StartClient (hub) {ip}:{port}");
        manager.StartClient();
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
