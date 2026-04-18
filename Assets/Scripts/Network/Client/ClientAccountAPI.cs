using System.Collections;
using AuthMessages;
using Mirror;
using UnityEngine;

public class ClientAccountAPI : MonoBehaviour
{
    public static ClientAccountAPI Instance { get; private set; }

    private bool handlersRegistered = false;
    public static bool ConnectingToHub;

    private bool redirectPending;
    private string pendingIp;
    private int pendingPort;

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

        pendingIp = msg.ip;
        pendingPort = msg.port;
        redirectPending = true;

        StartCoroutine(HandleRedirectNextFrame());
    }

    private IEnumerator HandleRedirectNextFrame()
    {
        yield return null;

        if (!redirectPending)
            yield break;

        redirectPending = false;
        ConnectingToHub = true;

        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[ClientAccountAPI] ClientSideInstanceManager.Instance is null");
            yield break;
        }

        ClientSideInstanceManager.Instance.SwitchToInstance((ushort)pendingPort, pendingIp, "Town");
    }
}