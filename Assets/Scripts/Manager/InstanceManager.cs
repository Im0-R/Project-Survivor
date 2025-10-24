using System.Diagnostics;
using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class InstanceManager : NetworkBehaviour
{
    public static InstanceManager Instance { get; private set; }

    private readonly Dictionary<int, InstanceInfo> activeInstances = new();
    private int nextInstanceId = 1;

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
        int id = nextInstanceId++;
        int port = 7777 + id;
        string scene = "MapScene";
        int seed = Random.Range(0, 999999);

        // Start the server process 
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "MyServerBuild.exe",
            Arguments = $"-batchmode -nographics -scene {scene} -port {port} -seed {seed}",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        Process.Start(psi);

        activeInstances[id] = new InstanceInfo
        {
            id = id,
            port = port,
            scene = scene,
            seed = seed
        };

        // Sending instance info to the client
        TargetSendInstanceInfo(conn, "127.0.0.1", port);
    }

    [TargetRpc]
    private void TargetSendInstanceInfo(NetworkConnectionToClient conn, string ip, int port)
    {
        // Call the client-side instance manager to switch
        if (ClientSideInstanceManager.Instance != null)
            ClientSideInstanceManager.Instance.SwitchToInstance(ip, (ushort)port);
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
