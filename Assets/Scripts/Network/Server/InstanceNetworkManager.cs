using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class InstanceNetworkManager : NetworkManager
{
    public override void Awake()
    {
#if UNITY_CLIENT && !UNITY_SERVER || UNITY_EDITOR
        // This NM is server-only. On clients we must DESTROY it (not disable),
        // otherwise it can mess with singleton / scene messages.
        Destroy(gameObject);
    return;
#endif

        base.Awake();

        // Optional: keep it across scenes on server if you want
        DontDestroyOnLoad(gameObject);

#if UNITY_SERVER
    var nms = FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    Debug.Log($"[SERVER] NetworkManagers found: {nms.Length}");
    foreach (var nm in nms)
        Debug.Log($"[SERVER] NM: {nm.GetType().Name}, active={nm.gameObject.activeInHierarchy}, scene={nm.gameObject.scene.name}");
#endif
    }

    public override void OnStartServer()
    {
        Debug.Log("[HUB] OnStartServer");
        base.OnStartServer();
        Debug.Log("[HUB] Active scene on start: " + SceneManager.GetActiveScene().name);
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

}
