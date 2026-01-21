using Mirror.BouncyCastle.Asn1.Mozilla;
using UnityEngine;
using UnityEngine.UI;

public class CanvasInventory : MonoBehaviour
{
    //Canvas used to display the inventory UI with PlayerInventory data's items
    //singleton pattern
    public static CanvasInventory Instance { get; private set; }

    
    
    public Transform DragRoot;

    private BackGroundSlot[] slots;
    private void Awake()
    {
        Instance = this;

        slots = GetComponentsInChildren<BackGroundSlot>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetId(i);
    }
    [SerializeField]
    private GameObject itemCardPrefab;

    // Getters

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance.HideSpellsRewardUI();
        }

    }


    public void PopulateInventory()
    {
        ClearCards();

        var inv = PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>();

        for (int i = 0; i < 40; i++)
        {
            var json = inv.ItemsJson[i];
            if (string.IsNullOrEmpty(json)) continue;

            var item = JsonUtility.FromJson<ItemInstance>(json);

            var cardObj = Instantiate(itemCardPrefab, slots[i].transform);
            var card = cardObj.GetComponent<ItemCard>();
            card.SetItemInstance(item);
            card.SetSlotIndex(i);

            // Optionnel: snap position
            var rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void ClearCards()
    {
        foreach (var card in GetComponentsInChildren<ItemCard>())
            Destroy(card.gameObject);
    }

    public void RequestMove(ItemCard card, int targetSlotId)
    {
        var inv = PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>();

        inv.CmdMoveOrSwap(card.SlotIndex, targetSlotId);
    }
}
