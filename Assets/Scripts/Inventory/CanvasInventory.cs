using Mirror.BouncyCastle.Asn1.Mozilla;
using UnityEngine;
using UnityEngine.UI;

public class CanvasInventory : MonoBehaviour
{
    //Canvas used to display the inventory UI with PlayerInventory data's items
    //singleton pattern
    public static CanvasInventory Instance { get; private set; }

    public PlayerInventory LocalInventory;

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
    public void Bind(PlayerInventory inv)
    {
        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged -= PopulateInventory;

        LocalInventory = inv;

        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged += PopulateInventory;

        PopulateInventory();
    }

    public void PopulateInventory()
    {
        ClearCards();

        if (LocalInventory == null) return;

        int count = Mathf.Min(LocalInventory.ItemsJson.Count, slots.Length);

        for (int i = 0; i < count; i++)
        {
             string json = LocalInventory.ItemsJson[i];
            if (string.IsNullOrEmpty(json)) continue;

            ItemInstance item = JsonUtility.FromJson<ItemInstance>(json);
            if (item == null) continue;

            GameObject cardObj = Instantiate(itemCardPrefab, slots[i].transform);
            ItemCard card = cardObj.GetComponent<ItemCard>();
            card.SetItemInstance(item);
            card.SetSlotIndex(i);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void ClearCards()
    {
        foreach (var card in GetComponentsInChildren<ItemCard>())
            Destroy(card.gameObject);
    }

    public void RequestMove(ItemCard card, int toSlotId)
    {
        if (card == null) return;
        if (PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>() == null) return;

        int from = card.SlotIndex;
        int to = toSlotId;

        if (from < 0 || to < 0 || from == to) return;

        PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>().CmdMoveOrSwap(from, to);

        card.transform.SetParent(GetSlotTransform(to), false);
        card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
    private Transform GetSlotTransform(int id)
    {
        return slots[id].transform;
    }

}
