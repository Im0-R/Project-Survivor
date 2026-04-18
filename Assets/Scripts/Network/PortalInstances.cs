using Mirror;
using UnityEngine;

public class PortalInstances : NetworkBehaviour, IInteractable
{
    [SerializeField] private string targetScene = "Town";

    public void OnInteract()
    {
        if (!NetworkClient.active) return;

        Debug.Log("[PortalInstances] Interacted with portal");
        CmdRequestInstance(targetScene);
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestInstance(string sceneName, NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;
        if (sender == null) return;
        if (InstanceManager.Instance == null)
        {
            Debug.LogError("[PortalInstances] InstanceManager.Instance is null");
            return;
        }

        Debug.Log($"[PortalInstances] Request from conn={sender.connectionId}, scene={sceneName}");
        InstanceManager.Instance.CreateInstance(sender, sceneName);
    }
}