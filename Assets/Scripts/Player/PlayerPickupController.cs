using Mirror;
using UnityEngine;

public class PlayerPickupController : NetworkBehaviour
{
    public static PlayerPickupController Local;

    [Header("Pickup rules")]
    [SerializeField] private float pickupRange = 4f;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Local = this;
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (Local == this) Local = null;
    }

    /// <summary>
    /// When the label is clicked
    /// </summary>
    public void RequestPickup(LootPickup loot)
    {
        if (!isLocalPlayer) return;
        if (loot == null) return;

        float dist = Vector3.Distance(transform.position, loot.transform.position);
        if (dist > pickupRange + 1f) //margin
        {
            return;
        }

        CmdRequestPickup(loot.netIdentity);
    }

    // --------------------
    // SERVER
    // --------------------

    [Command]
    private void CmdRequestPickup(NetworkIdentity lootIdentity)
    {
        if (lootIdentity == null) return;

        LootPickup loot = lootIdentity.GetComponent<LootPickup>();
        if (loot == null) return;

        float dist = Vector3.Distance(transform.position, loot.transform.position);

        if (loot.IsClaimed || dist > pickupRange)
            return;

        PlayerInventory inv = GetComponentInChildren<PlayerInventory>(true);

        if (inv == null)
        {
            Debug.LogError($"[Pickup] PlayerInventory missing on {name}");
            return;
        }

        ItemInstance item = loot.GetItem();

        bool added = inv.Server_AddItem(item);

        if (!added)
        {
            Debug.LogWarning($"[Pickup] Pickup cancelled, inventory full item={item?.itemName}");
            TargetPickupFeedback(connectionToClient, false);
            return;
        }

        loot.IsClaimed = true;

        Debug.Log($"[Pickup] Player {netId} picked loot netId={loot.netId}");

        NetworkServer.Destroy(loot.gameObject);

        TargetPickupFeedback(connectionToClient, true);
    }

    [TargetRpc]
    private void TargetPickupFeedback(NetworkConnectionToClient conn, bool success)
    {
        // Option: UI feedback (son, texte, etc.)
        // Debug.Log($"Pickup result: {success}");
    }
}
