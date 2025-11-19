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
            var manager = FindObjectOfType<NetworkManager>();
            if (manager != null)
            {
                Debug.Log("[ServerBootstrap] Starting Mirror server...");
                manager.StartServer();
            }
            else
            {
                Debug.LogError("[ServerBootstrap] No NetworkManager found!");
            }
        }
        //keep-alive loop for dedicated server
        while (true)
        {
            yield return new WaitForSeconds(10f);
            Debug.Log("[ServerBootstrap] Server alive, " +
                      $"connections={NetworkServer.connections.Count}");
        }

    }
}
#endif
