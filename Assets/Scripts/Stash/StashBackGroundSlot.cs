using UnityEngine;
using UnityEngine.EventSystems;

public class StashBackGroundSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int id = -1;

    public int Id => id;

    public void SetId(int newId)
    {
        id = newId;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (CanvasStash.Instance == null)
            return;

        ItemCard card = eventData.pointerDrag?.GetComponent<ItemCard>();
        if (card == null)
            return;

        CanvasStash.Instance.RequestDrop(card, this);
    }
}
