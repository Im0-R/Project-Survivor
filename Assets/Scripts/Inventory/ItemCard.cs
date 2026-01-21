using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemInstance itemInstance;

    public int SlotIndex { get; private set; }
    private Transform originalParent;
    public void SetSlotIndex(int idx) => SlotIndex = idx;
    public void SetItemInstance(ItemInstance item) => itemInstance = item;
    public ItemInstance GetItemInstance()
    {
        return itemInstance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(CanvasInventory.Instance.DragRoot, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Si drop a été accepté, le serveur va sync et on rebuild.
        // Sinon on snap back visuellement:
        if (transform.parent == CanvasInventory.Instance.DragRoot)
        {
            transform.SetParent(originalParent, true);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }
    public void DebugLogItemInstance()
    {
        if (itemInstance != null)
        {
            Debug.Log($"ItemInstance ID: {itemInstance.baseId}, Name: {itemInstance.rarity}");
        }
        else
        {
            Debug.Log("ItemInstance is null.");
        }
    }

}
