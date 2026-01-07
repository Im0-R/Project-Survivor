using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
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

        if (!ClientAccountAPI.ConnectingToHub)
        {
            Debug.Log("[CLIENT] Connected to MASTER, not spawning player.");
            return;
        }

        Debug.Log("[CLIENT] Connected to HUB -> Ready + AddPlayer");
        ClientAccountAPI.ConnectingToHub = false;

        if (!NetworkClient.ready)
            NetworkClient.Ready();

        if (NetworkClient.localPlayer == null)
            NetworkClient.AddPlayer();
        //Link the UI to the local player


        Debug.Log("[CLIENT] Starting coroutine to ensure UI is loaded.");
        GameUILoader.Instance.StartCoroutine(GameUILoader.Instance.EnsureUILoadedOnce());
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("[CLIENT] OnClientDisconnect");
        base.OnClientDisconnect();
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

        if (!NetworkClient.ready)
            NetworkClient.Ready();

        if (NetworkClient.localPlayer == null)
            NetworkClient.AddPlayer();
    }
    public void JoinInstance()
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[CLIENT] Cannot join instance: not connected to server.");
            return;
        }

        Debug.Log("[CLIENT] Joining instance: sending Ready + AddPlayer");

        if (!NetworkClient.ready)
            NetworkClient.Ready();

        if (NetworkClient.localPlayer == null)
            NetworkClient.AddPlayer();
    }
}
