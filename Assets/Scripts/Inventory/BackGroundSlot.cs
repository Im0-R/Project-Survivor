using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackGroundSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int id = -1;
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.Any; //Slot type restriction for this slot
    public int Id => id;

    public void SetId(int newId) => id = newId;

    public void OnDrop(PointerEventData eventData)
    {
        if (id < 0) return;
        ItemCard card = eventData.pointerDrag?.GetComponent<ItemCard>();
        if (card == null) return;

        if (slotType != EquipmentSlot.Any)
        {
            EquipmentSlot itemSlot = ItemDatabase.GetBase(card.GetItemInstance().baseId).slot;
            if (itemSlot != slotType)
            {
                Debug.LogWarning("Item cannot be equipped in this slot.");
                return;
            }
        }
        CanvasInventory.Instance.RequestMove(card, id);
    }
}