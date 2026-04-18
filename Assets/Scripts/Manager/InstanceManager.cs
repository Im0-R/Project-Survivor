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
    private const string INSTANCE_EXECUTABLE = "/home/server/instance/InstanceServer.x86_64";

    private readonly Dictionary<int, InstanceInfo> activeInstances = new();

    private int nextInstanceId = 1;
    private int nextDynamicPort = 8001;

    [Header("Boot Settings")]
    [SerializeField] private float instanceBootDelay = 2.0f;
    [SerializeField] private string hubSceneName = "Town";
    [SerializeField] private int hubPort = 8000;

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
        CreateInitialHubInstance();
    }

    [Server]
    public void CreateInstance(NetworkConnectionToClient conn, string scene)
    {
        if (conn == null)
        {
            UnityEngine.Debug.LogError("[InstanceManager] CreateInstance called with null conn");
            return;
        }

        if (string.IsNullOrWhiteSpace(scene))
            scene = hubSceneName;

        if (!File.Exists(INSTANCE_EXECUTABLE))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Missing executable: {INSTANCE_EXECUTABLE}");
            return;
        }

        int instanceId = nextInstanceId++;
        int port = GetNextFreeDynamicPort();
        int seed = Random.Range(0, 999999);

        Process process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = INSTANCE_EXECUTABLE,
                Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Failed to start instance process: {ex}");
            return;
        }

        if (process == null)
        {
            UnityEngine.Debug.LogError("[InstanceManager] Process.Start returned null");
            return;
        }

        var info = new InstanceInfo(instanceId, port, scene, seed, process.Id);
        activeInstances[instanceId] = info;

        UnityEngine.Debug.Log($"[InstanceManager] Starting instance #{instanceId} on port {port}, pid={process.Id}, scene={scene}");

        StartCoroutine(DelayedRedirectToInstance(conn, info));
    }

    [Server]
    private void CreateInitialHubInstance()
    {
        if (!File.Exists(INSTANCE_EXECUTABLE))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Missing executable: {INSTANCE_EXECUTABLE}");
            return;
        }

        int instanceId = nextInstanceId++;
        int seed = Random.Range(0, 999999);

        try
        {
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = INSTANCE_EXECUTABLE,
                Arguments = $"-batchmode -nographics -scene {hubSceneName} -port {hubPort} -seed {seed}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
            {
                UnityEngine.Debug.LogError("[InstanceManager] Hub process returned null");
                return;
            }

            activeInstances[instanceId] = new InstanceInfo(instanceId, hubPort, hubSceneName, seed, process.Id)
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

        if (conn != null)
        {
            UnityEngine.Debug.Log($"[InstanceManager] Redirecting conn={conn.connectionId} to {ipAddress}:{info.port} scene={info.scene}");
            TargetSendInstanceInfo(conn, ipAddress, info.port, info.scene);
        }
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
        ClientSideInstanceManager.Instance?.SwitchToInstance((ushort)port, ip, sceneName);
    }

    [System.Serializable]
    public class InstanceInfo
    {
        public int id;
        public int port;
        public string scene;
        public int seed;
        public int processId;
        public bool isReady;

        public InstanceInfo(int id, int port, string scene, int seed, int processId)
        {
            this.id = id;
            this.port = port;
            this.scene = scene;
            this.seed = seed;
            this.processId = processId;
            isReady = false;
        }
    }
}