using System.Collections;
using kcp2k;
using Mirror;
using UnityEngine;

public class ClientSideInstanceManager : MonoBehaviour
{
    public static ClientSideInstanceManager Instance { get; private set; }

    private bool isSwitching;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SwitchToInstance(ushort port, string ip, string sceneName)
    {
        if (isSwitching)
        {
            Debug.LogWarning("[ClientSideInstanceManager] Already switching");
            return;
        }

        StartCoroutine(SwitchRoutine(port, ip, sceneName));
    }

    private IEnumerator SwitchRoutine(ushort port, string ip, string sceneName)
    {
        isSwitching = true;

        Debug.Log($"[ClientSideInstanceManager] Switching to {ip}:{port}, requestedScene={sceneName}");

        NetworkManager manager = NetworkManager.singleton;
        if (manager == null)
        {
            Debug.LogError("[ClientSideInstanceManager] NetworkManager.singleton is null");
            isSwitching = false;
            yield break;
        }

        if (NetworkClient.isConnected || NetworkClient.isConnecting)
        {
            Debug.Log("[ClientSideInstanceManager] Stop current client");
            manager.StopClient();

            while (NetworkClient.isConnected || NetworkClient.isConnecting)
                yield return null;
        }

        yield return null;

        KcpTransport kcp = manager.transport as KcpTransport;
        if (kcp == null)
            kcp = manager.GetComponent<KcpTransport>();

        if (kcp == null)
        {
            Debug.LogError("[ClientSideInstanceManager] KcpTransport not found");
            isSwitching = false;
            yield break;
        }

        manager.networkAddress = ip;
        kcp.Port = port;

        Debug.Log($"[ClientSideInstanceManager] StartClient -> {ip}:{port}");
        manager.StartClient();

        isSwitching = false;
    }
}