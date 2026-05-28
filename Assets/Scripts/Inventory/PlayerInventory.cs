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

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.K))
            CmdClearInventory();

        if (Input.GetKeyDown(KeyCode.L))
            CmdClearPlayerState();
    }

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
    private void SavePlayerStateServer()
    {
#if !UNITY_CLIENT || UNITY_EDITOR
        if (connectionToClient == null)
            return;

        if (!DatabaseManager.IsInitialized())
            return;

        DatabaseManager.SavePlayerStateFromConnection(connectionToClient);
#endif
    }

    [Server]
    public bool AddItem(ItemInstance item)
    {
        EnsureSlots();

        int emptyCount = 0;

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (IsEmptySlot(ItemsJson[i]))
                emptyCount++;
        }

        Debug.Log($"[InventoryDebug] Count={ItemsJson.Count} Empty={emptyCount} Full={ItemsJson.Count - emptyCount}");

        if (item == null || item.instanceId == 0)
        {
            Debug.LogError("[Inventory] AddItem failed: invalid item");
            return false;
        }

        string json = SerializeItem(item);

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (IsEmptySlot(ItemsJson[i]))
            {
                ItemsJson[i] = json;

                Debug.Log($"[Inventory] {netId} picked item={item.itemName} baseId={item.baseId} rarity={item.rarity} slot={i}");

                SavePlayerStateServer();
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
        }
        else
        {
            ItemsJson[index] = SerializeItem(item);
        }

        SavePlayerStateServer();
    }

    [Server]
    public void RemoveAt(int index)
    {
        DeleteItemServer(index);
    }

    [Server]
    public bool DeleteItemServer(int index)
    {
        EnsureSlots();

        if (index < 0 || index >= ItemsJson.Count)
            return false;

        if (IsEmptySlot(ItemsJson[index]))
            return false;

        ItemInstance deletedItem = DeserializeItem(ItemsJson[index]);

        ItemsJson[index] = "";

        Debug.Log($"[Inventory] Deleted item slot={index} item={(deletedItem != null ? deletedItem.itemName : "unknown")}");

        SavePlayerStateServer();
        return true;
    }

    [Command]
    public void CmdDeleteItem(int index)
    {
        DeleteItemServer(index);
    }

    [Command]
    public void CmdDeleteItemByInstanceId(long instanceId)
    {
        TryRemoveByInstanceId(instanceId);
    }

    [Server]
    public bool MoveOrSwap(int from, int to)
    {
        EnsureSlots();

        if (from < 0 || from >= ItemsJson.Count) return false;
        if (to < 0 || to >= ItemsJson.Count) return false;
        if (from == to) return true;

        if (IsEmptySlot(ItemsJson[from]))
            return false;

        string temp = ItemsJson[from];
        ItemsJson[from] = ItemsJson[to];
        ItemsJson[to] = temp;

        SavePlayerStateServer();
        return true;
    }

    [Command]
    public void CmdMoveOrSwap(int from, int to)
    {
        MoveOrSwap(from, to);
    }

    [Server]
    public bool TryRemoveByInstanceId(long instanceId)
    {
        EnsureSlots();

        if (instanceId == 0)
            return false;

        int idx = FindIndexByInstanceId(instanceId);

        if (idx < 0)
            return false;

        ItemsJson[idx] = "";

        Debug.Log($"[Inventory] Deleted item instanceId={instanceId} slot={idx}");

        SavePlayerStateServer();
        return true;
    }

    public int Count => ItemsJson.Count;

    public ItemInstance GetItemByIndex(int index)
    {
        if (index < 0 || index >= ItemsJson.Count)
            return default;

        if (IsEmptySlot(ItemsJson[index]))
            return default;

        return DeserializeItem(ItemsJson[index]);
    }

    public bool TryGetByInstanceId(long instanceId, out ItemInstance inst, out int index)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (IsEmptySlot(ItemsJson[i]))
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
            items[i] = IsEmptySlot(ItemsJson[i])
                ? default
                : DeserializeItem(ItemsJson[i]);
        }

        return items;
    }

    private bool IsEmptySlot(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        json = json.Trim();

        if (json == "{}" || json == "[]" || json == "null")
            return true;

        ItemInstance item = DeserializeItem(json);

        return item == null || item.instanceId == 0;
    }

    private int FindIndexByInstanceId(long instanceId)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (IsEmptySlot(ItemsJson[i]))
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
            {
                string slotJson = data.itemsJson[i];
                ItemsJson.Add(IsEmptySlot(slotJson) ? "" : slotJson);
            }
            else
            {
                ItemsJson.Add("");
            }
        }

        Debug.Log($"[PlayerInventory] Inventory loaded with {ItemsJson.Count} slots.");
    }

    [Command]
    public void CmdClearInventory()
    {
        ClearInventoryServer();

#if !UNITY_CLIENT || UNITY_EDITOR
        string username = connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("[Inventory] Cannot clear database: username missing from authenticationData");
            return;
        }

        if (!DatabaseManager.IsInitialized())
        {
            Debug.LogError("[Inventory] Cannot clear database: DatabaseManager is not initialized");
            return;
        }

        DatabaseManager.ClearInventory(username);

        Debug.Log($"[Inventory] Inventory cleared in RAM + database for {username}");
#endif
    }

    [Server]
    public void ClearInventoryServer()
    {
        EnsureSlots();

        for (int i = 0; i < ItemsJson.Count; i++)
            ItemsJson[i] = "";

        OnInventoryChanged?.Invoke();
    }

    [Command]
    public void CmdClearPlayerState()
    {
        ClearInventoryServer();

        PlayerEquipment equipment = GetComponent<PlayerEquipment>();

        if (equipment != null)
            equipment.ClearEquipmentServer();

#if !UNITY_CLIENT || UNITY_EDITOR
        string username = connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("[PlayerState] Cannot clear database: username missing from authenticationData");
            return;
        }

        if (!DatabaseManager.IsInitialized())
        {
            Debug.LogError("[PlayerState] Cannot clear database: DatabaseManager is not initialized");
            return;
        }

        DatabaseManager.ClearPlayerState(username);

        Debug.Log($"[PlayerState] Inventory + equipment cleared in RAM + database for {username}");
#endif
    }
}