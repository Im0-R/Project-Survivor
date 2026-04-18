#if UNITY_SERVER || UNITY_EDITOR
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using kcp2k;

public class ServerBootstrap : MonoBehaviour
{
    public static string SceneArg = "Town";
    public static int PortArg = 7777;
    public static int SeedArg = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if UNITY_CLIENT
        Debug.Log("UNITY_CLIENT = TRUE");
#else
        Debug.Log("UNITY_CLIENT = FALSE");
#endif

#if UNITY_SERVER
        Debug.Log("UNITY_SERVER = TRUE");

        if (Keyboard.current != null)
            InputSystem.DisableDevice(Keyboard.current);

        if (Mouse.current != null)
            InputSystem.DisableDevice(Mouse.current);
#endif

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

        Debug.Log($"[ServerBootstrap] ARGS scene={SceneArg} | port={PortArg} | seed={SeedArg}");
    }

    private IEnumerator Start()
    {
        Debug.Log($"[ServerBootstrap] Booting dedicated server... activeScene={SceneManager.GetActiveScene().name}");

        // 1) Trouver le NetworkManager dans la scène bootstrap (Server_Main)
        NetworkManager manager = null;
        while (manager == null)
        {
            manager = Object.FindFirstObjectByType<NetworkManager>();
            yield return null;
        }

        Debug.Log($"[ServerBootstrap] NetworkManager found: {manager.GetType().Name}");

        // 2) Configurer le port AVANT StartServer
        KcpTransport kcp = manager.transport as KcpTransport;
        if (kcp == null)
            kcp = Object.FindFirstObjectByType<KcpTransport>();

        if (kcp != null)
        {
            kcp.Port = (ushort)PortArg;
            Debug.Log($"[ServerBootstrap] KCP port set to {kcp.Port}");
        }
        else
        {
            Debug.LogError("[ServerBootstrap] KcpTransport NOT found!");
            yield break;
        }

        // 3) Init DB avant ou juste après le start, peu importe ici
        Debug.Log("[ServerBootstrap] DB init...");
        DatabaseManager.Initialize();

        // 4) Start server dans la scène bootstrap
        manager.StartServer();
        Debug.Log("[ServerBootstrap] StartServer() called");

        // 5) Si on a un InstanceNetworkManager, on laisse Mirror changer de scène
        if (manager is InstanceNetworkManager instanceManager)
        {
            if (!string.IsNullOrWhiteSpace(SceneArg) &&
                SceneManager.GetActiveScene().name != SceneArg)
            {
                Debug.Log($"[ServerBootstrap] Requesting ServerChangeScene -> {SceneArg}");
                instanceManager.LoadGameplayScene(SceneArg);
            }
            else
            {
                Debug.Log($"[ServerBootstrap] Already in requested scene: {SceneArg}");
            }
        }
        else
        {
            Debug.LogWarning("[ServerBootstrap] NetworkManager is not InstanceNetworkManager, no ServerChangeScene helper available");
        }

        while (true)
        {
            Debug.Log($"[ServerBootstrap] Server alive | activeScene={SceneManager.GetActiveScene().name} | requestedScene={SceneArg} | port={PortArg} | players={NetworkServer.connections.Count}");
            yield return new WaitForSeconds(10f);
        }
    }
}
#endif