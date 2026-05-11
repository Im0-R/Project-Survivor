using System.Collections;
using Mirror;
using UnityEngine;

public enum PortalDestinationType
{
    MapInstance,
    Town
}

public class PortalInstances : NetworkBehaviour, IInteractable
{
    [Header("Destination")]
    [SerializeField] private PortalDestinationType destinationType = PortalDestinationType.MapInstance;

    [Header("Target Instance")]
    [SerializeField] private string targetScene = "MapInstance";
    [SerializeField] private string targetMapId = "forest_01";

    [Header("Town / Master Server")]
    [SerializeField] private string townScene = "Town";
    [SerializeField] private string masterIp = "72.60.212.58";
    [SerializeField] private int masterPort = 7777;

    [Header("Redirect")]
    [SerializeField] private float redirectDelay = 8f;

    [SyncVar] private bool isLaunching;

    public void OnInteract()
    {
        if (!NetworkClient.active) return;

        Debug.Log($"[PortalInstances] Interacted | destination={destinationType}");

        CmdRequestPortal();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestPortal(NetworkConnectionToClient sender = null)
    {
        if (isLaunching)
        {
            Debug.LogWarning("[PortalInstances] Portal already launching, ignoring request.");
            return;
        }

        if (sender == null)
        {
            Debug.LogError("[PortalInstances] Sender is null");
            return;
        }

        isLaunching = true;

        switch (destinationType)
        {
            case PortalDestinationType.MapInstance:
                RequestMapInstance(sender);
                break;

            case PortalDestinationType.Town:
                RequestTown(sender);
                break;
        }
    }

    [Server]
    private void RequestMapInstance(NetworkConnectionToClient sender)
    {
        if (InstanceManager.Instance == null)
        {
            Debug.LogError("[PortalInstances] InstanceManager.Instance is null");
            isLaunching = false;
            return;
        }

        string sceneName = string.IsNullOrWhiteSpace(targetScene)
            ? "MapInstance"
            : targetScene;

        string mapId = string.IsNullOrWhiteSpace(targetMapId)
            ? "forest_01"
            : targetMapId;

        Debug.Log($"[PortalInstances] Creating instance | scene={sceneName} | mapId={mapId}");

        InstanceManager.InstanceInfo info =
            InstanceManager.Instance.CreateInstance(sender, sceneName, mapId);

        if (info == null)
        {
            Debug.LogError("[PortalInstances] CreateInstance returned null");
            isLaunching = false;
            return;
        }

        StartCoroutine(DelayedRedirect(
            sender,
            InstanceManager.Instance.HubIp,
            info.port,
            info.scene
        ));
    }

    [Server]
    private void RequestTown(NetworkConnectionToClient sender)
    {
        string ip = string.IsNullOrWhiteSpace(masterIp)
            ? "127.0.0.1"
            : masterIp;

        string sceneName = string.IsNullOrWhiteSpace(townScene)
            ? "Town"
            : townScene;

        Debug.Log($"[PortalInstances] Returning to town | ip={ip} | port={masterPort} | scene={sceneName}");

        StartCoroutine(DelayedRedirect(
            sender,
            ip,
            masterPort,
            sceneName
        ));
    }

    [Server]
    private IEnumerator DelayedRedirect(
        NetworkConnectionToClient conn,
        string ip,
        int port,
        string sceneName
    )
    {
        yield return new WaitForSeconds(redirectDelay);

        if (conn == null)
        {
            Debug.LogWarning("[PortalInstances] Conn null before redirect");
            isLaunching = false;
            yield break;
        }

        Debug.Log($"[PortalInstances] Redirecting conn={conn.connectionId} to {ip}:{port} scene={sceneName}");

        TargetSwitchToInstance(conn, ip, port, sceneName);

        isLaunching = false;
    }

    [TargetRpc]
    private void TargetSwitchToInstance(
        NetworkConnectionToClient conn,
        string ip,
        int port,
        string sceneName
    )
    {
        Debug.Log($"[PortalInstances CLIENT] TargetSwitchToInstance received | ip={ip} | port={port} | scene={sceneName}");

        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[PortalInstances CLIENT] ClientSideInstanceManager.Instance is null");
            return;
        }

        ClientSideInstanceManager.Instance.SwitchToInstance(
            (ushort)port,
            ip,
            sceneName
        );
    }
}