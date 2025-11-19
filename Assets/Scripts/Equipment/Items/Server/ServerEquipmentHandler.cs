#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using System;
using UnityEngine;

public class ServerEquipmentHandler : MonoBehaviour
{
    private void Awake()
    {
        NetworkServer.RegisterHandler<EquipItemRequest>(OnEquipItemRequest);
    }

    private void OnEquipItemRequest(NetworkConnectionToClient conn, EquipItemRequest msg)
    {
        var player = conn.identity.GetComponent<PlayerEquipment>();
        if (player == null)
        {
            Debug.LogError("[Equip] PlayerEquipment not found");
            return;
        }

        int itemId = msg.itemId;

        //if (!DatabaseManager.PlayerOwnsItem(conn.authenticationData.userId, itemId))
        //{
        //    Debug.LogWarning("[Equip] Player does not own this item");
        //    return;
        //}

        ItemDataSO item = ItemDatabase.GetItem(itemId);
        if (item == null)
        {
            Debug.LogWarning("[Equip] Unknown item");
            return;
        }

        //Equip item
        player.Equip(item);

        // Put in Database
        //DatabaseManager.SetPlayerSlotItem(conn.authenticationData.userId, item.slot, itemId);

        Debug.Log($"[Equip] Player equipped {item.itemName} on {item.slot}");
    }
}
#endif
