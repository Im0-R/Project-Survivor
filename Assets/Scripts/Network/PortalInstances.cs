using UnityEngine;
using Mirror;

public class PortalInstances : NetworkBehaviour, IInteractable
{
    public void OnInteract()
    {
        if (!NetworkClient.active) return; // sécurité client
        Debug.Log("Interacted with portal");
        CmdRequestInstance();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestInstance(NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[PortalInstances] Creating instance for {sender?.connectionId}");
        InstanceManager.Instance.CreateInstance(sender);
    }
}

