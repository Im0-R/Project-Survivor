#if UNITY_SERVER
using UnityEngine;
using Mirror;
using System.Collections;

public class ServerBootstrap : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        Debug.Log("[ServerBootstrap] Booting dedicated server...");

        yield return new WaitForSeconds(1f); // attendre un peu pour éviter race conditions

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

        // Boucle keep-alive pour empêcher la fermeture
        while (true)
        {
            yield return new WaitForSeconds(10f);
            Debug.Log("[ServerBootstrap] Server alive, " +
                      $"connections={NetworkServer.connections.Count}");
        }
    }
}
#endif
