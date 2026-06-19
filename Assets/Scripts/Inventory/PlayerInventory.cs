using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInventoryData
{
    public List<string> itemsJson = new List<string>();
}

public class PlayerInventory : NetworkBehaviour, ITradeInventory
{
    public class SyncListString : SyncList<string> { }

    private class TradeSlotLock
    {
        public string lockId;
        public int reservedAmount;
    }

    public SyncListString ItemsJson = new SyncListString();

    [SerializeField] private int maxSlots = 40;

    public event Action OnInventoryChanged;

    private readonly Dictionary<int, TradeSlotLock> tradeLocks = new Dictionary<int, TradeSlotLock>();

    public int Count => ItemsJson.Count;
    public int TradeSlotCount => ItemsJson.Count;

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

    // =========================================================
    // TRADE API
    // =========================================================

    [Server]
    public bool TryGetTradePayloadServer(int slotIndex, out LootPayload payload)
    {
        payload = null;

        EnsureSlots();

        if (!IsValidSlot(slotIndex))
            return false;

        InventoryItemData data = GetDataByIndex(slotIndex);

        if (data == null)
            return false;

        payload = CreatePayloadFromInventoryData(data);

        return payload != null && payload.IsValid();
    }

    [Server]
    public bool IsTradeSlotLockedServer(int slotIndex)
    {
        return tradeLocks.ContainsKey(slotIndex);
    }

    [Server]
    public bool TryLockTradeSlotServer(int slotIndex, int amount, string lockId)
    {
        EnsureSlots();

        if (!IsValidSlot(slotIndex))
            return false;

        if (amount <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(lockId))
            return false;

        if (tradeLocks.ContainsKey(slotIndex))
            return false;

        if (!TryGetTradePayloadServer(slotIndex, out LootPayload payload))
            return false;

        if (payload.amount < amount)
            return false;

        tradeLocks.Add(slotIndex, new TradeSlotLock
        {
            lockId = lockId,
            reservedAmount = amount
        });

        Debug.Log($"[Inventory] Trade lock slot={slotIndex} amount={amount} lockId={lockId}");

        return true;
    }

    [Server]
    public void UnlockTradeSlotServer(int slotIndex, string lockId)
    {
        if (!tradeLocks.TryGetValue(slotIndex, out TradeSlotLock currentLock))
            return;

        if (currentLock.lockId != lockId)
            return;

        tradeLocks.Remove(slotIndex);

        Debug.Log($"[Inventory] Trade unlock slot={slotIndex}");
    }

    [Server]
    public bool CanReceiveTradePayloadsServer(List<LootPayload> payloads)
    {
        EnsureSlots();

        if (payloads == null || payloads.Count == 0)
            return true;

        List<string> simulatedSlots = new List<string>(ItemsJson);

        ApplySimulatedTradeRemovals(simulatedSlots);

        for (int i = 0; i < payloads.Count; i++)
        {
            InventoryItemData data = CreateDataFromPayload(payloads[i]);

            if (data == null)
                return false;

            bool added = SimulateAddInventoryData(simulatedSlots, data);

            if (!added)
                return false;
        }

        return true;
    }

    [Server]
    public string CreateTradeSnapshotServer()
    {
        EnsureSlots();

        PlayerInventoryData snapshot = new PlayerInventoryData
        {
            itemsJson = new List<string>(ItemsJson)
        };

        return JsonUtility.ToJson(snapshot);
    }

    [Server]
    public void RestoreTradeSnapshotServer(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return;

        try
        {
            PlayerInventoryData data = JsonUtility.FromJson<PlayerInventoryData>(snapshotJson);
            LoadSaveData(data);

            tradeLocks.Clear();

            Debug.Log("[Inventory] Trade snapshot restored.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inventory] RestoreTradeSnapshotServer failed. error={e}");
        }
    }

