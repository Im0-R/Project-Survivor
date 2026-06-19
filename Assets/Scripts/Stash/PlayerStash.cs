using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStashData
{
    public List<StashTabData> tabs = new();
}

[Serializable]
public class StashTabData
{
    public string tabName = "Tab";
    public List<string> itemsJson = new();
}

public class PlayerStash : NetworkBehaviour
{
    [Header("Stash Settings")]
    [SerializeField] private int slotsPerTab = 100;
    [SerializeField] private int defaultTabCount = 1;
    [SerializeField] private int maxTabCount = 3;

    private PlayerStashData stashData = new();
    private int currentTabIndexServer;
    private bool stashViewOpenServer;

    private readonly List<string> clientCurrentTabItemsJson = new();
    private readonly List<string> clientTabNames = new();

    public int CurrentTabIndex { get; private set; }

    public int MaxTabCount => Mathf.Max(1, maxTabCount);
    public bool CanCreateMoreTabs => TabCount < MaxTabCount;

    public event Action OnStashChanged;
    public event Action OnTabsChanged;
    public event Action<int> OnStashSlotChanged;

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureDefaultTabs();
    }

    // =========================================================
    // Client requests
    // =========================================================

    [Command]
    public void CmdRequestOpenStash()
    {
        stashViewOpenServer = true;

        EnsureDefaultTabs();
        SendFullStateToOwner();
    }

    [Command]
    public void CmdCloseStash()
    {
        stashViewOpenServer = false;
    }

    [Command]
    public void CmdOpenTab(int tabIndex)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return;

        currentTabIndexServer = tabIndex;
        stashViewOpenServer = true;

        SendCurrentTabSnapshotToOwner();
    }

    [Command]
    public void CmdCreateTab(string tabName)
    {
        CreateTabServer(tabName);
    }

    [Command]
    public void CmdRenameTab(int tabIndex, string newName)
    {
        RenameTabServer(tabIndex, newName);
    }

    [Command]
    public void CmdMoveOrSwapCurrentTab(int from, int to)
    {
        MoveOrSwapCurrentTabServer(from, to);
    }

    [Command]
    public void CmdMoveInventoryToCurrentStashSlot(int inventoryIndex, int stashIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            return;

        ITradeInventory tradeInventory = inventory as ITradeInventory;

        if (tradeInventory != null && tradeInventory.IsTradeSlotLockedServer(inventoryIndex))
        {
            Debug.LogWarning("[PlayerStash] Cannot move inventory item to stash: inventory slot is locked by trade.");
            return;
        }

        EnsureDefaultTabs();

        if (!IsValidTabIndex(currentTabIndexServer))
            return;

        StashTabData tab = stashData.tabs[currentTabIndexServer];
        EnsureTabSlots(tab);

        if (inventoryIndex < 0 || inventoryIndex >= inventory.Count)
            return;

        if (!IsValidSlotIndex(stashIndex))
            return;

        InventoryItemData inventoryData = inventory.GetSlotDataByIndex(inventoryIndex);

        if (IsInvalidSlotData(inventoryData))
            return;

        InventoryItemData previousStashData = DeserializeSlotData(tab.itemsJson[stashIndex]);

        string newStashJson = SerializeSlotData(inventoryData);

        if (string.IsNullOrWhiteSpace(newStashJson))
            return;

        InventoryItemData newInventoryData = IsInvalidSlotData(previousStashData)
            ? null
            : previousStashData;

        bool inventoryUpdated = inventory.SetSlotData(inventoryIndex, newInventoryData);

        if (!inventoryUpdated)
        {
            Debug.LogWarning("[PlayerStash] Move inventory to stash cancelled: failed to update inventory slot.");
            return;
        }

        tab.itemsJson[stashIndex] = newStashJson;

        SendSlotDeltaToOwner(stashIndex, newStashJson);
    }

    [Command]
    public void CmdMoveCurrentStashToInventorySlot(int stashIndex, int inventoryIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            return;

        ITradeInventory tradeInventory = inventory as ITradeInventory;

        if (tradeInventory != null && tradeInventory.IsTradeSlotLockedServer(inventoryIndex))
        {
            Debug.LogWarning("[PlayerStash] Cannot move stash item to inventory slot: inventory slot is locked by trade.");
            return;
        }

        EnsureDefaultTabs();

        if (!IsValidTabIndex(currentTabIndexServer))
            return;

        StashTabData tab = stashData.tabs[currentTabIndexServer];
        EnsureTabSlots(tab);

        if (!IsValidSlotIndex(stashIndex))
            return;

        if (inventoryIndex < 0 || inventoryIndex >= inventory.Count)
            return;

        InventoryItemData stashSlotData = DeserializeSlotData(tab.itemsJson[stashIndex]);

        if (IsInvalidSlotData(stashSlotData))
            return;

        InventoryItemData inventorySlotData = inventory.GetSlotDataByIndex(inventoryIndex);

        string newStashJson = "";

        if (!IsInvalidSlotData(inventorySlotData))
        {
            newStashJson = SerializeSlotData(inventorySlotData);

            if (string.IsNullOrWhiteSpace(newStashJson))
                return;
        }

        bool inventoryUpdated = inventory.SetSlotData(inventoryIndex, stashSlotData);

        if (!inventoryUpdated)
        {
            Debug.LogWarning("[PlayerStash] Move stash to inventory cancelled: failed to update inventory slot.");
            return;
        }

        tab.itemsJson[stashIndex] = newStashJson;

        SendSlotDeltaToOwner(stashIndex, newStashJson);
    }

    [Command]
    public void CmdMoveStashToInventory(int stashIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            return;

        EnsureDefaultTabs();

        if (!IsValidTabIndex(currentTabIndexServer) || !IsValidSlotIndex(stashIndex))
            return;

        StashTabData tab = stashData.tabs[currentTabIndexServer];
        EnsureTabSlots(tab);

        InventoryItemData stashSlotData = DeserializeSlotData(tab.itemsJson[stashIndex]);

        if (IsInvalidSlotData(stashSlotData))
            return;

        bool added = inventory.AddSlotData(stashSlotData);

        if (!added)
            return;

        tab.itemsJson[stashIndex] = "";

        SendSlotDeltaToOwner(stashIndex, "");
    }

    // =========================================================
    // Server API
    // =========================================================

    [Server]
    public void OpenTabServer(int tabIndex)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return;

        currentTabIndexServer = tabIndex;

        if (stashViewOpenServer)
            SendCurrentTabSnapshotToOwner();
    }

    [Server]
    public bool CreateTabServer(string tabName)
    {
        EnsureDefaultTabs();

        if (stashData.tabs.Count >= MaxTabCount)
        {
            Debug.LogWarning("[PlayerStash] Cannot create tab, max tab count reached.");
            return false;
        }

        StashTabData tab = new()
        {
            tabName = SanitizeTabName(tabName),
            itemsJson = CreateEmptySlots()
        };

        stashData.tabs.Add(tab);
        currentTabIndexServer = stashData.tabs.Count - 1;

        if (stashViewOpenServer)
            SendFullStateToOwner();

        return true;
    }

    [Server]
    public void RenameTabServer(int tabIndex, string newName)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return;

        stashData.tabs[tabIndex].tabName = SanitizeTabName(newName);

        if (stashViewOpenServer)
            SendTabNamesToOwner();
    }

    [Server]
    public bool MoveOrSwapCurrentTabServer(int from, int to)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(currentTabIndexServer))
            return false;

        if (!IsValidSlotIndex(from) || !IsValidSlotIndex(to))
            return false;

        if (from == to)
            return true;

        StashTabData tab = stashData.tabs[currentTabIndexServer];
        EnsureTabSlots(tab);

        if (IsEmptySlot(tab.itemsJson[from]))
            return false;

        string fromJson = tab.itemsJson[from];
        string toJson = tab.itemsJson[to];

        tab.itemsJson[from] = toJson;
        tab.itemsJson[to] = fromJson;

        SendTwoSlotDeltaToOwner(from, toJson, to, fromJson);

        return true;
    }

    [Server]
    public void RemoveAtServer(int tabIndex, int slotIndex)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex) || !IsValidSlotIndex(slotIndex))
            return;

        StashTabData tab = stashData.tabs[tabIndex];
        EnsureTabSlots(tab);

        tab.itemsJson[slotIndex] = "";

        if (stashViewOpenServer && tabIndex == currentTabIndexServer)
            SendSlotDeltaToOwner(slotIndex, "");
    }

    [Server]
    public PlayerStashData GetSaveData()
    {
        EnsureDefaultTabs();

        foreach (StashTabData tab in stashData.tabs)
            EnsureTabSlots(tab);

        return stashData;
    }

    [Server]
    public void LoadSaveData(PlayerStashData data)
    {
        stashData = data ?? new PlayerStashData();

        EnsureDefaultTabs();

        if (!IsValidTabIndex(currentTabIndexServer))
            currentTabIndexServer = 0;

        if (stashViewOpenServer)
            SendFullStateToOwner();
    }

    [Server]
    public void ClearStashServer()
    {
        stashData = new PlayerStashData();
        currentTabIndexServer = 0;

        EnsureDefaultTabs();

        if (stashViewOpenServer)
            SendFullStateToOwner();
    }

    // =========================================================
    // Server -> owner synchronization
    // =========================================================

    [Server]
    private void SendFullStateToOwner()
    {
        if (!CanSendToOwner())
            return;

        TargetReceiveFullState(
            connectionToClient,
            currentTabIndexServer,
            BuildTabNamesSnapshot(),
            BuildCurrentTabSnapshot()
        );
    }

    [Server]
    private void SendCurrentTabSnapshotToOwner()
    {
        if (!CanSendToOwner())
            return;

        TargetReceiveTabSnapshot(
            connectionToClient,
            currentTabIndexServer,
            BuildCurrentTabSnapshot()
        );
    }

    [Server]
    private void SendTabNamesToOwner()
    {
        if (!CanSendToOwner())
            return;

        TargetReceiveTabNames(connectionToClient, BuildTabNamesSnapshot());
    }

    [Server]
    private void SendSlotDeltaToOwner(int slotIndex, string slotJson)
    {
        if (!stashViewOpenServer || !CanSendToOwner())
            return;

        TargetReceiveSlotDelta(
            connectionToClient,
            currentTabIndexServer,
            slotIndex,
            slotJson ?? ""
        );
    }

    [Server]
    private void SendTwoSlotDeltaToOwner(
        int firstIndex,
        string firstJson,
        int secondIndex,
        string secondJson)
    {
        if (!stashViewOpenServer || !CanSendToOwner())
            return;

        TargetReceiveTwoSlotDelta(
            connectionToClient,
            currentTabIndexServer,
            firstIndex,
            firstJson ?? "",
            secondIndex,
            secondJson ?? ""
        );
    }

    [TargetRpc]
    private void TargetReceiveFullState(
        NetworkConnectionToClient target,
        int tabIndex,
        string[] tabNames,
        string[] itemsJson)
    {
        ReplaceClientTabNames(tabNames);
        ReplaceClientCurrentTab(tabIndex, itemsJson);

        OnTabsChanged?.Invoke();
        OnStashChanged?.Invoke();
    }

    [TargetRpc]
    private void TargetReceiveTabSnapshot(
        NetworkConnectionToClient target,
        int tabIndex,
        string[] itemsJson)
    {
        ReplaceClientCurrentTab(tabIndex, itemsJson);

        OnStashChanged?.Invoke();
        OnTabsChanged?.Invoke();
    }

    [TargetRpc]
    private void TargetReceiveTabNames(
        NetworkConnectionToClient target,
        string[] tabNames)
    {
        ReplaceClientTabNames(tabNames);
        OnTabsChanged?.Invoke();
    }

    [TargetRpc]
    private void TargetReceiveSlotDelta(
        NetworkConnectionToClient target,
        int tabIndex,
        int slotIndex,
        string slotJson)
    {
        if (tabIndex != CurrentTabIndex)
            return;

        if (slotIndex < 0 || slotIndex >= clientCurrentTabItemsJson.Count)
            return;

        clientCurrentTabItemsJson[slotIndex] = slotJson ?? "";

        OnStashSlotChanged?.Invoke(slotIndex);
    }

    [TargetRpc]
    private void TargetReceiveTwoSlotDelta(
        NetworkConnectionToClient target,
        int tabIndex,
        int firstIndex,
        string firstJson,
        int secondIndex,
        string secondJson)
    {
        if (tabIndex != CurrentTabIndex)
            return;

        bool firstValid = firstIndex >= 0 && firstIndex < clientCurrentTabItemsJson.Count;
        bool secondValid = secondIndex >= 0 && secondIndex < clientCurrentTabItemsJson.Count;

        if (firstValid)
            clientCurrentTabItemsJson[firstIndex] = firstJson ?? "";

        if (secondValid)
            clientCurrentTabItemsJson[secondIndex] = secondJson ?? "";

        if (firstValid)
            OnStashSlotChanged?.Invoke(firstIndex);

        if (secondValid && secondIndex != firstIndex)
            OnStashSlotChanged?.Invoke(secondIndex);
    }

    // =========================================================
    // Client read API
    // =========================================================

    public int TabCount => clientTabNames.Count;
    public int CurrentTabSlotCount => clientCurrentTabItemsJson.Count;

    public InventoryItemData GetCurrentTabSlotDataByIndex(int index)
    {
        if (index < 0 || index >= clientCurrentTabItemsJson.Count)
            return null;

        return DeserializeSlotData(clientCurrentTabItemsJson[index]);
    }

    public string GetTabName(int index)
    {
        if (index < 0 || index >= clientTabNames.Count)
            return "";

        return clientTabNames[index];
    }

    // =========================================================
    // Internal helpers
    // =========================================================

    [Server]
    private void EnsureDefaultTabs()
    {
        stashData ??= new PlayerStashData();
        stashData.tabs ??= new List<StashTabData>();

        int wantedTabCount = Mathf.Clamp(defaultTabCount, 1, MaxTabCount);

        while (stashData.tabs.Count < wantedTabCount)
        {
            stashData.tabs.Add(new StashTabData
            {
                tabName = $"Tab {stashData.tabs.Count + 1}",
                itemsJson = CreateEmptySlots()
            });
        }

        TrimExtraTabs();

        for (int i = 0; i < stashData.tabs.Count; i++)
        {
            if (stashData.tabs[i] == null)
            {
                stashData.tabs[i] = new StashTabData
                {
                    tabName = $"Tab {i + 1}",
                    itemsJson = CreateEmptySlots()
                };
            }

            if (string.IsNullOrWhiteSpace(stashData.tabs[i].tabName))
                stashData.tabs[i].tabName = $"Tab {i + 1}";

            EnsureTabSlots(stashData.tabs[i]);
        }

        if (!IsValidTabIndex(currentTabIndexServer))
            currentTabIndexServer = 0;
    }

    [Server]
    private void TrimExtraTabs()
    {
        if (stashData == null || stashData.tabs == null)
            return;

        while (stashData.tabs.Count > MaxTabCount)
        {
            int lastIndex = stashData.tabs.Count - 1;

            Debug.LogWarning(
                $"[PlayerStash] Removing extra stash tab '{stashData.tabs[lastIndex].tabName}' index={lastIndex}."
            );

            stashData.tabs.RemoveAt(lastIndex);
        }

        if (!IsValidTabIndex(currentTabIndexServer))
            currentTabIndexServer = Mathf.Max(0, stashData.tabs.Count - 1);
    }

    [Server]
    private void EnsureTabSlots(StashTabData tab)
    {
        tab.itemsJson ??= new List<string>();

        while (tab.itemsJson.Count < slotsPerTab)
            tab.itemsJson.Add("");

        while (tab.itemsJson.Count > slotsPerTab)
            tab.itemsJson.RemoveAt(tab.itemsJson.Count - 1);
    }

    private List<string> CreateEmptySlots()
    {
        List<string> slots = new(slotsPerTab);

        for (int i = 0; i < slotsPerTab; i++)
            slots.Add("");

        return slots;
    }

    [Server]
    private string[] BuildCurrentTabSnapshot()
    {
        EnsureDefaultTabs();

        StashTabData tab = stashData.tabs[currentTabIndexServer];
        EnsureTabSlots(tab);

        return tab.itemsJson.ToArray();
    }

    [Server]
    private string[] BuildTabNamesSnapshot()
    {
        EnsureDefaultTabs();

        string[] names = new string[stashData.tabs.Count];

        for (int i = 0; i < stashData.tabs.Count; i++)
            names[i] = stashData.tabs[i].tabName;

        return names;
    }

    private void ReplaceClientCurrentTab(int tabIndex, string[] itemsJson)
    {
        CurrentTabIndex = tabIndex;

        clientCurrentTabItemsJson.Clear();

        if (itemsJson == null)
            return;

        for (int i = 0; i < itemsJson.Length; i++)
            clientCurrentTabItemsJson.Add(itemsJson[i] ?? "");
    }

    private void ReplaceClientTabNames(string[] tabNames)
    {
        clientTabNames.Clear();

        if (tabNames == null)
            return;

        for (int i = 0; i < tabNames.Length; i++)
            clientTabNames.Add(tabNames[i] ?? $"Tab {i + 1}");
    }

    [Server]
    private bool CanSendToOwner()
    {
        return connectionToClient != null && connectionToClient.isReady;
    }

    private bool IsValidTabIndex(int tabIndex)
    {
        return stashData != null &&
               stashData.tabs != null &&
               tabIndex >= 0 &&
               tabIndex < stashData.tabs.Count;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < slotsPerTab;
    }

    private string SanitizeTabName(string tabName)
    {
        if (string.IsNullOrWhiteSpace(tabName))
            return $"Tab {stashData.tabs.Count + 1}";

        string safeName = tabName.Trim();

        if (safeName.Length > 24)
            safeName = safeName.Substring(0, 24);

        return safeName;
    }

    private bool IsEmptySlot(string json)
    {
        return IsInvalidSlotData(DeserializeSlotData(json));
    }

    private bool IsInvalidSlotData(InventoryItemData data)
    {
        return data == null || data.lootableId == 0 || data.amount <= 0;
    }

    private InventoryItemData DeserializeSlotData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonUtility.FromJson<InventoryItemData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PlayerStash] Failed to deserialize slot: {exception}");
            return null;
        }
    }

    private string SerializeSlotData(InventoryItemData data)
    {
        if (IsInvalidSlotData(data))
            return "";

        try
        {
            return JsonUtility.ToJson(data);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PlayerStash] Failed to serialize slot: {exception}");
            return "";
        }
    }
}