using System.Collections;
using Mirror;
using UnityEngine;

public class PortalInstances : NetworkBehaviour, IInteractable
{
    [SerializeField] private string targetScene = "Town";
    [SerializeField] private float redirectDelay = 8f;

    [SyncVar] private bool isLaunching;

    public void OnInteract()
    {
        if (!NetworkClient.active) return;

        Debug.Log("[PortalInstances] Interacted");
        CmdRequestInstance(targetScene);
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestInstance(string sceneName, NetworkConnectionToClient sender = null)
    {
        if (isLaunching)
        {
            Debug.LogWarning("[PortalInstances] Instance already launching, ignoring request.");
            return;
        }

        Debug.Log($"[PortalInstances] Cmd received | sender={(sender == null ? "NULL" : sender.connectionId.ToString())}");

        if (sender == null) return;

        if (InstanceManager.Instance == null)
        {
            Debug.LogError("[PortalInstances] InstanceManager.Instance is null");
            return;
        }

        isLaunching = true;

        InstanceManager.InstanceInfo info = InstanceManager.Instance.CreateInstance(sender, sceneName);

        if (info == null)
        {
            isLaunching = false;
            return;
        }

        StartCoroutine(DelayedRedirect(sender, info));
    }

    [Server]
    private IEnumerator DelayedRedirect(NetworkConnectionToClient conn, InstanceManager.InstanceInfo info)
    {
        yield return new WaitForSeconds(redirectDelay);

        if (conn == null)
        {
            Debug.LogWarning("[PortalInstances] Conn null before redirect");
            isLaunching = false;
            yield break;
        }

        Debug.Log($"[PortalInstances] Redirecting conn={conn.connectionId} to {InstanceManager.Instance.HubIp}:{info.port} scene={info.scene}");

        TargetSwitchToInstance(conn, InstanceManager.Instance.HubIp, info.port, info.scene);

        isLaunching = false;
    }

    [TargetRpc]
    private void TargetSwitchToInstance(NetworkConnectionToClient conn, string ip, int port, string sceneName)
    {
        Debug.Log($"[PortalInstances CLIENT] TargetSwitchToInstance received | ip={ip} | port={port} | scene={sceneName}");

        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[PortalInstances CLIENT] ClientSideInstanceManager.Instance is null");
            return;
        }

        ClientSideInstanceManager.Instance.SwitchToInstance((ushort)port, ip, sceneName);
    }
}