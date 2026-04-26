using AuthMessages;
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
    private void OnHubAuthMessage(NetworkConnectionToClient conn, HubAuthMessage msg)
    {
        conn.authenticationData = msg.username;

        Debug.Log($"[InstanceNetworkManager] Conn {conn.connectionId} authenticated as {msg.username}");
    }
    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log($"[InstanceNetworkManager] OnStartServer | active scene={SceneManager.GetActiveScene().name}");

        NetworkServer.RegisterHandler<HubAuthMessage>(OnHubAuthMessage);

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
        Debug.Log($"[InstanceNetworkManager] Active scene after change = {SceneManager.GetActiveScene().name}");
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

        GameObject player = conn.identity != null ? conn.identity.gameObject : null;
        if (player == null)
        {
            Debug.LogError("[InstanceNetworkManager] Player identity is null after AddPlayer");
            return;
        }

        string username = conn.authenticationData as string;

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[InstanceNetworkManager] Cannot load save, username is missing in authenticationData");
            return;
        }

        PlayerInventory inv = player.GetComponent<PlayerInventory>();
        PlayerEquipment equip = player.GetComponent<PlayerEquipment>();

        if (inv == null || equip == null)
        {
            Debug.LogError("[InstanceNetworkManager] PlayerInventory or PlayerEquipment missing on player prefab");
            return;
        }

        DatabaseManager.LoadPlayerState(username, inv, equip);

        Debug.Log($"[InstanceNetworkManager] Loaded save for {username}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[InstanceNetworkManager] OnServerDisconnect connId={conn.connectionId}");

        if (conn.identity != null)
        {
            GameObject player = conn.identity.gameObject;
            
            string username = conn.authenticationData as string;

            if (!string.IsNullOrEmpty(username))
            {
                PlayerInventory inv = player.GetComponent<PlayerInventory>();
                PlayerEquipment equip = player.GetComponent<PlayerEquipment>();

                if (inv != null && equip != null)
                {
                    DatabaseManager.SavePlayerState(username, inv, equip);
                    Debug.Log($"[InstanceNetworkManager] Saved player state for {username}");
                }
            }
            else
            {
                Debug.LogWarning("[InstanceNetworkManager] Cannot save, username is missing in authenticationData");
            }
        }

        base.OnServerDisconnect(conn);
    }
}