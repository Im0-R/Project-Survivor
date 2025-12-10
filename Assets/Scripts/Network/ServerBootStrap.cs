#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using kcp2k;

public class ServerBootstrap : MonoBehaviour
{
    // Arguments lus dans la ligne de commande
    public static string SceneArg = "Server_Main";
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

        // désactivation clavier/souris en mode serveur
        if (Keyboard.current != null)
            InputSystem.DisableDevice(Keyboard.current);
        if (Mouse.current != null)
            InputSystem.DisableDevice(Mouse.current);
#endif

        // Lire les arguments passés par l’instance
        ReadCommandLineArgs();
    }

    // ======================================================
    // =========== PARSING DES ARGUMENTS SERVEUR ============
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
        Debug.Log("[ServerBootstrap] Booting dedicated server...");

        // 1) Configurer le port AVANT StartServer
        KcpTransport kcp = FindObjectOfType<KcpTransport>();
        if (kcp != null)
        {
            kcp.Port = (ushort)PortArg;
            Debug.Log($"[ServerBootstrap] KCP port set to {kcp.Port}");
        }
        else
            Debug.LogWarning("[ServerBootstrap] KcpTransport NOT found!");

        // 2) Charger la scène demandée si ce n’est pas déjà la bonne
        if (SceneManager.GetActiveScene().name != SceneArg)
        {
            Debug.Log($"[ServerBootstrap] Loading scene: {SceneArg}");
            var asyncLoad = SceneManager.LoadSceneAsync(SceneArg);

            while (!asyncLoad.isDone)
                yield return null;
        }

        // 3) Attendre NetworkManager
        NetworkManager manager = null;
        while (manager == null)
        {
            manager = FindObjectOfType<NetworkManager>();
            yield return null;
        }

        Debug.Log("[ServerBootstrap] NetworkManager found → Starting server...");

        // 4) Démarrer le serveur
        manager.StartServer();

        Debug.Log("[ServerBootstrap] DB init...");
        DatabaseManager.Initialize();

        // 5) Log vivant toutes les 10 sec
        while (true)
        {
            Debug.Log($"[ServerBootstrap] Server alive | scene={SceneArg} | port={PortArg} | players={NetworkServer.connections.Count}");
            yield return new WaitForSeconds(10f);
        }
    }
}
#endif
