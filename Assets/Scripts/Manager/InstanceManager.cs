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
    public const string ipAddress = "72.60.212.58";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    [Server]
    public void CreateInstance(NetworkConnectionToClient conn)
    {
        // 🔥 Désactivé temporairement pour éviter le lancement d'instances secondaires
        UnityEngine.Debug.LogWarning("[InstanceManager] CreateInstance is DISABLED for testing.");
        return;

        // ↓↓↓ Le code original reste ici mais ne sera jamais exécuté ↓↓↓
        /*
        int id = nextInstanceId++;
        int port = 7777 + id;
        string scene = "MapScene";
        int seed = Random.Range(0, 999999);

        string buildPath = "/home/server/ServerBuild.x86_64";
        if (!File.Exists(buildPath))
        {
            UnityEngine.Debug.LogError($"[InstanceManager] Server build introuvable: {buildPath}");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = buildPath,
            Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        activeInstances[id] = new InstanceInfo
        {
            id = id,
            port = port,
            scene = scene,
            seed = seed
        };

        TargetSendInstanceInfo(conn, "127.0.0.1", port);
        */
    }

    [TargetRpc]
    private void TargetSendInstanceInfo(NetworkConnectionToClient conn, string ip, int port)
    {
        if (ClientSideInstanceManager.Instance != null)
            ClientSideInstanceManager.Instance.SwitchToInstance((ushort)port, ip);
        else
            UnityEngine.Debug.LogWarning("ClientSideInstanceManager instance not found!");
    }
}

[System.Serializable]
public class InstanceInfo
{
    public int id;
    public int port;
    public string scene;
    public int seed;
}
