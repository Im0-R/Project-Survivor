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
        // Lancer l'instance du HUB au démarrage du Master
        CreateInitialTownInstance();
    }

    // ==========================================================
    // =============== CREATION INSTANCE DYNAMIQUE ===============
    // ==========================================================

    [Server]
    public void CreateInstance(NetworkConnectionToClient conn)
    {
        int id = nextInstanceId++;
        int port = 7777 + id;
        string scene = "Town";
        int seed = Random.Range(0, 999999);

        string buildPath = "/home/server/ServerBuild.x86_64";
        if (!File.Exists(buildPath))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Missing server build: {buildPath}");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = buildPath,
            Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        activeInstances[id] = new InstanceInfo(id, port, scene, seed);

        // envoyer les infos de connexion au client
        TargetSendInstanceInfo(conn, ipAddress, port);
    }

    // ==========================================================
    // =============== CREATION INSTANCE HUB =====================
    // ==========================================================

    [Server]
    private void CreateInitialTownInstance()
    {
        int id = nextInstanceId++;
        int port = 8000;
        string scene = "Town";
        int seed = Random.Range(0, 999999);

        string buildPath = "/home/server/ServerBuild.x86_64";

        Process.Start(new ProcessStartInfo
        {
            FileName = buildPath,
            Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        activeInstances[id] = new InstanceInfo(id, port, scene, seed);

        UnityEngine.Debug.Log($"[Master] Town instance created on port {port}");
    }

    // ==========================================================
    // =============== SEND REDIRECT INFO TO CLIENT ==============
    // ==========================================================

    [TargetRpc]
    private void TargetSendInstanceInfo(NetworkConnectionToClient conn, string ip, int port)
    {
        ClientSideInstanceManager.Instance?.SwitchToInstance((ushort)port, ip);
    }

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
