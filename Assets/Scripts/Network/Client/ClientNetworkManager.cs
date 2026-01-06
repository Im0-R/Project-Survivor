using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class ClientNetworkManager : NetworkManager
{
    public override void OnClientConnect()
    {
        Debug.Log("[CLIENT] OnClientConnect");
        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("[CLIENT] OnClientDisconnect");
        base.OnClientDisconnect();
    }

    public override void OnClientChangeScene(
        string newSceneName,
        SceneOperation sceneOperation,
        bool customHandling
    )
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
