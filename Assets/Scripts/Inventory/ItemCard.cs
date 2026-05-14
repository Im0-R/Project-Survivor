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
    IPointerExitHandler
{
    [SerializeField] private ItemInstance itemInstance;

    [Header("Visuals")]
    [SerializeField] private Image rarityImage;
    [SerializeField] private Image baseImage;

    public int SlotIndex { get; private set; } = -1;
    public ItemCardSource Source { get; private set; } = ItemCardSource.Inventory;

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetSlotIndex(int idx)
    {
        SlotIndex = idx;
    }

    public void SetSource(ItemCardSource source)
    {
        Source = source;
    }

    public void SetItemInstance(ItemInstance item)
    {
        itemInstance = item;
        ChangeVisual();
    }

    public ItemInstance GetItemInstance()
    {
        return itemInstance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemInstance == null || itemInstance.instanceId == 0)
            return;

        if (Source == ItemCardSource.Equipment)
        {
            Debug.Log("[ItemCard] Drag from equipment disabled for now.");
            return;
        }

        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;

        Transform dragRoot = GetDragRoot();
        if (dragRoot != null)
            transform.SetParent(dragRoot, true);

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemInstance == null || itemInstance.instanceId == 0)
            return;

        if (canvasGroup.blocksRaycasts)
            return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        Transform dragRoot = GetDragRoot();

        if (dragRoot != null && transform.parent == dragRoot)
        {
            transform.SetParent(originalParent, true);

            if (rectTransform != null)
                rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private Transform GetDragRoot()
    {
        if (CanvasInventory.Instance != null && CanvasInventory.Instance.DragRoot != null)
            return CanvasInventory.Instance.DragRoot;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            return parentCanvas.transform;

        return transform.root;
    }

    public void ChangeVisual()
    {
        if (itemInstance == null || itemInstance.instanceId == 0)
        {
            if (baseImage != null)
                baseImage.sprite = null;

            if (rarityImage != null)
                rarityImage.color = Color.white;

            return;
        }

        ItemBaseSO baseSO = ItemDatabase.GetBase(itemInstance.baseId);

        if (baseSO != null && baseImage != null)
            baseImage.sprite = baseSO.Icon;

        if (rarityImage == null)
            return;

        switch (itemInstance.rarity)
        {
            case ItemRarity.Normal:
                rarityImage.color = Color.gray;
                break;

            case ItemRarity.Magic:
                rarityImage.color = Color.blue;
                break;

            case ItemRarity.Rare:
                rarityImage.color = Color.yellow;
                break;

            case ItemRarity.Unique:
                rarityImage.color = Color.orange;
                break;

            default:
                rarityImage.color = Color.white;
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInstance == null || itemInstance.instanceId == 0)
            return;

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.InitPreview(itemInstance);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }
}