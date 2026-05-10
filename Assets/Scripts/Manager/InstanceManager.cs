using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mirror;
using UnityEngine;

public class InstanceManager : NetworkBehaviour
{
    public static InstanceManager Instance { get; private set; }

    public const string ipAddress = "72.60.212.58";

    [Header("Executable")]
    [SerializeField]
    private string instanceExecutable =
        "/home/server/instance/current/InstanceServer.x86_64";

    private readonly Dictionary<int, InstanceInfo> activeInstances = new();

    private int nextInstanceId = 1;
    private int nextDynamicPort = 8001;

    [Header("Boot Settings")]
    [SerializeField] private float instanceBootDelay = 2.0f;
    [SerializeField] private string hubSceneName = "Town";
    [SerializeField] private int hubPort = 8000;

    [Header("Map Settings")]
    [SerializeField] private string defaultMapSceneName = "MapInstance";
    [SerializeField] private string defaultMapId = "forest_01";

    public string HubSceneName => hubSceneName;
    public int HubPort => hubPort;
    public string HubIp => ipAddress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [ServerCallback]
    private void Start()
    {
        UnityEngine.Debug.Log("[InstanceManager] Ready");
        UnityEngine.Debug.Log($"[InstanceManager] Executable = {instanceExecutable}");
    }

    [Server]
    public InstanceInfo CreateInstance(NetworkConnectionToClient conn, string scene, string mapId = "")
    {
        if (conn == null)
        {
            UnityEngine.Debug.LogError("[InstanceManager] CreateInstance called with null conn");
            return null;
        }

        if (string.IsNullOrWhiteSpace(scene))
            scene = defaultMapSceneName;

        if (string.IsNullOrWhiteSpace(mapId))
            mapId = defaultMapId;

        if (!File.Exists(instanceExecutable))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Missing executable: {instanceExecutable}");
            return null;
        }

        DatabaseManager.SavePlayerStateFromConnection(conn);

        int instanceId = nextInstanceId++;
        int port = GetNextFreeDynamicPort();
        int seed = Random.Range(0, 999999);

        string logFile = $"/home/server/instance/logs/instance_{port}.log";
        string workingDirectory = Path.GetDirectoryName(instanceExecutable);

        Process process;

        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = instanceExecutable,
                WorkingDirectory = workingDirectory,
                Arguments = $"-scene {scene} -mapId {mapId} -port {port} -seed {seed} -logFile {logFile}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Failed to start instance process: {ex}");
            return null;
        }

        if (process == null)
        {
            UnityEngine.Debug.LogError("[InstanceManager] Process.Start returned null");
            return null;
        }

        InstanceInfo info = new InstanceInfo(instanceId, port, scene, mapId, seed, process.Id);
        activeInstances[instanceId] = info;

        UnityEngine.Debug.Log($"[InstanceManager] Starting instance #{instanceId} on port {port}, pid={process.Id}, scene={scene}, mapId={mapId}, seed={seed}");
        UnityEngine.Debug.Log($"[InstanceManager] Log file: {logFile}");
        UnityEngine.Debug.Log($"[InstanceManager] WorkingDirectory: {workingDirectory}");

        return info;
    }

    [Server]
    private void CreateInitialHubInstance()
    {
        if (!File.Exists(instanceExecutable))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Missing executable: {instanceExecutable}");
            return;
        }

        int instanceId = nextInstanceId++;
        int seed = Random.Range(0, 999999);

        string logFile = $"/home/server/instance/logs/hub_{hubPort}.log";
        string workingDirectory = Path.GetDirectoryName(instanceExecutable);

        try
        {
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = instanceExecutable,
                WorkingDirectory = workingDirectory,
                Arguments = $"-scene {hubSceneName} -port {hubPort} -seed {seed} -logFile {logFile}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
            {
                UnityEngine.Debug.LogError("[InstanceManager] Hub process returned null");
                return;
            }

            activeInstances[instanceId] = new InstanceInfo(instanceId, hubPort, hubSceneName, "", seed, process.Id)
            {
                isReady = true
            };

            UnityEngine.Debug.Log($"[InstanceManager] HUB launched on {ipAddress}:{hubPort}, pid={process.Id}");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Failed to launch HUB: {ex}");
        }
    }

    [Server]
    private IEnumerator DelayedRedirectToInstance(NetworkConnectionToClient conn, InstanceInfo info)
    {
        yield return new WaitForSeconds(instanceBootDelay);

        info.isReady = true;

        if (conn == null)
        {
            UnityEngine.Debug.LogWarning("[InstanceManager] Conn became null before redirect");
            yield break;
        }

        UnityEngine.Debug.Log($"[InstanceManager] Redirecting conn={conn.connectionId} to {ipAddress}:{info.port} scene={info.scene}");
        TargetSendInstanceInfo(conn, ipAddress, info.port, info.scene);
    }

    [Server]
    private void SavePlayerBeforeRedirect(NetworkConnectionToClient conn)
    {
        DatabaseManager.SavePlayerStateFromConnection(conn);
    }

    private int GetNextFreeDynamicPort()
    {
        while (IsPortAlreadyTracked(nextDynamicPort))
            nextDynamicPort++;

        return nextDynamicPort++;
    }

    private bool IsPortAlreadyTracked(int port)
    {
        foreach (var kvp in activeInstances)
        {
            if (kvp.Value.port == port)
                return true;
        }

        return false;
    }

    [TargetRpc]
    private void TargetSendInstanceInfo(NetworkConnectionToClient conn, string ip, int port, string sceneName)
    {
        if (ClientSideInstanceManager.Instance == null)
        {
            UnityEngine.Debug.LogError("[InstanceManager] ClientSideInstanceManager.Instance is null");
            return;
        }

        ClientSideInstanceManager.Instance.SwitchToInstance((ushort)port, ip, sceneName);
    }

    [System.Serializable]
    public class InstanceInfo
    {
        public int id;
        public int port;
        public string scene;
        public string mapId;
        public int seed;
        public int processId;
        public bool isReady;

        public InstanceInfo(int id, int port, string scene, string mapId, int seed, int processId)
        {
            this.id = id;
            this.port = port;
            this.scene = scene;
            this.mapId = mapId;
            this.seed = seed;
            this.processId = processId;
            isReady = false;
        }
    }
}