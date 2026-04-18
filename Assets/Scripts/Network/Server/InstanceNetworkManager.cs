using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstanceNetworkManager : NetworkManager
{
    [Header("Managers")]
    [SerializeField] private GameObject serverTimeManagerPrefab;

    private bool managersSpawned;
    private bool gameplaySceneLoadingStarted;

    public override void Awake()
    {
#if !UNITY_SERVER
        Destroy(gameObject);
        return;
#endif

        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log($"[InstanceNetworkManager] OnStartServer | active scene={SceneManager.GetActiveScene().name}");
        SpawnServerManagersOnce();
    }

    [Server]
    public void LoadGameplayScene(string sceneName)
    {
        if (gameplaySceneLoadingStarted)
        {
            Debug.LogWarning("[InstanceNetworkManager] Gameplay scene load already started");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[InstanceNetworkManager] LoadGameplayScene received null/empty scene");
            return;
        }

        gameplaySceneLoadingStarted = true;

        Debug.Log($"[InstanceNetworkManager] ServerChangeScene -> {sceneName}");
        ServerChangeScene(sceneName);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        Debug.Log($"[InstanceNetworkManager] OnServerSceneChanged -> {sceneName}");
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
        DontDestroyOnLoad(stm);
        NetworkServer.Spawn(stm);

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