    [Server]
    public bool TryRemoveTradePayloadServer(
        int slotIndex,
        int amount,
        string lockId,
        out LootPayload removedPayload
    )
    {
        removedPayload = null;

        EnsureSlots();

        if (!IsValidSlot(slotIndex))
            return false;

        if (!tradeLocks.TryGetValue(slotIndex, out TradeSlotLock currentLock))
            return false;

        if (currentLock.lockId != lockId)
            return false;

        if (currentLock.reservedAmount != amount)
            return false;

        InventoryItemData currentData = GetDataByIndex(slotIndex);

        if (currentData == null)
            return false;

        if (currentData.amount < amount)
            return false;

        removedPayload = CreatePayloadFromInventoryData(currentData);

        if (removedPayload == null)
            return false;

        removedPayload.amount = amount;

        if (currentData.amount == amount)
        {
            ItemsJson[slotIndex] = "";
        }
        else
        {
            currentData.amount -= amount;
            ItemsJson[slotIndex] = SerializeInventoryData(currentData);
        }

        tradeLocks.Remove(slotIndex);

        SavePlayerStateServer();

        Debug.Log($"[Inventory] Trade removed slot={slotIndex} amount={amount}");

        return true;
    }

    [Server]
    public bool TryAddTradePayloadServer(LootPayload payload)
    {
        if (payload == null || !payload.IsValid())
            return false;

        InventoryItemData data = CreateDataFromPayload(payload);

        if (data == null)
            return false;

        bool added = AddInventoryData(data);

        if (added)
            Debug.Log($"[Inventory] Trade received lootableId={payload.lootableId} amount={payload.amount}");

        return added;
    }

    [Server]
    private void ApplySimulatedTradeRemovals(List<string> simulatedSlots)
    {
        foreach (KeyValuePair<int, TradeSlotLock> pair in tradeLocks)
        {
            int index = pair.Key;
            int reservedAmount = pair.Value.reservedAmount;

            if (index < 0 || index >= simulatedSlots.Count)
                continue;

            InventoryItemData data = DeserializeInventoryData(simulatedSlots[index]);

            if (data == null)
                continue;

            data.amount -= reservedAmount;

            if (data.amount <= 0)
                simulatedSlots[index] = "";
            else
                simulatedSlots[index] = SerializeInventoryData(data);
        }
    }

    private bool SimulateAddInventoryData(List<string> simulatedSlots, InventoryItemData data)
    {
        if (data == null || data.lootableId == 0 || data.amount <= 0)
            return false;

        if (data.IsGeneratedItem())
            return SimulateAddToEmptySlot(simulatedSlots, data);

        LootableSO lootable = LootableDatabase.Get(data.lootableId);

        bool stackable = lootable != null && lootable.Stackable;
        int maxStack = stackable ? Mathf.Max(1, lootable.MaxStack) : 1;

        if (!stackable)
        {
            data.amount = 1;
            return SimulateAddToEmptySlot(simulatedSlots, data);
        }

        return SimulateAddStackable(simulatedSlots, data, maxStack);
    }

    private bool SimulateAddStackable(List<string> simulatedSlots, InventoryItemData data, int maxStack)
    {
        int remaining = data.amount;

        for (int i = 0; i < simulatedSlots.Count; i++)
        {
            if (remaining <= 0)
                break;

            InventoryItemData slotData = DeserializeInventoryData(simulatedSlots[i]);

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

            simulatedSlots[i] = SerializeInventoryData(slotData);
        }

        for (int i = 0; i < simulatedSlots.Count; i++)
        {
            if (remaining <= 0)
                break;

            if (!IsEmptySlot(simulatedSlots[i]))
                continue;

            int added = Mathf.Min(maxStack, remaining);

            InventoryItemData newStack = new InventoryItemData
            {
                lootableId = data.lootableId,
                amount = added,
                itemJson = "",
                displayNameOverride = data.displayNameOverride,
                hasRarityColor = data.hasRarityColor,
                rarity = data.rarity,
                lootableType = data.lootableType,
                description = data.description
            };

            simulatedSlots[i] = SerializeInventoryData(newStack);
            remaining -= added;
        }

        return remaining <= 0;
    }

    private bool SimulateAddToEmptySlot(List<string> simulatedSlots, InventoryItemData data)
    {
        for (int i = 0; i < simulatedSlots.Count; i++)
        {
            if (!IsEmptySlot(simulatedSlots[i]))
                continue;

            simulatedSlots[i] = SerializeInventoryData(data);
            return true;
        }

        return false;
    }

