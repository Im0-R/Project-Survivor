using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasStash : MonoBehaviour
{
    public static CanvasStash Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Tabs")]
    [SerializeField] private Transform tabsParent;
    [SerializeField] private Button tabButtonPrefab;
    [SerializeField] private Button addTabButton;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Stash Slots")]
    [SerializeField] private StashBackGroundSlot[] stashSlots;

    [Header("Cards")]
    [SerializeField] private GameObject itemCardPrefab;

    private PlayerStash currentStash;
    private PlayerInventory currentInventory;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (stashSlots == null || stashSlots.Length == 0)
        {
            stashSlots = GetComponentsInChildren<StashBackGroundSlot>(true)
                .ToArray();
        }

        for (int i = 0; i < stashSlots.Length; i++)
            stashSlots[i].SetId(i);

        if (addTabButton != null)
            addTabButton.onClick.AddListener(CreateTab);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open(PlayerStash stash, PlayerInventory inventory)
    {
        currentStash = stash;
        currentInventory = inventory;

        if (currentStash != null)
        {
            currentStash.OnStashChanged -= RefreshAll;
            currentStash.OnTabsChanged -= RefreshTabs;

            currentStash.OnStashChanged += RefreshAll;
            currentStash.OnTabsChanged += RefreshTabs;
        }

        if (root != null)
            root.SetActive(true);

        if (CanvasInventory.Instance != null)
            CanvasInventory.Instance.gameObject.SetActive(true);

        RefreshAll();
    }

    public void Close()
    {
        if (currentStash != null)
        {
            currentStash.OnStashChanged -= RefreshAll;
            currentStash.OnTabsChanged -= RefreshTabs;
        }

        currentStash = null;
        currentInventory = null;

        if (root != null)
            root.SetActive(false);
    }

    private void RefreshAll()
    {
        RefreshTabs();
        PopulateStash();
    }

    private void RefreshTabs()
    {
        if (tabsParent == null || tabButtonPrefab == null) return;

        for (int i = tabsParent.childCount - 1; i >= 0; i--)
            Destroy(tabsParent.GetChild(i).gameObject);

        if (currentStash == null) return;

        for (int i = 0; i < currentStash.TabCount; i++)
        {
            int index = i;

            Button button = Instantiate(tabButtonPrefab, tabsParent);

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = currentStash.GetTabName(index);

            button.onClick.AddListener(() =>
            {
                currentStash.CmdOpenTab(index);
            });
        }
    }

    private void PopulateStash()
    {
        ClearStashCards();

        if (currentStash == null) return;

        int count = Mathf.Min(currentStash.CurrentTabSlotCount, stashSlots.Length);

        for (int i = 0; i < count; i++)
        {
            InventoryItemData data = currentStash.GetCurrentTabSlotDataByIndex(i);

            if (data == null || data.lootableId == 0 || data.amount <= 0)
                continue;

            GameObject cardObj = Instantiate(itemCardPrefab, stashSlots[i].transform);

            ItemCard card = cardObj.GetComponent<ItemCard>();
            card.SetInventoryItemData(data);
            card.SetSlotIndex(i);
            card.SetSource(ItemCardSource.Stash);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void ClearStashCards()
    {
        foreach (var slot in stashSlots)
        {
            if (slot == null) continue;

            ItemCard[] cards = slot.GetComponentsInChildren<ItemCard>(true);

            foreach (var card in cards)
                Destroy(card.gameObject);
        }
    }

    public void RequestDrop(ItemCard card, StashBackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null) return;
        if (currentStash == null) return;

        int targetStashSlot = targetSlot.Id;

        if (card.Source == ItemCardSource.Inventory)
        {
            currentStash.CmdMoveInventoryToCurrentStashSlot(card.SlotIndex, targetStashSlot);
            return;
        }

        if (card.Source == ItemCardSource.Stash)
        {
            currentStash.CmdMoveOrSwapCurrentTab(card.SlotIndex, targetStashSlot);
            return;
        }
    }

    private void CreateTab()
    {
        if (currentStash == null) return;

        int nextIndex = currentStash.TabCount + 1;
        currentStash.CmdCreateTab($"Tab {nextIndex}");
    }
}