using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class InstanceNetworkManager : NetworkManager
{
    [Scene] public string hubSceneName = "Town";
    [Header("Managers")]
    public GameObject serverTimeManagerPrefab;

    private bool managersSpawned;

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
        base.OnStartServer();

        Debug.Log("[SERVER] OnStartServer active scene: " + SceneManager.GetActiveScene().name);

        // Toujours spawn tes managers, une seule fois
        SpawnServerManagersOnce();

        // Si on n'est pas déjà dans Town, on change
        if (SceneManager.GetActiveScene().name != hubSceneName)
        {
            Debug.Log("[SERVER] Loading Town scene");
            ServerChangeScene(hubSceneName);
        }
        else
        {
            Debug.Log("[SERVER] Town scene already active");
        }
    }
    private void SpawnServerManagersOnce()
    {
        if (managersSpawned) return;
        managersSpawned = true;

        if (serverTimeManagerPrefab == null)
        {
            Debug.LogError("[SERVER] serverTimeManagerPrefab is NULL (assign it in inspector)");
            return;
        }

        // Si jamais il existe déjà (sécurité)
        if (FindFirstObjectByType<ServerTimeManager>() != null)
        {
            Debug.LogWarning("[SERVER] ServerTimeManager already exists, skipping spawn");
            return;
        }

        var stm = Instantiate(serverTimeManagerPrefab);
        NetworkServer.Spawn(stm);

        // Pour survivre aux changements de scène (Town etc.)
        DontDestroyOnLoad(stm);

        Debug.Log("[SERVER] Spawned ServerTimeManager");
    }
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        if (SceneManager.GetActiveScene().name != hubSceneName)
        {
            Debug.Log("[HUB] ServerChangeScene(Town) (first time only)");
            //ServerChangeScene(hubSceneName);
        }
    }



    private System.Collections.IEnumerator SendClientToTown(NetworkConnectionToClient conn)
    {
        yield return null;

        // Connexion not valid yet?
        if (conn == null || !conn.isAuthenticated && conn.connectionId == 0) { /* ignore */ }

        // Force scene change to Town
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