    private LootPayload CreatePayloadFromInventoryData(InventoryItemData data)
    {
        if (data == null || data.lootableId == 0 || data.amount <= 0)
            return null;

        return new LootPayload
        {
            lootableId = data.lootableId,
            amount = Mathf.Max(1, data.amount),
            itemJson = data.itemJson ?? "",
            displayNameOverride = data.displayNameOverride ?? "",
            hasRarityColor = data.hasRarityColor,
            rarity = data.rarity
        };
    }

    private InventoryItemData CreateDataFromPayload(LootPayload payload)
    {
        if (payload == null || payload.lootableId == 0 || payload.amount <= 0)
            return null;

        InventoryItemData data = InventoryItemData.FromPayload(payload);

        if (data == null)
            return null;

        data.amount = Mathf.Max(1, payload.amount);

        return data;
    }

    // =========================================================
    // CURRENCY / SIGIL
    // =========================================================

    [Command]
    public void CmdUseCurrencyOnItem(int currencySlotIndex, int targetItemSlotIndex)
    {
        UseCurrencyOnItemServer(currencySlotIndex, targetItemSlotIndex);
    }

    [Server]
    private bool UseCurrencyOnItemServer(int currencySlotIndex, int targetItemSlotIndex)
    {
        EnsureSlots();

        if (!IsValidSlot(currencySlotIndex) || !IsValidSlot(targetItemSlotIndex))
            return false;

        if (currencySlotIndex == targetItemSlotIndex)
            return false;

        if (IsTradeSlotLockedServer(currencySlotIndex) || IsTradeSlotLockedServer(targetItemSlotIndex))
        {
            Debug.LogWarning("[Inventory] Cannot use currency: one of the slots is locked by trade.");
            return false;
        }

        InventoryItemData currencyData = GetDataByIndex(currencySlotIndex);
        InventoryItemData targetData = GetDataByIndex(targetItemSlotIndex);

        if (currencyData == null || targetData == null)
            return false;

        if (!targetData.IsGeneratedItem())
            return false;

        LootableSO lootable = LootableDatabase.Get(currencyData.lootableId);
        CurrencySO currency = lootable as CurrencySO;

        if (currency == null)
            return false;

        ItemCurrencyEffectSO itemEffect = currency.effect as ItemCurrencyEffectSO;

        if (itemEffect == null)
            return false;

        ItemInstance targetItem = DeserializeItem(targetData.itemJson);

        if (targetItem == null || targetItem.instanceId == 0)
            return false;

        if (!itemEffect.CanUseOnItem(targetItem))
            return false;

        System.Random rng = new System.Random();

        itemEffect.UseOnItem(targetItem, rng);
        targetItem.EnsureLists();

        targetData.itemJson = JsonUtility.ToJson(targetItem);
        targetData.displayNameOverride = targetItem.itemName;
        targetData.hasRarityColor = true;
        targetData.rarity = targetItem.rarity;
        targetData.amount = 1;

        ItemsJson[targetItemSlotIndex] = SerializeInventoryData(targetData);

        ConsumeOneAt(currencySlotIndex);

        SavePlayerStateServer();

        Debug.Log($"[Inventory] Used currency={currency.DisplayName} on item={targetItem.itemName}");

        return true;
    }

    [Server]
    private void ConsumeOneAt(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (IsTradeSlotLockedServer(index))
            return;

        InventoryItemData data = GetDataByIndex(index);

        if (data == null)
            return;

        data.amount--;

        if (data.amount <= 0)
        {
            ItemsJson[index] = "";
            return;
        }

        ItemsJson[index] = SerializeInventoryData(data);
    }

    // =========================================================
    // ADD ITEMS / LOOT
    // =========================================================

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

            if (IsTradeSlotLockedServer(i))
                continue;

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

            if (IsTradeSlotLockedServer(i))
                continue;

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
                rarity = data.rarity,
                lootableType = data.lootableType,
                description = data.description
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
            if (IsTradeSlotLockedServer(i))
                continue;

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
            if (IsTradeSlotLockedServer(i))
                continue;

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

