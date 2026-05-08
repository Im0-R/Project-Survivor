using Mirror;
using UnityEngine;

public class PortalInstances : NetworkBehaviour, IInteractable
{
    [SerializeField] private string targetScene = "Town";

    public void OnInteract()
    {
        if (!NetworkClient.active) return;

        Debug.Log("[PortalInstances] Interacted");
        CmdRequestInstance(targetScene);
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestInstance(string sceneName, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[PortalInstances] Cmd received | sender={(sender == null ? "NULL" : sender.connectionId.ToString())}");

        if (sender == null) return;
        if (InstanceManager.Instance == null) return;

        Debug.Log($"[PortalInstances] Creating instance for conn={sender.connectionId}, scene={sceneName}");
        InstanceManager.Instance.CreateInstance(sender, sceneName);
    }
}