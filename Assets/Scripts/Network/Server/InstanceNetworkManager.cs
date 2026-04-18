using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstanceNetworkManager : NetworkManager
{
    [Header("Managers")]
    [SerializeField] private GameObject serverTimeManagerPrefab;

    private bool managersSpawned;

    public override void Awake()
    {
#if UNITY_CLIENT && !UNITY_SERVER || UNITY_EDITOR
        Destroy(gameObject);
        return;
#endif

        base.Awake();
        DontDestroyOnLoad(gameObject);

#if UNITY_SERVER
        var nms = FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[InstanceNetworkManager] NetworkManagers found: {nms.Length}");

        foreach (var nm in nms)
        {
            Debug.Log($"[InstanceNetworkManager] NM={nm.GetType().Name} | active={nm.gameObject.activeInHierarchy} | scene={nm.gameObject.scene.name}");
        }
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log($"[InstanceNetworkManager] OnStartServer | active scene={SceneManager.GetActiveScene().name}");
        SpawnServerManagersOnce();
    }

    private void SpawnServerManagersOnce()
    {
        if (managersSpawned) return;
        managersSpawned = true;

        if (serverTimeManagerPrefab == null)
        {
            Debug.LogError("[InstanceNetworkManager] serverTimeManagerPrefab is null");
            return;
        }

        if (FindFirstObjectByType<ServerTimeManager>() != null)
        {
            Debug.LogWarning("[InstanceNetworkManager] ServerTimeManager already exists, skipping");
            return;
        }

        GameObject stm = Instantiate(serverTimeManagerPrefab);
        NetworkServer.Spawn(stm);
        DontDestroyOnLoad(stm);

        Debug.Log("[InstanceNetworkManager] Spawned ServerTimeManager");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"[InstanceNetworkManager] OnServerConnect connId={conn.connectionId}");
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        Debug.Log($"[InstanceNetworkManager] OnServerReady connId={conn.connectionId}");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[InstanceNetworkManager] OnServerAddPlayer connId={conn.connectionId}");
        base.OnServerAddPlayer(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[InstanceNetworkManager] OnServerDisconnect connId={conn.connectionId}");
        base.OnServerDisconnect(conn);
    }
}