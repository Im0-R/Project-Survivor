using UnityEngine;
using UnityEngine.EventSystems;

public class BackGroundSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int id = -1;

    [Header("Slot type")]
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.Any;

    public int Id => id;
    public EquipmentSlot SlotType => slotType;
    public bool IsEquipmentSlot => slotType != EquipmentSlot.Any;

    public void SetId(int newId) => id = newId;

    public void OnDrop(PointerEventData eventData)
    {
        ItemCard card = eventData.pointerDrag?.GetComponent<ItemCard>();
        if (card == null) return;

        CanvasInventory.Instance.RequestDrop(card, this);
    }
}