    // =========================================================
    // SET / REMOVE / MOVE
    // =========================================================

    [Server]
    public bool SetSlot(int index, ItemInstance item)
    {
        EnsureSlots();

        if (!IsValidSlot(index))
            return false;

        if (IsTradeSlotLockedServer(index))
        {
            Debug.LogWarning($"[Inventory] Cannot SetSlot: slot {index} is locked by trade.");
            return false;
        }

        if (item == null || item.instanceId == 0)
            ItemsJson[index] = "";
        else
            ItemsJson[index] = SerializeInventoryData(CreateDataFromItem(item));

        SavePlayerStateServer();

        return true;
    }

    public InventoryItemData GetSlotDataByIndex(int index)
    {
        if (index < 0 || index >= ItemsJson.Count)
            return null;

        if (string.IsNullOrWhiteSpace(ItemsJson[index]))
            return null;

        return JsonUtility.FromJson<InventoryItemData>(ItemsJson[index]);
    }

    [Server]
    public bool SetSlotData(int index, InventoryItemData data)
    {
        EnsureSlots();

        if (!IsValidSlot(index))
            return false;

        if (IsTradeSlotLockedServer(index))
        {
            Debug.LogWarning($"[Inventory] Cannot SetSlotData: slot {index} is locked by trade.");
            return false;
        }

        ItemsJson[index] = data == null ? "" : SerializeInventoryData(data);

        SavePlayerStateServer();

        return true;
    }

    [Server]
    public bool AddSlotData(InventoryItemData data)
    {
        if (data == null || data.lootableId == 0 || data.amount <= 0)
            return false;

        return AddInventoryData(data);
    }

    [Server]
    public bool RemoveAt(int index)
    {
        return DeleteItemServer(index);
    }

    [Server]
    public bool DeleteItemServer(int index)
    {
        EnsureSlots();

        if (!IsValidSlot(index))
            return false;

        if (IsTradeSlotLockedServer(index))
        {
            Debug.LogWarning($"[Inventory] Cannot delete slot={index}: locked by trade.");
            return false;
        }

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

        if (!IsValidSlot(from) || !IsValidSlot(to))
            return false;

        if (from == to)
            return true;

        if (IsTradeSlotLockedServer(from) || IsTradeSlotLockedServer(to))
        {
            Debug.LogWarning("[Inventory] Cannot move/swap: one of the slots is locked by trade.");
            return false;
        }

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

        if (IsTradeSlotLockedServer(index))
        {
            Debug.LogWarning($"[Inventory] Cannot remove instanceId={instanceId}: slot locked by trade.");
            return false;
        }

        ItemsJson[index] = "";

        Debug.Log($"[Inventory] Deleted generated item instanceId={instanceId} slot={index}");

        SavePlayerStateServer();

        return true;
    }

    // =========================================================
    // READ API
    // =========================================================

    public InventoryItemData GetDataByIndex(int index)
    {
        if (!IsValidSlot(index))
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

    // =========================================================
    // HELPERS
    // =========================================================

    private bool IsValidSlot(int index)
    {
        return index >= 0 && index < ItemsJson.Count;
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
            rarity = item.rarity,
            lootableType = LootableType.GeneratedItem
        };
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

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
        tradeLocks.Clear();

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

    // =========================================================
    // DEBUG CLEAR
    // =========================================================

    [Command]
    public void CmdClearInventory()
    {
        if (tradeLocks.Count > 0)
        {
            Debug.LogWarning("[Inventory] Cannot clear inventory while trade slots are locked.");
            return;
        }

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

        if (tradeLocks.Count > 0)
        {
            Debug.LogWarning("[Inventory] Cannot clear inventory while trade slots are locked.");
            return;
        }

        for (int i = 0; i < ItemsJson.Count; i++)
            ItemsJson[i] = "";

        OnInventoryChanged?.Invoke();
    }

    [Command]
    public void CmdClearPlayerState()
    {
        if (tradeLocks.Count > 0)
        {
            Debug.LogWarning("[PlayerState] Cannot clear player state while trade slots are locked.");
            return;
        }

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