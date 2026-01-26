using Mirror;
using UnityEngine;
using System;

public class PlayerInventory : NetworkBehaviour
{
    public class SyncListString : SyncList<string> { }
    public SyncListString ItemsJson = new SyncListString();

    [SerializeField] private int maxSlots = 40;

    public event Action OnInventoryChanged;

#if UNITY_SERVER || UNITY_EDITOR
    public override void OnStartServer()
    {
        base.OnStartServer();

        // init fixed slots
        if (ItemsJson.Count == 0)
        {
            for (int i = 0; i < maxSlots; i++)
                ItemsJson.Add(""); // empty slot
        }
    }
#endif
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        PlayerInventory inv = GetComponent<PlayerInventory>();
        CanvasInventory.Instance.Bind(inv);
    }
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
        for (int i = 0; i < maxSlots; i++)
            if (string.IsNullOrEmpty(ItemsJson[i])) return true;
        return false;
    }


    [Server]
    public bool AddItem(ItemInstance inst)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (string.IsNullOrEmpty(ItemsJson[i]))
            {
                ItemsJson[i] = JsonUtility.ToJson(inst);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }
    [Server]
    public bool MoveOrSwap(int from, int to)
    {
        if (from < 0 || from >= maxSlots || to < 0 || to >= maxSlots) return false;
        if (from == to) return true;

        if (string.IsNullOrEmpty(ItemsJson[from])) return false;

        (ItemsJson[from], ItemsJson[to]) = (ItemsJson[to], ItemsJson[from]);
        return true;
    }
    [Server]
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= maxSlots) return false;
        ItemsJson[index] = "";
        return true;
    }

    [Server]
    public bool TryRemoveByInstanceId(long instanceId)
    {
        int idx = FindIndexByInstanceId(instanceId);
        if (idx < 0) return false;
        ItemsJson[idx] = "";
        return true;
    }

    // =========================
    // CLIENT + SERVER HELPERS
    // =========================
    [Command]
    public void CmdMoveOrSwap(int from, int to)
    {
        MoveOrSwap(from, to);
    }
    public int Count => ItemsJson.Count;

    public ItemInstance GetItemByIndex(int index)
    {
        if (index < 0 || index >= ItemsJson.Count) return default;
        return JsonUtility.FromJson<ItemInstance>(ItemsJson[index]);
    }

    public bool TryGetByInstanceId(long instanceId, out ItemInstance inst, out int index)
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
    public ItemInstance[] GetInventory()
    {
        ItemInstance[] items = new ItemInstance[ItemsJson.Count];

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            items[i] = JsonUtility.FromJson<ItemInstance>(ItemsJson[i]);
        }
        return items;
    }
    private int FindIndexByInstanceId(long instanceId)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            var tmp = JsonUtility.FromJson<ItemInstance>(ItemsJson[i]);
            if (tmp.instanceId == instanceId) return i;
        }
        return -1;
    }

    [Server]
    public void Server_AddItem(ItemInstance item)
    {
        AddItem(item);
        Debug.Log($"[Inventory] {netId} picked item baseId={item.baseId} rarity={item.rarity}");
        // TODO: save DB + sync UI
    }
}
