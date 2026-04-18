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

    public void SwitchToInstance(ushort port, string ip = InstanceManager.ipAddress)
    {
        if (isSwitching) return;
        StartCoroutine(SwitchRoutine(port, ip));
    }

    private IEnumerator SwitchRoutine(ushort port, string ip)
    {
        isSwitching = true;

        Debug.Log($"[ClientSideInstanceManager] Switching to {ip}:{port}");

        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[ClientSideInstanceManager] NetworkManager.singleton is null");
            isSwitching = false;
            yield break;
        }

        if (NetworkClient.isConnected || NetworkClient.isConnecting)
        {
            NetworkManager.singleton.StopClient();

            while (NetworkClient.isConnected || NetworkClient.isConnecting)
                yield return null;
        }

        yield return null;

        KcpTransport kcp = NetworkManager.singleton.transport as KcpTransport;
        if (kcp == null)
        {
            Debug.LogError("[ClientSideInstanceManager] KcpTransport not found");
            isSwitching = false;
            yield break;
        }

        kcp.Port = port;
        NetworkManager.singleton.networkAddress = ip;

        Debug.Log($"[ClientSideInstanceManager] Connecting to {ip}:{port}");
        NetworkManager.singleton.StartClient();

        isSwitching = false;
    }
}