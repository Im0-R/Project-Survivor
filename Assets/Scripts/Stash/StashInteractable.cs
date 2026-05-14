using Mirror;
using UnityEngine;

public class StashInteractable : NetworkBehaviour, IInteractable
{
    public void OnInteract()
    {
        if (!NetworkClient.active) return;
        if (NetworkClient.localPlayer == null) return;

        PlayerStash stash = NetworkClient.localPlayer.GetComponent<PlayerStash>();
        PlayerInventory inventory = NetworkClient.localPlayer.GetComponent<PlayerInventory>();

        if (stash == null)
        {
            Debug.LogError("[StashInteractable] Local player has no PlayerStash.");
            return;
        }

        if (CanvasStash.Instance == null)
        {
            Debug.LogError("[StashInteractable] CanvasStash.Instance is null.");
            return;
        }

        CanvasStash.Instance.Open(stash, inventory);
    }
}