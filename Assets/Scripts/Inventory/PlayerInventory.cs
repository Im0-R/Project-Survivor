using Mirror;
using UnityEngine;
using System;

public class PlayerInventory : NetworkBehaviour
{
    // Sync inventory: we store ItemInstance as JSON
    // (easy to save in DB)
    public class SyncListString : SyncList<string> { }
    public SyncListString ItemsJson = new SyncListString();

    // Optional: inventory limit
    [SerializeField] private int maxSlots = 60;

    // Local callback (UI) when inventory changes
    public event Action OnInventoryChanged;

    public override void OnStartClient()
    {
        base.OnStartClient();
        ItemsJson.Callback += OnItemsChanged;
    }

    public override void OnStopClient()
    {
        ItemsJson.Callback -= OnItemsChanged;
        base.OnStopClient();
    }

    private void OnItemsChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        // rebuild UI
        OnInventoryChanged?.Invoke();
    }

    // =========================
    // SERVER API
    // =========================

    [Server]
    public bool CanAddItem()
    {
        return ItemsJson.Count < maxSlots;
    }

    [Server]
    public bool AddItem(ItemInstance inst)
    {
        if (!CanAddItem()) return false;

        ItemsJson.Add(JsonUtility.ToJson(inst));
        return true;
    }

    [Server]
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= ItemsJson.Count) return false;
        ItemsJson.RemoveAt(index);
        return true;
    }

    [Server]
    public bool TryRemoveByInstanceId(int instanceId)
    {
        int idx = FindIndexByInstanceId(instanceId);
        if (idx < 0) return false;
        ItemsJson.RemoveAt(idx);
        return true;
    }

    // =========================
    // CLIENT + SERVER HELPERS
    // =========================

    public int Count => ItemsJson.Count;

    public ItemInstance GetItemByIndex(int index)
    {
        if (index < 0 || index >= ItemsJson.Count) return default;
        return JsonUtility.FromJson<ItemInstance>(ItemsJson[index]);
    }

    public bool TryGetByInstanceId(int instanceId, out ItemInstance inst, out int index)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            var tmp = JsonUtility.FromJson<ItemInstance>(ItemsJson[i]);
            if (tmp.instanceId == instanceId)
            {
                inst = tmp;
                index = i;
                return true;
            }
        }

        inst = default;
        index = -1;
        return false;
    }

    private int FindIndexByInstanceId(int instanceId)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            var tmp = JsonUtility.FromJson<ItemInstance>(ItemsJson[i]);
            if (tmp.instanceId == instanceId) return i;
        }
        return -1;
    }

    // =========================
    // DEBUG / TEST
    // =========================

#if UNITY_SERVER || UNITY_EDITOR
    public override void OnStartServer()
    {
        base.OnStartServer();

        // Exemple: donner 2 items test au spawn serveur
        // (à enlever quand t’es ok)
        // var it1 = ItemGenerator.Create(baseId: 1, itemLevel: 1, rarity: ItemRarity.Magic);
        // var it2 = ItemGenerator.Create(baseId: 2, itemLevel: 1, rarity: ItemRarity.Normal);
        // AddItem(it1);
        // AddItem(it2);
    }
#endif
}
