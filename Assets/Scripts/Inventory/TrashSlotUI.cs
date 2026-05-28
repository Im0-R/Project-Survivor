using UnityEngine;
using UnityEngine.EventSystems;

public class TrashSlotUI : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        ItemCard card = eventData.pointerDrag?.GetComponent<ItemCard>();

        if (card == null)
            return;

        if (CanvasInventory.Instance == null)
            return;

        CanvasInventory.Instance.RequestDelete(card);

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }
}