using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    // On garde une intention de spawn après redirect/login
    private bool pendingAddPlayerAfterSceneSync;

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
        Debug.Log($"[CLIENT] OnClientConnect activeScene={SceneManager.GetActiveScene().name}");

        // Connexion initiale au MASTER :
        // on ne spawn PAS ici.
        if (!ClientAccountAPI.ConnectingToHub)
        {
            Debug.Log("[CLIENT] Connected to MASTER, not spawning player.");
            return;
        }

        // Après redirect vers hub/instance :
        // on attend la synchro de scène Mirror.
        Debug.Log("[CLIENT] Connected to HUB/INSTANCE, waiting for scene sync before AddPlayer.");
        pendingAddPlayerAfterSceneSync = true;
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("[CLIENT] OnClientDisconnect");
        base.OnClientDisconnect();

        pendingAddPlayerAfterSceneSync = false;

        if (ServerTimeManager.instance)
        {
            ServerTimeManager.instance.ResumeGame();
        }
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
        Debug.Log("[CLIENT] OnClientSceneChanged activeScene=" + SceneManager.GetActiveScene().name);

        // IMPORTANT :
        // sans Auto Create Player, c'est ici qu'on devient Ready
        // puis qu'on AddPlayer, uniquement si un redirect l'a demandé.
        if (!pendingAddPlayerAfterSceneSync)
        {
            Debug.Log("[CLIENT] No pending player add after scene sync.");
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

        Debug.Log("[CLIENT] JoinInstance requested, waiting for scene sync.");
        pendingAddPlayerAfterSceneSync = true;
    }
}