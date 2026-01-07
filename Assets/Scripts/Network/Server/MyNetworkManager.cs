using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    public override void OnStartClient()
    {
        Debug.Log("[CLIENT] OnStartClient");
        base.OnStartClient();
    }

    public override void OnClientConnect()
    {
        Debug.Log("[CLIENT] OnClientConnect");
        base.OnClientConnect();

        if (!NetworkClient.ready)
            NetworkClient.Ready();

        if (NetworkClient.localPlayer == null)
            NetworkClient.AddPlayer();
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
        Debug.Log($"[CLIENT] OnClientChangeScene -> {newSceneName} | op={sceneOperation} | customHandling={customHandling}");
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }

    public override void OnClientSceneChanged()
    {
        Debug.Log($"[CLIENT] OnClientSceneChanged -> active scene = {SceneManager.GetActiveScene().name}");
        base.OnClientSceneChanged();
    }
}
