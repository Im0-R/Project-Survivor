using System.Collections;
using AuthMessages;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstanceNetworkManager : NetworkManager
{
    [Header("Managers")]
    [SerializeField] private GameObject serverTimeManagerPrefab;

    [Header("Instance State")]
    [SerializeField] private GameObject instanceStatePrefab;

    [Header("Scenes")]
    [SerializeField] private string mapInstanceSceneName = "MapInstance";

    [Header("Map")]
    [SerializeField] private MapGenerator mapGenerator;

    private InstanceState instanceState;

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

        SpawnInstanceStateOnce();

        StartCoroutine(GenerateMapWhenSceneReady());
    }

    [Server]
    private void SpawnInstanceStateOnce()
    {
        if (instanceState != null)
            return;

        string mapId = GetServerMapId();
        int seed = GetServerSeed();

        if (string.IsNullOrWhiteSpace(mapId))
        {
            Debug.LogError("[InstanceNetworkManager] Cannot spawn InstanceState, mapId is empty");
            return;
        }

        if (instanceStatePrefab == null)
        {
            Debug.LogError("[InstanceNetworkManager] instanceStatePrefab is null");
            return;
        }

        GameObject obj = Instantiate(instanceStatePrefab);
        DontDestroyOnLoad(obj);

        instanceState = obj.GetComponent<InstanceState>();

        if (instanceState == null)
        {
            Debug.LogError("[InstanceNetworkManager] InstanceState missing on prefab");
            Destroy(obj);
            return;
        }

        SpawnDebug.LogSpawn(obj, "InstanceState");
        NetworkServer.Spawn(obj);

        int difficulty = GetServerDifficulty();

        instanceState.SetMap(mapId, seed, difficulty);
        Debug.Log($"[InstanceNetworkManager] Spawned InstanceState | mapId={mapId} | seed={seed} | difficulty={difficulty}");
    }

    private int GetServerDifficulty()
    {
#if UNITY_SERVER
        return InstanceBootStrap.DifficultyArg;
#else
        return 1;
#endif
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

        string mapId = GetServerMapId();
        int seed = GetServerSeed();

        if (string.IsNullOrWhiteSpace(mapId))
        {
            Debug.LogError("[InstanceNetworkManager] Cannot generate map, mapId is empty");
            mapReady = true;
            yield break;
        }

        Debug.Log($"[InstanceNetworkManager] Starting map generation | mapId={mapId} | seed={seed}");

        yield return mapGenerator.Generate(mapId, seed);

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

        NetworkEntity entity = player.GetComponent<NetworkEntity>();

        if (entity != null)
        {
            bool isTown = SceneManager.GetActiveScene().name == "Town";

            if (isTown)
                entity.DisableSpells();
            else
                entity.EnableSpells();
        }

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
        PlayerStash stash = player.GetComponent<PlayerStash>();
        PlayerArcanaLoadout arcanaLoadout = player.GetComponent<PlayerArcanaLoadout>();

        if (inv == null || equip == null || stash == null || arcanaLoadout == null)
        {
            Debug.LogError("[InstanceNetworkManager] Missing PlayerInventory, PlayerEquipment, PlayerStash or PlayerArcanaLoadout on player prefab");
            yield break;
        }

        DatabaseManager.LoadPlayerState(username, inv, equip, stash, arcanaLoadout);
        PlayerEntity playerEntity = player.GetComponent<PlayerEntity>();

        if (playerEntity != null)
        {
            ushort currentPort = 7777;

            kcp2k.KcpTransport kcp = Transport.active as kcp2k.KcpTransport;

            if (kcp != null)
                currentPort = kcp.port;

            PartyManager.UpdateLocationFor(playerEntity, currentPort);
        }
        Debug.Log($"[InstanceNetworkManager] Loaded save + stash + arcana loadout for {username}");
    }

    private void SpawnServerManagersOnce()
    {
        if (managersSpawned)
            return;

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
        SpawnDebug.LogSpawn(stm, "ServerTimeManager");
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
                PlayerStash stash = player.GetComponent<PlayerStash>();
                PlayerArcanaLoadout arcanaLoadout = player.GetComponent<PlayerArcanaLoadout>();

                if (inv != null && equip != null && stash != null && arcanaLoadout != null)
                {
                    DatabaseManager.SavePlayerState(username, inv, equip, stash, arcanaLoadout);
                    Debug.Log($"[InstanceNetworkManager] Saved player state + stash + arcana loadout for {username}");
                }
                else
                {
                    Debug.LogError("[InstanceNetworkManager] Cannot save, PlayerInventory, PlayerEquipment, PlayerStash or PlayerArcanaLoadout missing");
                }
            }
            else
            {
                Debug.LogError("[InstanceNetworkManager] Cannot save, username missing from authenticationData");
            }
        }

        base.OnServerDisconnect(conn);
    }

    private string GetServerMapId()
    {
#if UNITY_SERVER
        return InstanceBootStrap.MapIdArg;
#else
        return "";
#endif
    }

    private int GetServerSeed()
    {
#if UNITY_SERVER
        return InstanceBootStrap.SeedArg;
#else
        return 0;
#endif
    }
}