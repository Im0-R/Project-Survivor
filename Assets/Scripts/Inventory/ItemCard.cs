using UnityEngine;
using UnityEngine.EventSystems;

public class ItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemInstance itemInstance;

    public int SlotIndex { get; private set; }
    private Transform originalParent;
    private CanvasGroup cg;

    public void SetSlotIndex(int idx) => SlotIndex = idx;
    public void SetItemInstance(ItemInstance item) => itemInstance = item;
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
}
