#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using kcp2k;

public class InstanceBootStrap : MonoBehaviour
{
    // Arguments reads from the command line
    public static string SceneArg = "Town";
    public static int PortArg = 8000;
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

        // Deactivate keyboard/mouse in server mode
        if (Keyboard.current != null)
            InputSystem.DisableDevice(Keyboard.current);
        if (Mouse.current != null)
            InputSystem.DisableDevice(Mouse.current);
#endif

        // Read the arguments passed by the instance
        ReadCommandLineArgs();
    }

    // ======================================================
    // =========== SERVER'S ARGUMENTS PASSING F ============
    // ======================================================

    private void ReadCommandLineArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-scene":
                    SceneArg = args[i + 1];
                    break;

                case "-port":
                    int.TryParse(args[i + 1], out PortArg);
                    break;

                case "-seed":
                    int.TryParse(args[i + 1], out SeedArg);
                    break;
            }
        }

        Debug.Log($"[ARGS] scene={SceneArg} | port={PortArg} | seed={SeedArg}");
    }

    // ======================================================
    // ===================== START ==========================
    // ======================================================

    private IEnumerator Start()
    {
        Debug.Log("[InstanceBootStrap] Booting dedicated server...");

        // 1) Configure the port before starting the server
        KcpTransport kcp = FindObjectOfType<KcpTransport>();
        if (kcp != null)
        {
            kcp.Port = (ushort)PortArg;
            Debug.Log($"[InstanceBootStrap] KCP port set to {kcp.Port}");
        }
        else
            Debug.LogWarning("[InstanceBootStrap] KcpTransport NOT found!");

        // 2) Load the asked scene
        if (SceneManager.GetActiveScene().name != SceneArg)
        {
            Debug.Log($"[InstanceBootStrap] Loading scene: {SceneArg}");
            var asyncLoad = SceneManager.LoadSceneAsync(SceneArg);

            while (!asyncLoad.isDone)
                yield return null;
        }

        // 3) Wait for NetworkManager
        NetworkManager manager = null;
        while (manager == null)
        {
            manager = FindObjectOfType<NetworkManager>();
            yield return null;
        }

        Debug.Log("[InstanceBootStrap] NetworkManager found → Starting instance...");

        // 4) Start the server
        manager.StartServer();

        Debug.Log("[InstanceBootStrap] DB init...");
        DatabaseManager.Initialize();

        // 5) Log to check if server is alive every 10 seconds
        while (true)
        {
            Debug.Log($"[InstanceBootStrap] Instance alive | scene={SceneArg} | port={PortArg} | players={NetworkServer.connections.Count}");
            yield return new WaitForSeconds(10f);
        }
    }
}
#endif
