using System.Diagnostics;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.IO;

public class InstanceManager : NetworkBehaviour
{
    public static InstanceManager Instance { get; private set; }

    private readonly Dictionary<int, InstanceInfo> activeInstances = new();
    private int nextInstanceId = 1;

    public const string ipAddress = "72.60.212.58"; // IP PUBLIQUE DU SERVEUR

    // Nouvel EXE utilisé par TOUTES les instances (Hub + Maps)
    private const string INSTANCE_EXECUTABLE = "/home/server/instance/InstanceServer.x86_64";

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
        // Hub Town créé au lancement
        CreateInitialTownInstance();
    }

    // ==========================================================
    // =============== CREATION DYNAMIC INSTANCES ===============
    // ==========================================================

    [Server]
    public void CreateInstance(NetworkConnectionToClient conn)
    {
        int id = nextInstanceId++;
        int port = 8000 + id; // changing port to avoid conflicts
        string scene = "Town";
        int seed = Random.Range(0, 999999);

        if (!File.Exists(INSTANCE_EXECUTABLE))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] ❌ Missing instance server build: {INSTANCE_EXECUTABLE}");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = INSTANCE_EXECUTABLE,
            Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        activeInstances[id] = new InstanceInfo(id, port, scene, seed);

        // Envoyer les infos de connexion au client
        TargetSendInstanceInfo(conn, ipAddress, port);

        UnityEngine.Debug.Log($"[InstanceManager] Dynamic instance #{id} launched on port {port}");
    }

    // ==========================================================
    // =============== CREATION INSTANCE HUB =====================
    // ==========================================================

    [Server]
    private void CreateInitialTownInstance()
    {
        int id = nextInstanceId++;
        int port = 8000;        // Port fixe du HUB
        string scene = "Town";
        int seed = Random.Range(0, 999999);

        if (!File.Exists(INSTANCE_EXECUTABLE))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] ❌ Missing instance server build: {INSTANCE_EXECUTABLE}");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = INSTANCE_EXECUTABLE,
            Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        activeInstances[id] = new InstanceInfo(id, port, scene, seed);

        UnityEngine.Debug.Log($"[Master] HUB Town instance created on port {port}");
    }

    // ==========================================================
    // =============== CLIENT REDIRECT ===========================
    // ==========================================================

    [TargetRpc]
    private void TargetSendInstanceInfo(NetworkConnectionToClient conn, string ip, int port)
    {
        ClientSideInstanceManager.Instance?.SwitchToInstance((ushort)port, ip);
    }

    // ==========================================================
    // =============== INSTANCE INFO STRUCT ======================
    // ==========================================================

    [System.Serializable]
    public class InstanceInfo
    {
        public int id;
        public int port;
        public string scene;
        public int seed;

        public InstanceInfo(int id, int port, string scene, int seed)
        {
            this.id = id;
            this.port = port;
            this.scene = scene;
            this.seed = seed;
        }
    }
}
