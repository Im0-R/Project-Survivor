#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using kcp2k;

public class ServerAutoStart : MonoBehaviour
{
    [SerializeField] string defaultScene = "Town";

    void Awake()
    {
#if !UNITY_SERVER
        return; // en Editor tu peux choisir de le laisser ou non
#endif
        // 1) Parse args
        int port = GetIntArg("-port", 7777);
        string scene = GetStringArg("-scene", defaultScene);

        // 2) Configure transport (KCP)
        var nm = NetworkManager.singleton;
        var kcp = Transport.active as KcpTransport;
        if (kcp != null)
        {
            kcp.Port = (ushort)port;
            Debug.Log($"[AUTO] KCP port set to {kcp.Port}");
        }
        else
        {
            Debug.LogWarning("[AUTO] Active transport is not KcpTransport, port not applied here.");
        }

        // 3) Start server if not already started
        if (!NetworkServer.active)
        {
            Debug.Log($"[AUTO] Starting server, scene={SceneManager.GetActiveScene().name}, targetScene={scene}");
            nm.StartServer();
        }

        // 4) Optional, ensure scene
        if (SceneManager.GetActiveScene().name != scene)
        {
            Debug.Log($"[AUTO] ServerChangeScene({scene})");
            nm.ServerChangeScene(scene);
        }
    }

    static int GetIntArg(string key, int fallback)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key && int.TryParse(args[i + 1], out var v))
                return v;
        return fallback;
    }

    static string GetStringArg(string key, string fallback)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key)
                return args[i + 1];
        return fallback;
    }
}
#endif
