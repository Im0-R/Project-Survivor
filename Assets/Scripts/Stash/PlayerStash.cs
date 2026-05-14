using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

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
    public class SyncListString : SyncList<string> { }

    [Header("Stash Settings")]
    [SerializeField] private int slotsPerTab = 100;
    [SerializeField] private int defaultTabCount = 1;
    [SerializeField] private int maxTabCount = 20;

    public SyncListString CurrentTabItemsJson = new SyncListString();
    public SyncListString TabNames = new SyncListString();

    [SyncVar(hook = nameof(OnCurrentTabChanged))]
    public int CurrentTabIndex;

    private PlayerStashData stashData = new PlayerStashData();

    public event Action OnStashChanged;
    public event Action OnTabsChanged;

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureDefaultTabs();
        RefreshSyncedTabNames();
        RefreshSyncedCurrentTab();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        CurrentTabItemsJson.Callback += OnCurrentTabItemsChanged;
        TabNames.Callback += OnTabNamesChanged;

        OnStashChanged?.Invoke();
        OnTabsChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        CurrentTabItemsJson.Callback -= OnCurrentTabItemsChanged;
        TabNames.Callback -= OnTabNamesChanged;

        base.OnStopClient();
    }

    private void OnCurrentTabItemsChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        OnStashChanged?.Invoke();
    }

    private void OnTabNamesChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        OnTabsChanged?.Invoke();
    }

    private void OnCurrentTabChanged(int oldIndex, int newIndex)
    {
        OnStashChanged?.Invoke();
        OnTabsChanged?.Invoke();
    }

    // =========================
    // Commands
    // =========================

    [Command]
    public void CmdOpenTab(int tabIndex)
    {
        OpenTabServer(tabIndex);
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
    public void CmdMoveInventoryToCurrentStashTab(int inventoryIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;

        ItemInstance item = inventory.GetItemByIndex(inventoryIndex);
        if (item == null || item.instanceId == 0) return;

        bool added = AddItemToTabServer(CurrentTabIndex, item);
        if (!added) return;

        inventory.RemoveAt(inventoryIndex);

        RefreshSyncedCurrentTab();

        Debug.Log($"[Stash] Moved inventory slot {inventoryIndex} to first empty slot in stash tab {CurrentTabIndex}.");
    }

    [Command]
    public void CmdMoveInventoryToCurrentStashSlot(int inventoryIndex, int stashIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;

        EnsureDefaultTabs();

        if (!IsValidTabIndex(CurrentTabIndex)) return;

        StashTabData tab = stashData.tabs[CurrentTabIndex];
        EnsureTabSlots(tab);

        if (inventoryIndex < 0 || inventoryIndex >= inventory.Count) return;
        if (stashIndex < 0 || stashIndex >= tab.itemsJson.Count) return;

        ItemInstance inventoryItem = inventory.GetItemByIndex(inventoryIndex);
        if (inventoryItem == null || inventoryItem.instanceId == 0) return;

        ItemInstance stashItem = GetItemFromTab(CurrentTabIndex, stashIndex);

        tab.itemsJson[stashIndex] = SerializeItem(inventoryItem);

        if (stashItem != null && stashItem.instanceId != 0)
            inventory.SetSlot(inventoryIndex, stashItem);
        else
            inventory.RemoveAt(inventoryIndex);

        RefreshSyncedCurrentTab();

        Debug.Log($"[Stash] Inventory slot {inventoryIndex} moved/swapped to stash slot {stashIndex}");
    }

    [Command]
    public void CmdMoveStashToInventory(int stashIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;

        ItemInstance item = GetItemFromTab(CurrentTabIndex, stashIndex);
        if (item == null || item.instanceId == 0) return;

        bool added = inventory.AddItem(item);
        if (!added) return;

        RemoveAtServer(CurrentTabIndex, stashIndex);

        RefreshSyncedCurrentTab();

        Debug.Log($"[Stash] Moved stash tab {CurrentTabIndex} slot {stashIndex} to first empty inventory slot.");
    }

    [Command]
    public void CmdMoveCurrentStashToInventorySlot(int stashIndex, int inventoryIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;

        EnsureDefaultTabs();

        if (!IsValidTabIndex(CurrentTabIndex)) return;

        StashTabData tab = stashData.tabs[CurrentTabIndex];
        EnsureTabSlots(tab);

        if (stashIndex < 0 || stashIndex >= tab.itemsJson.Count) return;
        if (inventoryIndex < 0 || inventoryIndex >= inventory.Count) return;

        ItemInstance stashItem = GetItemFromTab(CurrentTabIndex, stashIndex);
        if (stashItem == null || stashItem.instanceId == 0) return;

        ItemInstance inventoryItem = inventory.GetItemByIndex(inventoryIndex);

        inventory.SetSlot(inventoryIndex, stashItem);

        if (inventoryItem != null && inventoryItem.instanceId != 0)
            tab.itemsJson[stashIndex] = SerializeItem(inventoryItem);
        else
            tab.itemsJson[stashIndex] = "";

        RefreshSyncedCurrentTab();

        Debug.Log($"[Stash] Stash slot {stashIndex} moved/swapped to inventory slot {inventoryIndex}");
    }

    // =========================
    // Server API
    // =========================

    [Server]
    public void OpenTabServer(int tabIndex)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return;

        CurrentTabIndex = tabIndex;
        RefreshSyncedCurrentTab();
    }

    [Server]
    public bool CreateTabServer(string tabName)
    {
        EnsureDefaultTabs();

        if (stashData.tabs.Count >= maxTabCount)
        {
            Debug.LogWarning("[Stash] Cannot create tab: max tab count reached.");
            return false;
        }

        string safeName = SanitizeTabName(tabName);

        StashTabData tab = new StashTabData
        {
            tabName = safeName,
            itemsJson = CreateEmptySlots()
        };

        stashData.tabs.Add(tab);

        CurrentTabIndex = stashData.tabs.Count - 1;

        RefreshSyncedTabNames();
        RefreshSyncedCurrentTab();

        Debug.Log($"[Stash] Created tab '{safeName}' index={CurrentTabIndex}");

        return true;
    }

    [Server]
    public void RenameTabServer(int tabIndex, string newName)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return;

        stashData.tabs[tabIndex].tabName = SanitizeTabName(newName);

        RefreshSyncedTabNames();

        Debug.Log($"[Stash] Renamed tab {tabIndex} to '{stashData.tabs[tabIndex].tabName}'");
    }

    [Server]
    public bool AddItemToCurrentTabServer(ItemInstance item)
    {
        return AddItemToTabServer(CurrentTabIndex, item);
    }

    [Server]
    public bool AddItemToTabServer(int tabIndex, ItemInstance item)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return false;

        if (item == null || item.instanceId == 0)
        {
            Debug.LogError("[Stash] AddItem failed: invalid item");
            return false;
        }

        StashTabData tab = stashData.tabs[tabIndex];
        EnsureTabSlots(tab);

        string json = SerializeItem(item);

        for (int i = 0; i < tab.itemsJson.Count; i++)
        {
            if (IsEmptySlot(tab.itemsJson[i]))
            {
                tab.itemsJson[i] = json;

                if (tabIndex == CurrentTabIndex)
                    RefreshSyncedCurrentTab();

                Debug.Log($"[Stash] Added item={item.itemName} baseId={item.baseId} rarity={item.rarity} tab={tabIndex} slot={i}");

                return true;
            }
        }

        Debug.LogWarning($"[Stash] AddItem failed: tab {tabIndex} full");
        return false;
    }

    [Server]
    public bool MoveOrSwapCurrentTabServer(int from, int to)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(CurrentTabIndex))
            return false;

        StashTabData tab = stashData.tabs[CurrentTabIndex];
        EnsureTabSlots(tab);

        if (from < 0 || from >= tab.itemsJson.Count) return false;
        if (to < 0 || to >= tab.itemsJson.Count) return false;
        if (from == to) return true;

        if (IsEmptySlot(tab.itemsJson[from]))
            return false;

        string temp = tab.itemsJson[from];
        tab.itemsJson[from] = tab.itemsJson[to];
        tab.itemsJson[to] = temp;

        RefreshSyncedCurrentTab();

        return true;
    }

    [Server]
    public void RemoveAtServer(int tabIndex, int slotIndex)
    {
        EnsureDefaultTabs();

        if (!IsValidTabIndex(tabIndex))
            return;

        StashTabData tab = stashData.tabs[tabIndex];
        EnsureTabSlots(tab);

        if (slotIndex < 0 || slotIndex >= tab.itemsJson.Count)
            return;

        tab.itemsJson[slotIndex] = "";

        if (tabIndex == CurrentTabIndex)
            RefreshSyncedCurrentTab();
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

        foreach (StashTabData tab in stashData.tabs)
            EnsureTabSlots(tab);

        if (CurrentTabIndex < 0 || CurrentTabIndex >= stashData.tabs.Count)
            CurrentTabIndex = 0;

        RefreshSyncedTabNames();
        RefreshSyncedCurrentTab();

        Debug.Log($"[PlayerStash] Loaded stash with {stashData.tabs.Count} tabs.");
    }

    [Server]
    public void ClearStashServer()
    {
        stashData = new PlayerStashData();

        EnsureDefaultTabs();

        CurrentTabIndex = 0;

        RefreshSyncedTabNames();
        RefreshSyncedCurrentTab();

        Debug.Log("[PlayerStash] Stash cleared.");
    }

    // =========================
    // Client/Public Read API
    // =========================

    public int TabCount => TabNames.Count;
    public int CurrentTabSlotCount => CurrentTabItemsJson.Count;

    public ItemInstance GetCurrentTabItemByIndex(int index)
    {
        if (index < 0 || index >= CurrentTabItemsJson.Count)
            return default;

        if (IsEmptySlot(CurrentTabItemsJson[index]))
            return default;

        return DeserializeItem(CurrentTabItemsJson[index]);
    }

    public ItemInstance[] GetCurrentTabItems()
    {
        ItemInstance[] items = new ItemInstance[CurrentTabItemsJson.Count];

        for (int i = 0; i < CurrentTabItemsJson.Count; i++)
        {
            items[i] = IsEmptySlot(CurrentTabItemsJson[i])
                ? default
                : DeserializeItem(CurrentTabItemsJson[i]);
        }

        return items;
    }

    public string GetTabName(int index)
    {
        if (index < 0 || index >= TabNames.Count)
            return "";

        return TabNames[index];
    }

    // =========================
    // Internal helpers
    // =========================

    [Server]
    private void EnsureDefaultTabs()
    {
        if (stashData == null)
            stashData = new PlayerStashData();

        if (stashData.tabs == null)
            stashData.tabs = new List<StashTabData>();

        int wantedTabCount = Mathf.Max(1, defaultTabCount);

        while (stashData.tabs.Count < wantedTabCount)
        {
            stashData.tabs.Add(new StashTabData
            {
                tabName = $"Tab {stashData.tabs.Count + 1}",
                itemsJson = CreateEmptySlots()
            });
        }

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

        if (CurrentTabIndex < 0 || CurrentTabIndex >= stashData.tabs.Count)
            CurrentTabIndex = 0;
    }

    [Server]
    private void EnsureTabSlots(StashTabData tab)
    {
        if (tab.itemsJson == null)
            tab.itemsJson = new List<string>();

        while (tab.itemsJson.Count < slotsPerTab)
            tab.itemsJson.Add("");

        while (tab.itemsJson.Count > slotsPerTab)
            tab.itemsJson.RemoveAt(tab.itemsJson.Count - 1);
    }

    private List<string> CreateEmptySlots()
    {
        List<string> slots = new List<string>();

        for (int i = 0; i < slotsPerTab; i++)
            slots.Add("");

        return slots;
    }

    [Server]
    private void RefreshSyncedTabNames()
    {
        TabNames.Clear();

        for (int i = 0; i < stashData.tabs.Count; i++)
        {
            string name = stashData.tabs[i].tabName;

            if (string.IsNullOrWhiteSpace(name))
                name = $"Tab {i + 1}";

            TabNames.Add(name);
        }

        OnTabsChanged?.Invoke();
    }

    [Server]
    private void RefreshSyncedCurrentTab()
    {
        CurrentTabItemsJson.Clear();

        if (!IsValidTabIndex(CurrentTabIndex))
            return;

        StashTabData tab = stashData.tabs[CurrentTabIndex];
        EnsureTabSlots(tab);

        for (int i = 0; i < tab.itemsJson.Count; i++)
        {
            string slotJson = tab.itemsJson[i];

            if (IsEmptySlot(slotJson))
                CurrentTabItemsJson.Add("");
            else
                CurrentTabItemsJson.Add(slotJson);
        }

        OnStashChanged?.Invoke();
    }

    private bool IsValidTabIndex(int tabIndex)
    {
        return stashData != null &&
               stashData.tabs != null &&
               tabIndex >= 0 &&
               tabIndex < stashData.tabs.Count;
    }

    private ItemInstance GetItemFromTab(int tabIndex, int slotIndex)
    {
        if (!IsValidTabIndex(tabIndex))
            return default;

        StashTabData tab = stashData.tabs[tabIndex];

        if (tab.itemsJson == null)
            return default;

        if (slotIndex < 0 || slotIndex >= tab.itemsJson.Count)
            return default;

        if (IsEmptySlot(tab.itemsJson[slotIndex]))
            return default;

        return DeserializeItem(tab.itemsJson[slotIndex]);
    }

    private string SanitizeTabName(string tabName)
    {
        if (string.IsNullOrWhiteSpace(tabName))
            return $"Tab {stashData.tabs.Count + 1}";

        tabName = tabName.Trim();

        if (tabName.Length > 24)
            tabName = tabName.Substring(0, 24);

        return tabName;
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
            Debug.LogError($"[Stash] Deserialize failed. json={json} error={e}");
            return default;
        }
    }

    private string SerializeItem(ItemInstance item)
    {
        if (item == null || item.instanceId == 0)
            return "";

        try
        {
            return JsonUtility.ToJson(item);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Stash] Serialize failed. item={item.itemName} error={e}");
            return "";
        }
    }
}