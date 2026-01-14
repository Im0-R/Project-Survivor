using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ItemInstance itemInstance;

    public void SetItemInstance(ItemInstance item)
    {
        itemInstance = item;
    }
    public ItemInstance GetItemInstance()
    {
        return itemInstance;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (CanvasInventory.Instance.GetTargetCard() == null)
        {
            CanvasInventory.Instance.SetTargetCard(this);
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
