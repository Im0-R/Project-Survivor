using kcp2k;
using Mirror;
using System.Collections;
using UnityEngine;

public class ClientSideInstanceManager : MonoBehaviour
{
    public static ClientSideInstanceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SwitchToInstance(ushort port, string ip = InstanceManager.ipAddress)
    {
        StartCoroutine(SwitchRoutine(port, ip));
    }

    private IEnumerator SwitchRoutine(ushort port, string ip)
    {
        Debug.Log("[InstanceManager] Preparing to switch instance...");

        // -----------------------------
        // 1) Disconnect cleanly
        // -----------------------------
        if (NetworkClient.isConnected || NetworkClient.isConnecting)
        {
            NetworkManager.singleton.StopClient();
            yield return new WaitForSeconds(0.3f);
        }

        // -----------------------------
        // 2) Configure KCP transport
        // -----------------------------
        KcpTransport kcp = Transport.active as KcpTransport;
        if (kcp == null)
        {
            Debug.LogError("[InstanceManager] KcpTransport not found!");
            yield break;
        }

        kcp.Port = port;
        NetworkManager.singleton.networkAddress = ip;

        Debug.Log($"[InstanceManager] Transport configured → {ip}:{port}");

        // -----------------------------
        // 3) Start connection
        // -----------------------------
        Debug.Log("[InstanceManager] Connecting to instance...");
        NetworkManager.singleton.StartClient();

        yield return null;

        Debug.Log("[InstanceManager] Instance switch done! Waiting for scene sync...");
    }
}
