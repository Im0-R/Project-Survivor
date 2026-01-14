using UnityEngine;
using UnityEngine.EventSystems;

public class BackGroundSlot : MonoBehaviour, IPointerClickHandler
{
     [SerializeField] private int id = -1;

    public void OnPointerClick(PointerEventData eventData)
    {
        ItemCard targetCard = CanvasInventory.Instance.GetTargetCard();
        if (targetCard != null)
        {
            CanvasInventory.Instance.ResetTargetCard();
            targetCard.transform.position = transform.position;
        }
    }

    public void SetId(int newId)
    {
        id = newId;
    }
    public int GetId()
    {
        return id;
    }
}
