using AuthMessages;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    private bool pendingAddPlayerAfterSceneSync;

    [Header("Client Scene Names")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string loadingSceneName = "Loading";

    public override void Awake()
    {
        Debug.Log("[CLIENT] MyNetworkManager Awake scene=" + gameObject.scene.name);
        base.Awake();
    }

    public override void OnStartClient()
    {
        Debug.Log("[CLIENT] OnStartClient");
        base.OnStartClient();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        string activeScene = SceneManager.GetActiveScene().name;

        Debug.Log(
            $"[CLIENT] OnClientConnect activeScene={activeScene} " +
            $"ConnectingToHub={ClientAccountAPI.ConnectingToHub}"
        );

        SendAuthToServer();

        pendingAddPlayerAfterSceneSync = true;

        if (!NetworkClient.ready)
        {
            Debug.Log("[CLIENT] Calling Ready on connect.");
            NetworkClient.Ready();
        }

        TryAddPlayerIfSceneIsReady();
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("[CLIENT] OnClientDisconnect");

        pendingAddPlayerAfterSceneSync = false;

        if (ServerTimeManager.instance)
            ServerTimeManager.instance.ResumeGame();

        base.OnClientDisconnect();
    }

    public override void OnClientError(TransportError error, string reason)
    {
        Debug.LogError($"[CLIENT] OnClientError: {error} reason={reason}");
        base.OnClientError(error, reason);
    }

    public override void OnClientChangeScene(
        string newSceneName,
        SceneOperation sceneOperation,
        bool customHandling
    )
    {
        Debug.Log(
            $"[CLIENT] OnClientChangeScene -> {newSceneName} " +
            $"op={sceneOperation} customHandling={customHandling}"
        );

        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        string activeScene = SceneManager.GetActiveScene().name;

        Debug.Log("[CLIENT] OnClientSceneChanged activeScene=" + activeScene);

        TryAddPlayerIfSceneIsReady();
    }

    private void SendAuthToServer()
    {
        if (string.IsNullOrEmpty(ClientAccountAPI.CurrentUsername))
        {
            Debug.LogWarning("[CLIENT] CurrentUsername is empty, auth not sent.");
            return;
        }

        NetworkClient.Send(new HubAuthMessage
        {
            username = ClientAccountAPI.CurrentUsername
        });

        Debug.Log($"[CLIENT] Sent HubAuthMessage as {ClientAccountAPI.CurrentUsername}");
    }

    private void TryAddPlayerIfSceneIsReady()
    {
        if (!pendingAddPlayerAfterSceneSync)
        {
            Debug.Log("[CLIENT] No pending player add after scene sync.");
            return;
        }

        string activeScene = SceneManager.GetActiveScene().name;

        if (activeScene == menuSceneName || activeScene == loadingSceneName)
        {
            Debug.Log($"[CLIENT] Still in non-gameplay scene ({activeScene}), waiting.");
            return;
        }

        if (!NetworkClient.ready)
        {
            Debug.Log("[CLIENT] Calling Ready before AddPlayer.");
            NetworkClient.Ready();
        }

        if (NetworkClient.localPlayer == null)
        {
            Debug.Log("[CLIENT] AddPlayer.");
            NetworkClient.AddPlayer();
        }
        else
        {
            Debug.Log("[CLIENT] LocalPlayer already exists, skipping AddPlayer.");
        }

        pendingAddPlayerAfterSceneSync = false;
        ClientAccountAPI.ConnectingToHub = false;

        LoadGameUI();
    }

    private void LoadGameUI()
    {
        if (GameUILoader.Instance != null)
        {
            Debug.Log("[CLIENT] Starting coroutine to ensure UI is loaded.");
            GameUILoader.Instance.StartCoroutine(GameUILoader.Instance.EnsureUILoadedOnce());
        }
        else
        {
            Debug.LogWarning("[CLIENT] GameUILoader.Instance is null.");
        }
    }

    public void JoinInstance()
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[CLIENT] Cannot join instance: not connected to server.");
            return;
        }

        Debug.Log("[CLIENT] JoinInstance requested.");

        ClientAccountAPI.ConnectingToHub = true;
        pendingAddPlayerAfterSceneSync = true;
    }
}