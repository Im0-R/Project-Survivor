using Mirror;
using UnityEngine;

public class PlayerSigilInventory : NetworkBehaviour
{
    public readonly SyncDictionary<int, int> Sigils = new();

    [Server]
    public bool AddSigil(int sigilId, int amount)
    {
        if (amount <= 0)
            return false;

        SigilSO sigil = SigilDatabase.Get(sigilId);
        if (sigil == null)
            return false;

        if (Sigils.ContainsKey(sigilId))
            Sigils[sigilId] += amount;
        else
            Sigils.Add(sigilId, amount);

        Debug.Log($"[Sigils] Added {amount}x {sigil.sigilName}");
        return true;
    }

    [Server]
    public bool RemoveSigil(int sigilId, int amount)
    {
        if (amount <= 0)
            return false;

        if (!Sigils.ContainsKey(sigilId))
            return false;

        if (Sigils[sigilId] < amount)
            return false;

        Sigils[sigilId] -= amount;

        if (Sigils[sigilId] <= 0)
            Sigils.Remove(sigilId);

        return true;
    }

    public int GetAmount(int sigilId)
    {
        return Sigils.TryGetValue(sigilId, out int amount) ? amount : 0;
    }

    [Command]
    public void CmdUseSigilOnInventoryItem(int sigilId, int itemIndex)
    {
        PlayerInventory itemInventory = GetComponent<PlayerInventory>();
        if (itemInventory == null)
            return;

        ItemInstance item = itemInventory.GetItemByIndex(itemIndex);
        if (item == null || item.instanceId == 0)
            return;

        if (GetAmount(sigilId) <= 0)
            return;

        SigilSO sigil = SigilDatabase.Get(sigilId);
        if (sigil == null)
            return;

        System.Random rng = new System.Random();

        bool applied = sigil.UseOn(item, rng);

        if (!applied)
            return;

        RemoveSigil(sigilId, 1);
        itemInventory.SetSlot(itemIndex, item);

        Debug.Log($"[Sigils] Used {sigil.sigilName} on {item.itemName}");
    }
}