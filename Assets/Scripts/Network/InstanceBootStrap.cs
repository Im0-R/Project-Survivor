#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using kcp2k;

public class InstanceBootStrap : MonoBehaviour
{
    public static string SceneArg = "Town";
    public static int PortArg = 8000;
    public static int SeedArg = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if UNITY_CLIENT || UNITY_EDITOR
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

    private IEnumerator Start()
    {
        Debug.Log("[InstanceBootStrap] Booting dedicated server...");

        // 1) Load the asked scene FIRST (so we get the right NetworkManager + Transport)
        if (SceneManager.GetActiveScene().name != SceneArg)
        {
            Debug.Log($"[InstanceBootStrap] Loading scene: {SceneArg}");
            var asyncLoad = SceneManager.LoadSceneAsync(SceneArg);

            while (!asyncLoad.isDone)
                yield return null;
        }

        // 2) Wait for NetworkManager (the one in the target scene)
        NetworkManager manager = null;
        while (manager == null)
        {
            manager = FindObjectOfType<NetworkManager>();
            yield return null;
        }

        // 3) Configure port on the transport ACTUALLY used by NetworkManager
        var kcp = manager.transport as KcpTransport;
        Debug.Log($"[InstanceBootStrap] Active transport = {manager.transport?.GetType().Name}");

        if (kcp != null)
        {
            Debug.Log($"[InstanceBootStrap] KCP port before = {kcp.Port}");
            kcp.Port = (ushort)PortArg;
            Debug.Log($"[InstanceBootStrap] KCP port after  = {kcp.Port}");
        }
        else
        {
            Debug.LogError("[InstanceBootStrap] NetworkManager transport is NOT KcpTransport!");
        }

        Debug.Log("[InstanceBootStrap] NetworkManager found → Starting instance...");

        // 4) Start the server
        manager.StartServer();

        Debug.Log("[InstanceBootStrap] DB init...");
        DatabaseManager.Initialize();

        while (true)
        {
            Debug.Log($"[InstanceBootStrap] Instance alive | scene={SceneArg} | port={PortArg} | players={NetworkServer.connections.Count}");
            yield return new WaitForSeconds(10f);
        }
    }
}
#endif
