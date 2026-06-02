using Mirror;
using UnityEngine;

public class InstanceRedirectManager : NetworkBehaviour
{
    public static InstanceRedirectManager Instance { get; private set; }

    [Header("Town")]
    [SerializeField] private string townIp = "72.60.212.58";
    [SerializeField] private int townPort = 8000;
    [SerializeField] private string townSceneName = "Town";

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void RedirectToTown(NetworkConnectionToClient conn)
    {
        if (conn == null)
        {
            Debug.LogError("[InstanceRedirectManager] Cannot redirect to Town, conn is null");
            return;
        }

        Debug.Log(
            $"[InstanceRedirectManager] Redirecting to Town | " +
            $"ip={townIp} | port={townPort} | scene={townSceneName}"
        );

        TargetSwitchToInstance(conn, townIp, townPort, townSceneName);
    }

    [Server]
    public void RedirectToInstance(NetworkConnectionToClient conn, string ip, int port, string sceneName)
    {
        if (conn == null)
        {
            Debug.LogError("[InstanceRedirectManager] Cannot redirect, conn is null");
            return;
        }

        TargetSwitchToInstance(conn, ip, port, sceneName);
    }

    [TargetRpc]
    private void TargetSwitchToInstance(
        NetworkConnectionToClient conn,
        string ip,
        int port,
        string sceneName
    )
    {
        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[InstanceRedirectManager] ClientSideInstanceManager.Instance is null");
            return;
        }

        ClientSideInstanceManager.Instance.SwitchToInstance(
            (ushort)port,
            ip,
            sceneName
        );
    }
}