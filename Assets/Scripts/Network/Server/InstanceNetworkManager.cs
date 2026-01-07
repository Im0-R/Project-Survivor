using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class InstanceNetworkManager : NetworkManager
{
    [Scene] public string hubSceneName = "Town";
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
        base.OnServerConnect(conn);

        // Forcer le client à charger Town si il n'est pas déjà dessus.
        // On attend un frame, ça évite les cas où le client n'est pas prêt à recevoir.
        StartCoroutine(SendClientToTown(conn));
    }

    private System.Collections.IEnumerator SendClientToTown(NetworkConnectionToClient conn)
    {
        yield return null;

        // Connexion encore valide ?
        if (conn == null || !conn.isAuthenticated && conn.connectionId == 0) { /* ignore */ }

        // Forcer Mirror à envoyer le changement de scène
        Debug.Log("[HUB] Forcing ServerChangeScene(Town) for new connection");
        ServerChangeScene(hubSceneName);
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
