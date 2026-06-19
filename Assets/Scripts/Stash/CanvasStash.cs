using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

    private void OnDestroy()
    {
        UnsubscribeFromCurrentStash();

        if (addTabButton != null)
            addTabButton.onClick.RemoveListener(CreateTab);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (Instance == this)
            Instance = null;
    }

    public void Open(PlayerStash stash, PlayerInventory inventory)
    {
        UnsubscribeFromCurrentStash();

        currentStash = stash;
        currentInventory = inventory;

        SubscribeToCurrentStash();

        if (root != null)
            root.SetActive(true);

        if (CanvasInventory.Instance != null)
            CanvasInventory.Instance.gameObject.SetActive(true);

        ClearStashCards();
        ClearTabButtons();
        UpdateAddTabButtonVisibility();

        currentStash?.CmdRequestOpenStash();
    }

    public void Close()
    {
        if (currentStash != null && currentStash.isOwned)
            currentStash.CmdCloseStash();

        UnsubscribeFromCurrentStash();

        currentStash = null;
        currentInventory = null;

        ClearStashCards();
        ClearTabButtons();
        UpdateAddTabButtonVisibility();

        if (root != null)
            root.SetActive(false);

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }

    private void SubscribeToCurrentStash()
    {
        if (currentStash == null)
            return;

        currentStash.OnStashChanged += PopulateStash;
        currentStash.OnStashSlotChanged += RefreshSlot;
        currentStash.OnTabsChanged += RefreshTabs;
    }

    private void UnsubscribeFromCurrentStash()
    {
        if (currentStash == null)
            return;

        currentStash.OnStashChanged -= PopulateStash;
        currentStash.OnStashSlotChanged -= RefreshSlot;
        currentStash.OnTabsChanged -= RefreshTabs;
    }

    private void RefreshTabs()
    {
        ClearTabButtons();

        if (currentStash == null || tabsParent == null || tabButtonPrefab == null)
        {
            UpdateAddTabButtonVisibility();
            return;
        }

        for (int i = 0; i < currentStash.TabCount; i++)
        {
            int tabIndex = i;
            Button button = Instantiate(tabButtonPrefab, tabsParent);

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = currentStash.GetTabName(tabIndex);

            button.interactable = tabIndex != currentStash.CurrentTabIndex;
            button.onClick.AddListener(() => currentStash.CmdOpenTab(tabIndex));
        }

        UpdateAddTabButtonVisibility();
    }

    private void UpdateAddTabButtonVisibility()
    {
        if (addTabButton == null)
            return;

        addTabButton.gameObject.SetActive(
            currentStash != null && currentStash.CanCreateMoreTabs);
    }

    private void PopulateStash()
    {
        ClearStashCards();

        if (currentStash == null)
            return;

        int count = Mathf.Min(currentStash.CurrentTabSlotCount, stashSlots.Length);

        for (int i = 0; i < count; i++)
            CreateOrRefreshCard(i);
    }

    private void RefreshSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= stashSlots.Length)
            return;

        ClearSlotCard(slotIndex);
        CreateOrRefreshCard(slotIndex);
    }

    private void CreateOrRefreshCard(int slotIndex)
    {
        if (currentStash == null)
            return;

        if (slotIndex < 0 || slotIndex >= stashSlots.Length)
            return;

        InventoryItemData data = currentStash.GetCurrentTabSlotDataByIndex(slotIndex);

        if (data == null || data.lootableId == 0 || data.amount <= 0)
            return;

        GameObject cardObject = Instantiate(itemCardPrefab, stashSlots[slotIndex].transform);

        ItemCard card = cardObject.GetComponent<ItemCard>();
        if (card == null)
        {
            Debug.LogError("[CanvasStash] itemCardPrefab has no ItemCard component.");
            Destroy(cardObject);
            return;
        }

        card.SetInventoryItemData(data);
        card.SetSlotIndex(slotIndex);
        card.SetSource(ItemCardSource.Stash);

        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }

    private void ClearSlotCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= stashSlots.Length)
            return;

        ItemCard[] cards = stashSlots[slotIndex].GetComponentsInChildren<ItemCard>(true);

        foreach (ItemCard card in cards)
            Destroy(card.gameObject);
    }

    private void ClearStashCards()
    {
        foreach (StashBackGroundSlot slot in stashSlots)
        {
            if (slot == null)
                continue;

            ItemCard[] cards = slot.GetComponentsInChildren<ItemCard>(true);

            foreach (ItemCard card in cards)
                Destroy(card.gameObject);
        }
    }

    private void ClearTabButtons()
    {
        if (tabsParent == null)
            return;

        Transform addButtonTransform = addTabButton != null
            ? addTabButton.transform
            : null;

        for (int i = tabsParent.childCount - 1; i >= 0; i--)
        {
            Transform child = tabsParent.GetChild(i);

            if (child == addButtonTransform)
                continue;

            Destroy(child.gameObject);
        }
    }

    public void RequestDrop(ItemCard card, StashBackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null || currentStash == null)
            return;

        int targetStashSlot = targetSlot.Id;

        if (card.Source == ItemCardSource.Inventory)
        {
            currentStash.CmdMoveInventoryToCurrentStashSlot(
                card.SlotIndex,
                targetStashSlot);
            return;
        }

        if (card.Source == ItemCardSource.Stash)
        {
            currentStash.CmdMoveOrSwapCurrentTab(
                card.SlotIndex,
                targetStashSlot);
        }
    }

    private void CreateTab()
    {
        if (currentStash == null)
            return;

        if (!currentStash.CanCreateMoreTabs)
        {
            UpdateAddTabButtonVisibility();
            return;
        }

        int nextIndex = currentStash.TabCount + 1;
        currentStash.CmdCreateTab($"Tab {nextIndex}");
    }
}
