using Mirror;
using UnityEngine;

public class ClientConnectionManager : MonoBehaviour
{
    private void Start()
    {
        // Auto-start client connection on awake
        if (!NetworkClient.isConnected && !NetworkClient.active)
        {
            Debug.Log("[ClientConnectionManager] Connecting to server...");
            NetworkManager.singleton.StartClient();
        }
    }
}
