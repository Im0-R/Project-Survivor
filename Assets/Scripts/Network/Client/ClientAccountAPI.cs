using AuthMessages;
using Mirror;
using UnityEngine;

public class ClientAccountAPI : MonoBehaviour
{
    public static ClientAccountAPI Instance { get; private set; }

    private bool handlersRegistered = false;
    public static bool ConnectingToHub;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

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

    public void TryRegister(string username, string password)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        Debug.Log($"[ClientAccountAPI] Sending RegisterMessage for {username}");
        NetworkClient.Send(new RegisterMessage
        {
            username = username,
            password = password
        });
    }

    public void TryLogin(string username, string password)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        Debug.Log($"[ClientAccountAPI] Sending LoginMessage for {username}");
        NetworkClient.Send(new LoginMessage
        {
            username = username,
            password = password
        });
    }

    private void OnAuthResponse(AuthResponseMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] AuthResponse success={msg.success}, message={msg.message}");

        if (msg.success)
            Debug.Log("[ClientAccountAPI] Authentication successful, waiting for redirection...");
        else
            Debug.LogWarning($"[ClientAccountAPI] Login/Register failed: {msg.message}");
    }

    private void OnRedirect(RedirectMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] Redirect received -> {msg.ip}:{msg.port}");

        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[ClientAccountAPI] ClientSideInstanceManager.Instance is null");
            return;
        }

        ConnectingToHub = true;

        // Pour l’instant on force Town comme scène de hub
        ClientSideInstanceManager.Instance.SwitchToInstance((ushort)msg.port, msg.ip, "Town");
    }
}