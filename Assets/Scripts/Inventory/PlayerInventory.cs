using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInventoryData
{
    public List<string> itemsJson = new List<string>();
}

public class PlayerInventory : NetworkBehaviour
{
    public class SyncListString : SyncList<string> { }

    public SyncListString ItemsJson = new SyncListString();

    [SerializeField] private int maxSlots = 40;

    public event Action OnInventoryChanged;

    private void Update()
    {
        if (!isLocalPlayer)
            return;

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
    public bool Server_AddLoot(LootPayload payload)
    {
        if (payload == null || payload.lootableId == 0 || payload.amount <= 0)
        {
            Debug.LogError("[Inventory] Server_AddLoot failed: invalid payload.");
            return false;
        }

        InventoryItemData data = InventoryItemData.FromPayload(payload);

        if (data == null)
            return false;

        return AddInventoryData(data);
    }

    [Server]
    public bool AddItem(ItemInstance item)
    {
        return Server_AddItem(item);
    }

    [Server]
    public bool Server_AddItem(ItemInstance item)
    {
        if (item == null || item.instanceId == 0)
        {
            Debug.LogError("[Inventory] Server_AddItem failed: invalid item.");
            return false;
        }

        InventoryItemData data = CreateDataFromItem(item);
        return AddInventoryData(data);
    }

    [Server]
    private bool AddInventoryData(InventoryItemData data)
    {
        EnsureSlots();

        if (data == null || data.lootableId == 0 || data.amount <= 0)
        {
            Debug.LogError("[Inventory] AddInventoryData failed: invalid data.");
            return false;
        }

        if (data.IsGeneratedItem())
            return AddToEmptySlot(data);

        LootableSO lootable = LootableDatabase.Get(data.lootableId);

        bool stackable = lootable != null && lootable.Stackable;
        int maxStack = stackable ? Mathf.Max(1, lootable.MaxStack) : 1;

        if (!stackable)
        {
            data.amount = 1;
            return AddToEmptySlot(data);
        }

        return AddStackable(data, maxStack);
    }

    [Server]
    private bool AddStackable(InventoryItemData data, int maxStack)
    {
        int remaining = data.amount;
        int availableSpace = GetAvailableStackSpace(data.lootableId, maxStack);

        if (remaining > availableSpace)
        {
            Debug.LogWarning($"[Inventory] Not enough space for lootableId={data.lootableId} amount={data.amount}");
            return false;
        }

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (remaining <= 0)
                break;

            InventoryItemData slotData = GetDataByIndex(i);

            if (slotData == null)
                continue;

            if (slotData.IsGeneratedItem())
                continue;

            if (slotData.lootableId != data.lootableId)
                continue;

            int space = maxStack - slotData.amount;

            if (space <= 0)
                continue;

            int added = Mathf.Min(space, remaining);
            slotData.amount += added;
            remaining -= added;

            ItemsJson[i] = SerializeInventoryData(slotData);
        }

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (remaining <= 0)
                break;

            if (!IsEmptySlot(ItemsJson[i]))
                continue;

            int added = Mathf.Min(maxStack, remaining);

            InventoryItemData newStack = new InventoryItemData
            {
                lootableId = data.lootableId,
                amount = added,
                itemJson = "",
                displayNameOverride = data.displayNameOverride,
                hasRarityColor = data.hasRarityColor,
                rarity = data.rarity
            };

            ItemsJson[i] = SerializeInventoryData(newStack);
            remaining -= added;
        }

