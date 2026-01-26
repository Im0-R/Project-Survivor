using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemInstance itemInstance;

    public int SlotIndex { get; private set; }
    private Transform originalParent;
    private CanvasGroup cg;

    [SerializeField] private Image rarityImage;
    [SerializeField] private Image baseImage;

    public void SetSlotIndex(int idx) => SlotIndex = idx;
    public void SetItemInstance(ItemInstance item)
    {

        itemInstance = item;
        ChangeVisual();
    }
    public ItemInstance GetItemInstance() => itemInstance;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        cg.blocksRaycasts = false;
        transform.SetParent(CanvasInventory.Instance.DragRoot, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        if (transform.parent == CanvasInventory.Instance.DragRoot)
        {
            transform.SetParent(originalParent, true);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    public void ChangeVisual()
    {

        // Get icon from item instance's base

        baseImage.sprite = ItemDatabase.GetBase(itemInstance.baseId).icon;
        // Change rarity color
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
}
