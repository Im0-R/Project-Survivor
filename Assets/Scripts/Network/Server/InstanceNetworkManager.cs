#if UNITY_SERVER
using System.Collections;
using AuthMessages;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstanceNetworkManager : NetworkManager
{
    [Header("Managers")]
    [SerializeField] private GameObject serverTimeManagerPrefab;

    [Header("Scenes")]
    [SerializeField] private string mapInstanceSceneName = "MapInstance";

    [Header("Map")]
    [SerializeField] private MapGenerator mapGenerator;

    private bool managersSpawned;
    private bool gameplaySceneLoadingStarted;
    private bool mapReady;
    private bool mapGenerationStarted;
    private bool sceneReadyHandled;

    public override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log($"[InstanceNetworkManager] OnStartServer | active scene={SceneManager.GetActiveScene().name}");

        NetworkServer.RegisterHandler<HubAuthMessage>(OnHubAuthMessage);

        SpawnServerManagersOnce();
    }

    private void OnHubAuthMessage(NetworkConnectionToClient conn, HubAuthMessage msg)
    {
        conn.authenticationData = msg.username;
        Debug.Log($"[InstanceNetworkManager] Conn {conn.connectionId} authenticated as {msg.username}");
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

    [Server]
    public void HandleTargetSceneReadyManually()
    {
        if (sceneReadyHandled)
            return;

        Debug.Log("[InstanceNetworkManager] Handling already loaded target scene manually");
        HandleSceneReady(SceneManager.GetActiveScene().name);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        Debug.Log($"[InstanceNetworkManager] OnServerSceneChanged -> {sceneName}");
        Debug.Log($"[InstanceNetworkManager] Active scene after change = {SceneManager.GetActiveScene().name}");

        HandleSceneReady(sceneName);
    }

    [Server]
    private void HandleSceneReady(string sceneName)
    {
        if (sceneReadyHandled)
        {
            Debug.LogWarning($"[InstanceNetworkManager] Scene ready already handled, ignoring duplicate for {sceneName}");
            return;
        }

        sceneReadyHandled = true;

        if (sceneName != mapInstanceSceneName)
        {
            Debug.Log($"[InstanceNetworkManager] Scene {sceneName} is not {mapInstanceSceneName}, skipping map generation");
            mapReady = true;
            return;
        }

        StartCoroutine(GenerateMapWhenSceneReady());
    }

    private IEnumerator GenerateMapWhenSceneReady()
    {
        if (mapGenerationStarted)
            yield break;

        mapGenerationStarted = true;
        mapReady = false;

        yield return null;

        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();

        if (mapGenerator == null)
        {
            Debug.LogError("[InstanceNetworkManager] MapGenerator not found in MapInstance scene");
            mapReady = true;
            yield break;
        }

        Debug.Log("[InstanceNetworkManager] Starting map generation");

        yield return mapGenerator.Generate();

        mapReady = true;

        Debug.Log("[InstanceNetworkManager] Map is ready");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[InstanceNetworkManager] OnServerAddPlayer connId={conn.connectionId}");
        StartCoroutine(AddPlayerWhenMapReady(conn));
    }

    private IEnumerator AddPlayerWhenMapReady(NetworkConnectionToClient conn)
    {
        Debug.Log("[InstanceNetworkManager] Waiting for mapReady before spawning player");

        while (!mapReady)
            yield return null;

        Debug.Log("[InstanceNetworkManager] mapReady true, spawning player");

        if (conn == null)
        {
            Debug.LogWarning("[InstanceNetworkManager] Conn became null before map was ready");
            yield break;
        }

        Transform spawn = null;

        if (SceneManager.GetActiveScene().name == mapInstanceSceneName && mapGenerator != null)
            spawn = mapGenerator.GetPlayerSpawnPoint();

        Transform start = GetStartPosition();

        Vector3 spawnPosition = spawn != null ? spawn.position : start != null ? start.position : Vector3.zero;
        Quaternion spawnRotation = spawn != null ? spawn.rotation : start != null ? start.rotation : Quaternion.identity;

        GameObject player = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"[InstanceNetworkManager] Player spawned at {spawnPosition}");

        StartCoroutine(LoadPlayerWhenAuthReady(conn, player));
    }

    private IEnumerator LoadPlayerWhenAuthReady(NetworkConnectionToClient conn, GameObject player)
    {
        string username = null;

        float timeout = 5f;
        float timer = 0f;

        while (timer < timeout)
        {
            if (conn == null || player == null)
                yield break;

            if (conn.authenticationData is string user && !string.IsNullOrWhiteSpace(user))
            {
                username = user;
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError($"[InstanceNetworkManager] Cannot load save, username missing connId={conn.connectionId}");
            yield break;
        }

        PlayerInventory inv = player.GetComponent<PlayerInventory>();
        PlayerEquipment equip = player.GetComponent<PlayerEquipment>();

        if (inv == null || equip == null)
        {
            Debug.LogError("[InstanceNetworkManager] PlayerInventory or PlayerEquipment missing on player prefab");
            yield break;
        }

        DatabaseManager.LoadPlayerState(username, inv, equip);

        Debug.Log($"[InstanceNetworkManager] Loaded save for {username}");
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
            return;

        GameObject stm = Instantiate(serverTimeManagerPrefab);
        DontDestroyOnLoad(stm);
        NetworkServer.Spawn(stm);

        Debug.Log("[InstanceNetworkManager] Spawned ServerTimeManager");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[InstanceNetworkManager] OnServerDisconnect connId={conn.connectionId}");

        if (conn.identity != null)
        {
            GameObject player = conn.identity.gameObject;
            string username = conn.authenticationData as string;

            if (!string.IsNullOrWhiteSpace(username))
            {
                PlayerInventory inv = player.GetComponent<PlayerInventory>();
                PlayerEquipment equip = player.GetComponent<PlayerEquipment>();

                if (inv != null && equip != null)
                {
                    DatabaseManager.SavePlayerState(username, inv, equip);
                    Debug.Log($"[InstanceNetworkManager] Saved player state for {username}");
                }
            }
        }

        base.OnServerDisconnect(conn);
    }
}
#endif