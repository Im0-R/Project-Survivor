using Mirror;
using UnityEngine;

public class PortalInstances : NetworkBehaviour, IInteractable
{
    [SerializeField] private string targetScene = "Town";

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

        Debug.Log($"[PortalInstances] Creating instance for conn={sender.connectionId}, scene={sceneName}");
        InstanceManager.Instance.CreateInstance(sender, sceneName);
    }
}