        SavePlayerStateServer();
        return true;
    }

    private int GetAvailableStackSpace(int lootableId, int maxStack)
    {
        int available = 0;

        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (IsEmptySlot(ItemsJson[i]))
            {
                available += maxStack;
                continue;
            }

            InventoryItemData slotData = GetDataByIndex(i);

            if (slotData == null)
                continue;

            if (slotData.IsGeneratedItem())
                continue;

            if (slotData.lootableId != lootableId)
                continue;

            available += Mathf.Max(0, maxStack - slotData.amount);
        }

        return available;
    }

    [Server]
    private bool AddToEmptySlot(InventoryItemData data)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            if (!IsEmptySlot(ItemsJson[i]))
                continue;

            ItemsJson[i] = SerializeInventoryData(data);

            Debug.Log($"[Inventory] Added lootableId={data.lootableId} amount={data.amount} slot={i}");

            SavePlayerStateServer();
            return true;
        }

        Debug.LogWarning("[Inventory] Add failed: inventory full.");
        return false;
    }

    [Server]
    public void SetSlot(int index, ItemInstance item)
    {
        EnsureSlots();

        if (index < 0 || index >= ItemsJson.Count)
            return;

        if (item == null || item.instanceId == 0)
            ItemsJson[index] = "";
        else
            ItemsJson[index] = SerializeInventoryData(CreateDataFromItem(item));

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

        InventoryItemData deletedData = GetDataByIndex(index);

        ItemsJson[index] = "";

        Debug.Log($"[Inventory] Deleted slot={index} lootableId={(deletedData != null ? deletedData.lootableId : 0)}");

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

        if (from < 0 || from >= ItemsJson.Count)
            return false;

        if (to < 0 || to >= ItemsJson.Count)
            return false;

        if (from == to)
            return true;

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

        int index = FindIndexByInstanceId(instanceId);

        if (index < 0)
            return false;

        ItemsJson[index] = "";

        Debug.Log($"[Inventory] Deleted generated item instanceId={instanceId} slot={index}");

        SavePlayerStateServer();
        return true;
    }

    public int Count => ItemsJson.Count;

    public InventoryItemData GetDataByIndex(int index)
    {
        if (index < 0 || index >= ItemsJson.Count)
            return null;

        if (IsEmptySlot(ItemsJson[index]))
            return null;

        return DeserializeInventoryData(ItemsJson[index]);
    }

    public ItemInstance GetItemByIndex(int index)
    {
        InventoryItemData data = GetDataByIndex(index);

        if (data == null || !data.IsGeneratedItem())
            return null;

        return DeserializeItem(data.itemJson);
    }

    public bool TryGetByInstanceId(long instanceId, out ItemInstance inst, out int index)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            ItemInstance item = GetItemByIndex(i);

            if (item != null && item.instanceId == instanceId)
            {
                inst = item;
                index = i;
                return true;
            }
        }

        inst = null;
        index = -1;
        return false;
    }

    public ItemInstance[] GetInventory()
    {
        ItemInstance[] items = new ItemInstance[ItemsJson.Count];

        for (int i = 0; i < ItemsJson.Count; i++)
            items[i] = GetItemByIndex(i);

        return items;
    }

    private int FindIndexByInstanceId(long instanceId)
    {
        for (int i = 0; i < ItemsJson.Count; i++)
        {
            ItemInstance item = GetItemByIndex(i);

            if (item != null && item.instanceId == instanceId)
                return i;
        }

        return -1;
    }

    private bool IsEmptySlot(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        string trimmed = json.Trim();

        if (trimmed == "{}" || trimmed == "[]" || trimmed == "null")
            return true;

        InventoryItemData data = DeserializeInventoryData(json);

        return data == null || data.lootableId == 0 || data.amount <= 0;
    }

    private InventoryItemData DeserializeInventoryData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            InventoryItemData data = JsonUtility.FromJson<InventoryItemData>(json);

            if (data != null && data.lootableId != 0 && data.amount > 0)
                return data;

            ItemInstance oldItem = JsonUtility.FromJson<ItemInstance>(json);

            if (oldItem != null && oldItem.instanceId != 0)
                return CreateDataFromItem(oldItem);

            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inventory] DeserializeInventoryData failed. json={json} error={e}");
            return null;
        }
    }

    private string SerializeInventoryData(InventoryItemData data)
    {
        if (data == null)
            return "";

        try
        {
            data.amount = Mathf.Max(1, data.amount);
            return JsonUtility.ToJson(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inventory] SerializeInventoryData failed. error={e}");
            return "";
        }
    }

    private ItemInstance DeserializeItem(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            ItemInstance item = JsonUtility.FromJson<ItemInstance>(json);
            item?.EnsureLists();
            return item;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inventory] DeserializeItem failed. json={json} error={e}");
            return null;
        }
    }

    private InventoryItemData CreateDataFromItem(ItemInstance item)
    {
        if (item == null || item.instanceId == 0)
            return null;

        item.EnsureLists();

        return new InventoryItemData
        {
            lootableId = item.baseId,
            amount = 1,
            itemJson = JsonUtility.ToJson(item),
            displayNameOverride = item.itemName,
            hasRarityColor = true,
            rarity = item.rarity
        };
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