using UnityEngine;
using UnityEngine.EventSystems;

public enum BackGroundSlotContext
{
    InventoryOrEquipment,
    TradeSelf,
    TradeOther
}

public class BackGroundSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int id = -1;

    [Header("Slot type")]
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.Any;

    [Header("Context")]
    [SerializeField] private BackGroundSlotContext context = BackGroundSlotContext.InventoryOrEquipment;

    public int Id => id;
    public EquipmentSlot SlotType => slotType;
    public BackGroundSlotContext Context => context;

    public bool IsEquipmentSlot => slotType != EquipmentSlot.Any;

    public bool IsTradeSlot =>
        context == BackGroundSlotContext.TradeSelf ||
        context == BackGroundSlotContext.TradeOther;

    public bool IsSelfTradeSlot =>
        context == BackGroundSlotContext.TradeSelf;

    public bool IsOtherTradeSlot =>
        context == BackGroundSlotContext.TradeOther;

    public void SetId(int newId)
    {
        id = newId;
    }

    public void SetContext(BackGroundSlotContext newContext)
    {
        context = newContext;
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemCard card = eventData.pointerDrag?.GetComponent<ItemCard>();

        if (card == null)
            return;

        if (context == BackGroundSlotContext.TradeSelf)
        {
            if (CanvasTrade.Instance == null)
                return;

            CanvasTrade.Instance.RequestDrop(card, this);
            return;
        }

        if (context == BackGroundSlotContext.TradeOther)
            return;

        if (card.Source == ItemCardSource.TradeSelf)
        {
            if (CanvasTrade.Instance != null && CanvasTrade.Instance.IsOpen())
                CanvasTrade.Instance.RequestRemoveSelfOfferSlot(card.SlotIndex);

            return;
        }

        if (CanvasInventory.Instance == null)
            return;

        CanvasInventory.Instance.RequestDrop(card, this);
    }
}