using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    [SerializeField]
    private EquipmentSlot slotType = EquipmentSlot.Any;

    [Header("Context")]
    [SerializeField]
    private BackGroundSlotContext context =
        BackGroundSlotContext.InventoryOrEquipment;

    [Header("Raycast")]
    [Tooltip(
        "Graphic utilisé pour recevoir les drops, même lorsque le slot est vide."
    )]
    [SerializeField]
    private Graphic raycastGraphic;

    public int Id => id;
    public EquipmentSlot SlotType => slotType;
    public BackGroundSlotContext Context => context;

    public bool IsEquipmentSlot =>
        slotType != EquipmentSlot.Any;

    public bool IsTradeSlot =>
        context == BackGroundSlotContext.TradeSelf ||
        context == BackGroundSlotContext.TradeOther;

    public bool IsSelfTradeSlot =>
        context == BackGroundSlotContext.TradeSelf;

    public bool IsOtherTradeSlot =>
        context == BackGroundSlotContext.TradeOther;

    private void Awake()
    {
        EnsureRaycastGraphic();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (raycastGraphic == null)
            raycastGraphic = GetComponent<Graphic>();

        if (raycastGraphic != null)
            raycastGraphic.raycastTarget = true;
    }
#endif

    private void EnsureRaycastGraphic()
    {
        if (raycastGraphic == null)
            raycastGraphic = GetComponent<Graphic>();

        /*
         * Un GameObject UI sans Graphic ne peut pas recevoir
         * les événements de drop lorsqu'il est vide.
         */
        if (raycastGraphic == null)
        {
            Image image = gameObject.AddComponent<Image>();

            /*
             * Image presque transparente.
             * Elle reste détectable par le GraphicRaycaster.
             */
            image.color = new Color(
                1f,
                1f,
                1f,
                0.001f
            );

            image.raycastTarget = true;
            raycastGraphic = image;

            Debug.LogWarning(
                $"[BackGroundSlot] Added transparent Image " +
                $"to empty slot {name} so it can receive drops.",
                this
            );
        }
        else
        {
            raycastGraphic.raycastTarget = true;
        }
    }

    public void SetId(int newId)
    {
        id = newId;
    }

    public void SetContext(
        BackGroundSlotContext newContext)
    {
        context = newContext;
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemCard card =
            eventData.pointerDrag != null
                ? eventData.pointerDrag
                    .GetComponentInParent<ItemCard>()
                : null;

        if (card == null)
        {
            Debug.LogWarning(
                $"[BackGroundSlot] Drop on {name}, " +
                "but no ItemCard was found.",
                this
            );

            return;
        }

        Debug.Log(
            $"[BackGroundSlot] OnDrop | " +
            $"slot={name} | " +
            $"id={id} | " +
            $"slotType={slotType} | " +
            $"context={context} | " +
            $"cardSource={card.Source}",
            this
        );

        if (context == BackGroundSlotContext.TradeSelf)
        {
            if (CanvasTrade.Instance == null)
                return;

            CanvasTrade.Instance.RequestDrop(
                card,
                this
            );

            return;
        }

        if (context == BackGroundSlotContext.TradeOther)
            return;

        if (card.Source == ItemCardSource.TradeSelf)
        {
            if (CanvasTrade.Instance != null &&
                CanvasTrade.Instance.HasActiveTrade)
            {
                CanvasTrade.Instance
                    .RequestRemoveSelfOfferSlot(
                        card.SlotIndex
                    );
            }

            return;
        }

        if (CanvasInventory.Instance == null)
        {
            Debug.LogError(
                "[BackGroundSlot] CanvasInventory.Instance is null.",
                this
            );

            return;
        }

        CanvasInventory.Instance.RequestDrop(
            card,
            this
        );
    }
}