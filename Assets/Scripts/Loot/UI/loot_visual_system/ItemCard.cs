using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    IPointerExitHandler,
    IPointerClickHandler
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

    private bool isDragging;
    private bool isBeingDestroyed;

    public int SlotIndex { get; private set; } = -1;
    public ItemCardSource Source { get; private set; }

    public bool IsDragging => isDragging;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // =========================================================
    // Preview
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotData == null || isDragging)
            return;

        if (ItemPreviewManager.Instance != null)
        {
            ItemPreviewManager.Instance.InitPreview(
                slotData,
                transform as RectTransform
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }

    // =========================================================
    // Initialization
    // =========================================================

    public void SetLootable(
        LootableSO newLootable,
        InventoryItemData newSlotData)
    {
        lootable = newLootable;
        slotData = newSlotData;
        itemInstance = null;

        if (slotData == null)
            return;

        LootVisualStyle style = LootVisualManager.Instance != null
            ? LootVisualManager.Instance.Resolve(slotData)
            : LootVisualStyle.CreateFallback();

        // Le fond reste identique pour toutes les ItemCards.
        if (backgroundImage != null)
            backgroundImage.color = style.itemCardBackgroundColor;

        if (icon != null)
        {
            icon.sprite = lootable != null
                ? lootable.Icon
                : null;

            icon.enabled = icon.sprite != null;
        }

        if (nameText != null)
        {
            if (!string.IsNullOrWhiteSpace(slotData.displayNameOverride))
            {
                nameText.text = slotData.displayNameOverride;
            }
            else if (lootable != null)
            {
                nameText.text = lootable.DisplayName;
            }
            else
            {
                nameText.text = $"Lootable {slotData.lootableId}";
            }

            nameText.color = style.itemCardTextColor;
        }

        if (amountText != null)
        {
            amountText.text = slotData.amount > 1
                ? slotData.amount.ToString()
                : "";
        }

        if (!string.IsNullOrWhiteSpace(slotData.itemJson))
        {
            itemInstance = JsonUtility.FromJson<ItemInstance>(
                slotData.itemJson
            );

            itemInstance?.EnsureLists();
        }
    }

    public void SetItemInstance(ItemInstance item)
    {
        if (item == null)
        {
            itemInstance = null;
            return;
        }

        item.EnsureLists();

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

    public void SetInventoryItemData(InventoryItemData data)
    {
        if (data == null)
        {
            slotData = null;
            lootable = null;
            itemInstance = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(data.itemJson))
        {
            ItemInstance item = JsonUtility.FromJson<ItemInstance>(
                data.itemJson
            );

            if (item != null)
            {
                SetItemInstance(item);
                return;
            }
        }

        LootableSO foundLootable = LootableDatabase.Get(data.lootableId);
        SetLootable(foundLootable, data);
    }

    // =========================================================
    // Clicks and currencies
    // =========================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotData == null || isDragging)
            return;

        if (Source != ItemCardSource.Inventory)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            TrySelectCurrency();
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
            TryUseSelectedCurrencyOnThisItem();
    }

    private void TrySelectCurrency()
    {
        if (CurrencyTargetingManager.Instance == null)
        {
            Debug.LogError(
                "[ItemCard] CurrencyTargetingManager.Instance is null."
            );

            return;
        }

        if (slotData.lootableType != LootableType.Currency &&
            slotData.lootableType != LootableType.Sigil)
        {
            return;
        }

        CurrencySO currency = lootable as CurrencySO;

        if (currency == null)
        {
            Debug.LogWarning(
                "[ItemCard] Selected lootable is not a CurrencySO."
            );

            return;
        }

        if (currency.effect is not ItemCurrencyEffectSO)
        {
            Debug.LogWarning(
                $"[ItemCard] Currency {currency.DisplayName} has no item effect."
            );

            return;
        }

        PlayerInventory inventory = GetLocalInventory();

        if (inventory == null)
        {
            Debug.LogError(
                "[ItemCard] No local PlayerInventory found."
            );

            return;
        }

        CurrencyTargetingManager.Instance.StartTargeting(
            inventory,
            SlotIndex,
            currency
        );

        Debug.Log(
            $"[ItemCard] Selected currency {currency.DisplayName} " +
            $"from slot {SlotIndex}"
        );
    }

    private void TryUseSelectedCurrencyOnThisItem()
    {
        if (CurrencyTargetingManager.Instance == null)
            return;

        if (!CurrencyTargetingManager.Instance.IsTargetingItem)
            return;

        if (slotData.lootableType != LootableType.GeneratedItem &&
            string.IsNullOrWhiteSpace(slotData.itemJson))
        {
            Debug.LogWarning(
                "[ItemCard] Target is not a generated item."
            );

            return;
        }

        CurrencyTargetingManager.Instance.TryUseOnItem(SlotIndex);

        Debug.Log(
            $"[ItemCard] Trying to use selected currency " +
            $"on item slot {SlotIndex}"
        );
    }

    private PlayerInventory GetLocalInventory()
    {
        if (CanvasInventory.Instance != null &&
            CanvasInventory.Instance.LocalInventory != null)
        {
            return CanvasInventory.Instance.LocalInventory;
        }

        if (Mirror.NetworkClient.localPlayer != null)
        {
            return Mirror.NetworkClient.localPlayer
                .GetComponent<PlayerInventory>();
        }

        return null;
    }

    // =========================================================
    // Public data
    // =========================================================

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

    // =========================================================
    // Drag
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isDragging || isBeingDestroyed)
            return;

        originalParent = transform.parent;
        isDragging = true;

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();

        if (CanvasInventory.Instance != null)
        {
            CanvasInventory.Instance.RegisterActiveDrag(this);

            if (CanvasInventory.Instance.DragRoot != null)
            {
                transform.SetParent(
                    CanvasInventory.Instance.DragRoot,
                    true
                );

                transform.SetAsLastSibling();
            }
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isBeingDestroyed)
            return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || isBeingDestroyed)
            return;

        ReturnToOriginalSlot();
    }

    /// <summary>
    /// Utilisé lorsqu'un drag se termine normalement.
    /// La carte retourne temporairement dans son slot.
    /// </summary>
    public void ReturnToOriginalSlot()
    {
        if (isBeingDestroyed)
            return;

        Transform targetParent = originalParent;

        isDragging = false;
        originalParent = null;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (CanvasInventory.Instance != null)
            CanvasInventory.Instance.UnregisterActiveDrag(this);

        if (targetParent == null)
            return;

        transform.SetParent(targetParent, false);
        ResetRectTransform();
    }

    /// <summary>
    /// Utilisé lorsque le Canvas est fermé ou rafraîchi
    /// pendant que cette carte est déplacée.
    /// </summary>
    public void DestroyDragVisual()
    {
        if (isBeingDestroyed)
            return;

        isBeingDestroyed = true;
        isDragging = false;
        originalParent = null;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (CanvasInventory.Instance != null)
            CanvasInventory.Instance.UnregisterActiveDrag(this);

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();

        // Destroy s'effectue en fin de frame,
        // donc on cache immédiatement l'ancienne carte.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void ResetRectTransform()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void OnDisable()
    {
        // Ne pas remettre la carte dans son ancien parent ici.
        // Une fermeture forcée est gérée par CanvasInventory.
        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }

    private void OnDestroy()
    {
        if (CanvasInventory.Instance != null)
            CanvasInventory.Instance.UnregisterActiveDrag(this);
    }
}