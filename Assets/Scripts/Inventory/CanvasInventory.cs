using Mirror.BouncyCastle.Asn1.Mozilla;
using UnityEngine;
using UnityEngine.UI;

public class CanvasInventory : MonoBehaviour
{
    //Canvas used to display the inventory UI with PlayerInventory data's items
    //singleton pattern
    public static CanvasInventory Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        //Get all BackGroundSlot components in children and set id to their index in the array

        foreach (var slot in GetComponentsInChildren<BackGroundSlot>())
        {
            slot.SetId(slot.transform.GetSiblingIndex());
        }
    }
    [SerializeField]
    private ItemCard targetCard;

    // Getters

    public ItemCard GetTargetCard()
    {
        return targetCard;
    }

    public void SetTargetCard(ItemCard itemCard)
    {
        targetCard = itemCard;
        foreach(var slot in targetCard.GetComponentsInChildren<Image>())
        {
            slot.raycastTarget = false;
        }
    }
    public void ResetTargetCard()
    {
        foreach (var slot in targetCard.GetComponentsInChildren<Image>())
        {
            slot.raycastTarget = true;
        }
        if (targetCard != null)
            targetCard = null;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance.HideSpellsRewardUI();
        }



        if (targetCard == null) return;

        targetCard.transform.position = Input.mousePosition;
    }
}
