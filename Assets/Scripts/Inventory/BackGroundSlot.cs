using UnityEngine;
using UnityEngine.EventSystems;

public class BackGroundSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int id = -1;
    public int Id => id;

    public void SetId(int newId) => id = newId;

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<ItemCard>();
        if (card == null) return;

        CanvasInventory.Instance.RequestMove(card, id);
    }
}