using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum ItemCardSource
{
    Inventory,
    Equipment,
    Stash
}

public class ItemCard : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;

    private LootableSO lootable;
    private InventoryItemData slotData;
    private ItemInstance itemInstance;

    private Transform originalParent;
    private CanvasGroup canvasGroup;

    public int SlotIndex { get; private set; } = -1;
    public ItemCardSource Source { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetLootable(LootableSO newLootable, InventoryItemData newSlotData)
    {
        lootable = newLootable;
        slotData = newSlotData;
        itemInstance = null;

        if (lootable == null || slotData == null)
            return;

        if (backgroundImage != null)
        {
            if (slotData.hasRarityColor)
                backgroundImage.color = GetRarityColor(slotData.rarity);
            else
                backgroundImage.color = lootable.LabelColor;
        }

        if (icon != null)
        {
            icon.sprite = lootable.Icon;
            icon.enabled = lootable.Icon != null;
        }

        if (nameText != null)
        {
            if (!string.IsNullOrWhiteSpace(slotData.displayNameOverride))
                nameText.text = slotData.displayNameOverride;
            else
                nameText.text = lootable.DisplayName;
        }

        if (amountText != null)
            amountText.text = slotData.amount > 1 ? slotData.amount.ToString() : "";

        if (!string.IsNullOrWhiteSpace(slotData.itemJson))
        {
            itemInstance = JsonUtility.FromJson<ItemInstance>(slotData.itemJson);
            itemInstance?.EnsureLists();
        }
    }

    public void SetItemInstance(ItemInstance item)
    {
        itemInstance = item;

        if (item == null)
            return;

        LootableSO foundLootable = LootableDatabase.Get(item.baseId);

        InventoryItemData generatedData = new InventoryItemData
        {
            lootableId = item.baseId,
            lootableType = LootableType.GeneratedItem,
            amount = 1,
            itemJson = JsonUtility.ToJson(item),
            displayNameOverride = item.itemName,
            rarity = item.rarity,
            hasRarityColor = true
        };

        SetLootable(foundLootable, generatedData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotData == null)
            return;

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.InitPreview(slotData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }

    public LootableSO GetLootable()
    {
        return lootable;
    }

    public InventoryItemData GetSlotData()
    {
        return slotData;
    }

    public ItemInstance GetItemInstance()
    {
        return itemInstance;
    }

    public void SetSlotIndex(int index)
    {
        SlotIndex = index;
    }

    public void SetSource(ItemCardSource source)
    {
        Source = source;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();

        if (CanvasInventory.Instance != null && CanvasInventory.Instance.DragRoot != null)
            transform.SetParent(CanvasInventory.Instance.DragRoot);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (originalParent != null)
            transform.SetParent(originalParent);

        RectTransform rt = GetComponent<RectTransform>();

        if (rt != null)
            rt.anchoredPosition = Vector2.zero;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Normal:
                return new Color(0.5f, 0.5f, 0.5f);

            case ItemRarity.Magic:
                return new Color(0.3f, 0.5f, 1f);

            case ItemRarity.Rare:
                return new Color(1f, 0.85f, 0.2f);

            case ItemRarity.Unique:
                return new Color(1f, 0.5f, 0.1f);

            default:
                return Color.white;
        }
    }
}