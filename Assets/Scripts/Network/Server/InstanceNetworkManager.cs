using Mirror;
using UnityEngine;

public class InstanceNetworkManager : NetworkManager
{
    void Awake()
    {
#if !UNITY_SERVER
        //we deactivate the InstanceNetworkManager if not a server build
        gameObject.SetActive(false);
        Debug.Log("[HUB] InstanceNetworkManager deactivated (not a server build)");
        return;
#endif

        var nms = GameObject.FindObjectsOfType<NetworkManager>(true);
        Debug.Log($"[CLIENT] NetworkManagers found: {nms.Length}");
        foreach (var nm in nms)
            Debug.Log($"[CLIENT] NM: {nm.name}, active={nm.gameObject.activeInHierarchy}, scene={nm.gameObject.scene.name}");

    }

    public override void OnStartServer()
    {
        Debug.Log("[HUB] OnStartServer");
        base.OnStartServer();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[HUB] OnServerConnect connId={conn.connectionId}");
        base.OnServerConnect(conn);
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        Debug.Log($"[HUB] OnServerReady connId={conn.connectionId}");
        base.OnServerReady(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[HUB] OnServerAddPlayer connId={conn.connectionId}");
        base.OnServerAddPlayer(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[HUB] OnServerDisconnect connId={conn.connectionId}");
        base.OnServerDisconnect(conn);
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[CLIENT] Connected, sending Ready + AddPlayer");

        NetworkClient.Ready();

        if (NetworkClient.localPlayer == null)
            NetworkClient.AddPlayer();
    }

}
