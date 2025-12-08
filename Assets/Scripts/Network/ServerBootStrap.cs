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

#if UNITY_SERVER
    if (Keyboard.current != null)
        InputSystem.DisableDevice(Keyboard.current);

    if (Mouse.current != null)
        InputSystem.DisableDevice(Mouse.current);
#endif
    }


    private IEnumerator Start()
    {
        Debug.Log("[ServerBootstrap] Booting dedicated server...");

        // Attend que NetworkManager soit chargé dans la scène
        NetworkManager manager = null;
        while (manager == null)
        {
            manager = FindObjectOfType<NetworkManager>();
            yield return null; // attendre le frame suivant
        }

        Debug.Log("[ServerBootstrap] NetworkManager found, starting server...");
        manager.StartServer();

        Debug.Log("[ServerBootstrap] Initializing DB right after StartServer()");
        DatabaseManager.Initialize();

        while (true)
        {
            yield return new WaitForSeconds(10f);
            Debug.Log("[ServerBootstrap] Server alive, " +
                      $"connections={NetworkServer.connections.Count}");
        }
    }

}
#endif
