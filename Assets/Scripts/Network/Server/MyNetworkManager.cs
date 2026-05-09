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
        Debug.Log($"[CLIENT] OnClientConnect activeScene={activeScene} ConnectingToHub={ClientAccountAPI.ConnectingToHub}");

        if (!ClientAccountAPI.ConnectingToHub)
        {
            Debug.Log("[CLIENT] Connected to MASTER, not spawning player.");
            return;
        }

        Debug.Log("[CLIENT] Connected to gameplay server, waiting for scene sync before AddPlayer.");
        pendingAddPlayerAfterSceneSync = true;

        if (!NetworkClient.ready)
        {
            Debug.Log("[CLIENT] Calling Ready on connect.");
            NetworkClient.Ready();
        }
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("[CLIENT] OnClientDisconnect");

        base.OnClientDisconnect();

        pendingAddPlayerAfterSceneSync = false;

        if (ServerTimeManager.instance)
            ServerTimeManager.instance.ResumeGame();
    }

    public override void OnClientError(TransportError error, string reason)
    {
        Debug.LogError($"[CLIENT] OnClientError: {error} reason={reason}");
        base.OnClientError(error, reason);
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        Debug.Log($"[CLIENT] OnClientChangeScene -> {newSceneName} op={sceneOperation} customHandling={customHandling}");
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        string activeScene = SceneManager.GetActiveScene().name;
        Debug.Log("[CLIENT] OnClientSceneChanged activeScene=" + activeScene);

        if (!pendingAddPlayerAfterSceneSync)
        {
            Debug.Log("[CLIENT] No pending player add after scene sync.");
            return;
        }

        if (activeScene == menuSceneName || activeScene == loadingSceneName)
        {
            Debug.Log($"[CLIENT] Still in non-gameplay scene ({activeScene}), waiting.");
            return;
        }

        if (!NetworkClient.ready)
        {
            Debug.Log("[CLIENT] Calling Ready after scene sync.");
            NetworkClient.Ready();
        }

        if (NetworkClient.localPlayer == null)
        {
            Debug.Log("[CLIENT] AddPlayer after scene sync.");
            NetworkClient.AddPlayer();
        }
        else
        {
            Debug.Log("[CLIENT] LocalPlayer already exists, skipping AddPlayer.");
        }

        pendingAddPlayerAfterSceneSync = false;
        ClientAccountAPI.ConnectingToHub = false;

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