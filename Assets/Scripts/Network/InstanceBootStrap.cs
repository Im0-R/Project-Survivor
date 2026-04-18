#if UNITY_SERVER
using System.Collections;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class InstanceBootStrap : MonoBehaviour
{
    public static string SceneArg = "Town";
    public static int PortArg = 8000;
    public static int SeedArg = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (Keyboard.current != null)
            InputSystem.DisableDevice(Keyboard.current);

        if (Mouse.current != null)
            InputSystem.DisableDevice(Mouse.current);

        ReadCommandLineArgs();
    }

    private void ReadCommandLineArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-scene":
                    if (i + 1 < args.Length)
                        SceneArg = args[i + 1];
                    break;

                case "-port":
                    if (i + 1 < args.Length)
                        int.TryParse(args[i + 1], out PortArg);
                    break;

                case "-seed":
                    if (i + 1 < args.Length)
                        int.TryParse(args[i + 1], out SeedArg);
                    break;
            }
        }

        Debug.Log($"[InstanceBootStrap] ARGS scene={SceneArg} | port={PortArg} | seed={SeedArg}");
    }

    private IEnumerator Start()
    {
        Debug.Log("[InstanceBootStrap] Booting dedicated instance...");

        NetworkManager manager = null;
        while (manager == null)
        {
            manager = FindFirstObjectByType<NetworkManager>();
            yield return null;
        }

        Debug.Log($"[InstanceBootStrap] Found NetworkManager: {manager.GetType().Name}");

        KcpTransport kcp = manager.transport as KcpTransport;
        if (kcp == null)
        {
            Debug.LogError("[InstanceBootStrap] Transport is not KcpTransport");
            yield break;
        }

        kcp.Port = (ushort)PortArg;
        Debug.Log($"[InstanceBootStrap] KCP configured on port {kcp.Port}");

        DatabaseManager.Initialize();
        Debug.Log("[InstanceBootStrap] Database initialized");

        manager.StartServer();
        Debug.Log("[InstanceBootStrap] Server started");

        // Laisse Mirror faire la synchro de scène correctement
        if (manager is InstanceNetworkManager instanceManager)
        {
            instanceManager.LoadGameplayScene(SceneArg);
        }
        else
        {
            Debug.LogError("[InstanceBootStrap] NetworkManager is not InstanceNetworkManager");
        }

        while (true)
        {
            Debug.Log($"[InstanceBootStrap] Alive | targetScene={SceneArg} | port={PortArg} | players={NetworkServer.connections.Count}");
            yield return new WaitForSeconds(10f);
        }
    }
}
#endif