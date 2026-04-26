using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInventoryData
{
    public List<string> itemsJson = new();
}

public class PlayerInventory : NetworkBehaviour
{
    public class SyncListString : SyncList<string> { }

    public SyncListString ItemsJson = new SyncListString();

    [SerializeField] private int maxSlots = 40;

    public event Action OnInventoryChanged;

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureSlots();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        StartCoroutine(BindWhenReady());
    }

    private System.Collections.IEnumerator BindWhenReady()
    {
        while (CanvasInventory.Instance == null)
            yield return null;

        CanvasInventory.Instance.Bind(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ItemsJson.Callback += OnItemsChanged;
        OnInventoryChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        ItemsJson.Callback -= OnItemsChanged;
        base.OnStopClient();
    }

    private void OnItemsChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        OnInventoryChanged?.Invoke();
    }

    [Server]
    private void EnsureSlots()
    {
        while (ItemsJson.Count < maxSlots)
            ItemsJson.Add("");

        while (ItemsJson.Count > maxSlots)
            ItemsJson.RemoveAt(ItemsJson.Count - 1);
    }

    [Server]
    public bool AddItem(ItemInstance item)
    {
        EnsureSlots();

        if (item == null || item.instanceId == 0)
        {
            Debug.LogError("[Inventory] AddItem failed: invalid item");
            return false;
        }

        string json = SerializeItem(item);

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(ItemsJson[i]))
            {
                ItemsJson[i] = json;
                Debug.Log($"[Inventory] {netId} picked item={item.itemName} baseId={item.baseId} rarity={item.rarity} slot={i}");
                return true;
            }
        }

        Debug.LogWarning("[Inventory] AddItem failed: inventory full");
        return false;
    }

    [Server]
    public bool Server_AddItem(ItemInstance item)
    {
        return AddItem(item);
    }

    [Server]
    public void SetSlot(int index, ItemInstance item)
    {
        EnsureSlots();

        if (index < 0 || index >= ItemsJson.Count)
            return;

        if (item == null || item.instanceId == 0)
        {
            ItemsJson[index] = "";
            return;
        }

        ItemsJson[index] = SerializeItem(item);
    }

    [Server]
    public void RemoveAt(int index)
    {
        EnsureSlots();

        if (index < 0 || index >= ItemsJson.Count)
            return;

        ItemsJson[index] = "";
    }

    [Server]
    public bool MoveOrSwap(int from, int to)
    {
        EnsureSlots();

        if (from < 0 || from >= ItemsJson.Count) return false;
        if (to < 0 || to >= ItemsJson.Count) return false;
        if (from == to) return true;

        if (string.IsNullOrWhiteSpace(ItemsJson[from]))
            return false;

        string temp = ItemsJson[from];
        ItemsJson[from] = ItemsJson[to];
        ItemsJson[to] = temp;

        return true;
    }

    [Server]
    public bool TryRemoveByInstanceId(long instanceId)
    {
        EnsureSlots();

        int idx = FindIndexByInstanceId(instanceId);

        if (idx < 0)
            return false;

        ItemsJson[idx] = "";
        return true;
    }

    [Command]
    public void CmdMoveOrSwap(int from, int to)
    {
        MoveOrSwap(from, to);
    }

    public int Count => ItemsJson.Count;

    public ItemInstance GetItemByIndex(int index)
    {
        if (index < 0 || index >= ItemsJson.Count)
            return default;

        if (string.IsNullOrWhiteSpace(ItemsJson[index]))
            return default;

        return DeserializeItem(ItemsJson[index]);
    }

    public bool TryGetByInstanceId(long instanceId, out ItemInstance inst, out int index)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(ItemsJson[i]))
                continue;

            ItemInstance tmp = DeserializeItem(ItemsJson[i]);

            if (tmp != null && tmp.instanceId == instanceId)
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
            items[i] = string.IsNullOrWhiteSpace(ItemsJson[i])
                ? default
                : DeserializeItem(ItemsJson[i]);
        }

        return items;
    }

    private int FindIndexByInstanceId(long instanceId)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(ItemsJson[i]))
                continue;

            ItemInstance tmp = DeserializeItem(ItemsJson[i]);

            if (tmp != null && tmp.instanceId == instanceId)
                return i;
        }

        return -1;
    }

    private ItemInstance DeserializeItem(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonUtility.FromJson<ItemInstance>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inventory] Deserialize failed. json={json} error={e}");
            return default;
        }
    }

    private string SerializeItem(ItemInstance item)
    {
        if (item == null)
            return "";

        try
        {
            return JsonUtility.ToJson(item);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inventory] Serialize failed. item={item.itemName} error={e}");
            return "";
        }
    }

    [Server]
    public PlayerInventoryData GetSaveData()
    {
        EnsureSlots();

        return new PlayerInventoryData
        {
            itemsJson = new List<string>(ItemsJson)
        };
    }

    [Server]
    public void LoadSaveData(PlayerInventoryData data)
    {
        ItemsJson.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            if (data != null && data.itemsJson != null && i < data.itemsJson.Count)
                ItemsJson.Add(data.itemsJson[i]);
            else
                ItemsJson.Add("");
        }

        Debug.Log($"[PlayerInventory] Inventory loaded with {ItemsJson.Count} slots.");
    }
}