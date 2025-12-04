#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ServerBootstrap : MonoBehaviour
{
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
#endif

#if ENABLE_HEADLESS_MODE
    Debug.Log("HEADLESS MODE = TRUE");
#endif
        // Deactivate keyboard and mouse input on server to avoid automatic shutdown
        InputSystem.DisableDevice(Keyboard.current);
        InputSystem.DisableDevice(Mouse.current);
    }

    private IEnumerator Start()
    {
        Debug.Log("[ServerBootstrap] Booting dedicated server...");

        yield return new WaitForSeconds(1f);

        if (!NetworkServer.active)
        {
            NetworkManager manager = FindObjectOfType<NetworkManager>();
            if (manager != null)
            {
                Debug.Log("[ServerBootstrap] Starting Mirror server...");
                manager.StartServer();

                Debug.Log("[ServerBootstrap] Initializing DB right after StartServer()");
                DatabaseManager.Initialize();
            }
            else
            {
                Debug.LogError("[ServerBootstrap] No NetworkManager found!");
            }
        }

        while (true)
        {
            yield return new WaitForSeconds(10f);
            Debug.Log("[ServerBootstrap] Server alive, " +
                      $"connections={NetworkServer.connections.Count}");
        }

    }
}
#endif
