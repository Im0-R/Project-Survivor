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
    public void CmdMoveOrSwapCurrentTab(int from, int to)
    {
        MoveOrSwapCurrentTabServer(from, to);
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

        InventoryItemData inventoryData = inventory.GetSlotDataByIndex(inventoryIndex);
        if (IsInvalidSlotData(inventoryData)) return;

        InventoryItemData stashDataInSlot = DeserializeSlotData(tab.itemsJson[stashIndex]);

        tab.itemsJson[stashIndex] = SerializeSlotData(inventoryData);

        if (!IsInvalidSlotData(stashDataInSlot))
            inventory.SetSlotData(inventoryIndex, stashDataInSlot);
        else
            inventory.SetSlotData(inventoryIndex, null);

        RefreshSyncedCurrentTab();
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

        InventoryItemData stashSlotData = DeserializeSlotData(tab.itemsJson[stashIndex]);
        if (IsInvalidSlotData(stashSlotData)) return;

        InventoryItemData inventorySlotData = inventory.GetSlotDataByIndex(inventoryIndex);

        inventory.SetSlotData(inventoryIndex, stashSlotData);

        if (!IsInvalidSlotData(inventorySlotData))
            tab.itemsJson[stashIndex] = SerializeSlotData(inventorySlotData);
        else
            tab.itemsJson[stashIndex] = "";

        RefreshSyncedCurrentTab();
    }

    [Command]
    public void CmdMoveStashToInventory(int stashIndex)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;

        InventoryItemData stashSlotData = GetCurrentTabSlotDataByIndex(stashIndex);
        if (IsInvalidSlotData(stashSlotData)) return;

        bool added = inventory.AddSlotData(stashSlotData);
        if (!added) return;

        RemoveAtServer(CurrentTabIndex, stashIndex);
        RefreshSyncedCurrentTab();
    }

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
            return false;

        StashTabData tab = new StashTabData
        {
            tabName = SanitizeTabName(tabName),
            itemsJson = CreateEmptySlots()
        };

        stashData.tabs.Add(tab);

        CurrentTabIndex = stashData.tabs.Count - 1;

        RefreshSyncedTabNames();
        RefreshSyncedCurrentTab();

        return true;
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

        if (!IsValidTabIndex(tabIndex)) return;

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
    }

    public int TabCount => TabNames.Count;
    public int CurrentTabSlotCount => CurrentTabItemsJson.Count;

    public InventoryItemData GetCurrentTabSlotDataByIndex(int index)
    {
        if (index < 0 || index >= CurrentTabItemsJson.Count)
            return null;

        if (IsEmptySlot(CurrentTabItemsJson[index]))
            return null;

        return DeserializeSlotData(CurrentTabItemsJson[index]);
    }

    public string GetTabName(int index)
    {
        if (index < 0 || index >= TabNames.Count)
            return "";

        return TabNames[index];
    }

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

        foreach (StashTabData tab in stashData.tabs)
            EnsureTabSlots(tab);

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
            TabNames.Add(stashData.tabs[i].tabName);

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

        foreach (string slotJson in tab.itemsJson)
            CurrentTabItemsJson.Add(IsEmptySlot(slotJson) ? "" : slotJson);

        OnStashChanged?.Invoke();
    }

    private bool IsValidTabIndex(int tabIndex)
    {
        return stashData != null &&
               stashData.tabs != null &&
               tabIndex >= 0 &&
               tabIndex < stashData.tabs.Count;
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

        InventoryItemData data = DeserializeSlotData(json);

        return IsInvalidSlotData(data);
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
        catch (Exception e)
        {
            Debug.LogError($"[Stash] Deserialize slot failed. json={json} error={e}");
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
        catch (Exception e)
        {
            Debug.LogError($"[Stash] Serialize slot failed. error={e}");
            return "";
        }
    }
}