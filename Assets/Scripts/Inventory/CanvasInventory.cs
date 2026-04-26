using UnityEngine;

public class CanvasInventory : MonoBehaviour
{
    public static CanvasInventory Instance { get; private set; }

    public PlayerInventory LocalInventory;

    public Transform DragRoot;

    [Header("Inventory Slots ONLY")]
    [SerializeField] private BackGroundSlot[] inventorySlots;

    [Header("Equipment Slots ONLY")]
    [SerializeField] private BackGroundSlot[] equipmentSlots;

    [SerializeField] private GameObject itemCardPrefab;

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < inventorySlots.Length; i++)
            inventorySlots[i].SetId(i);

        foreach (var equipSlot in equipmentSlots)
            equipSlot.SetId(-1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            UIManager.Instance.HideSpellsRewardUI();
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
        ClearInventoryCards();

        if (LocalInventory == null) return;

        int count = Mathf.Min(LocalInventory.ItemsJson.Count, inventorySlots.Length);

        for (int i = 0; i < count; i++)
        {
            string json = LocalInventory.ItemsJson[i];
            if (string.IsNullOrEmpty(json)) continue;

            ItemInstance item = JsonUtility.FromJson<ItemInstance>(json);
            if (item == null) continue;

            GameObject cardObj = Instantiate(itemCardPrefab, inventorySlots[i].transform);

            ItemCard card = cardObj.GetComponent<ItemCard>();
            card.SetItemInstance(item);
            card.SetSlotIndex(i);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void ClearInventoryCards()
    {
        foreach (var slot in inventorySlots)
        {
            ItemCard[] cards = slot.GetComponentsInChildren<ItemCard>(true);

            foreach (var card in cards)
                Destroy(card.gameObject);
        }
    }

    public void RequestDrop(ItemCard card, BackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null) return;

        if (PlayerUI.Instance == null || PlayerUI.Instance.playerEnt == null)
            return;

        if (targetSlot.IsEquipmentSlot)
        {
            RequestEquip(card, targetSlot.SlotType);
        }
        else
        {
            RequestMove(card, targetSlot.Id);
        }
    }

    private void RequestEquip(ItemCard card, EquipmentSlot targetEquipmentSlot)
    {
        ItemInstance item = card.GetItemInstance();
        if (item == null || item.instanceId == 0) return;

        ItemBaseSO baseSO = ItemDatabase.GetBase(item.baseId);
        if (baseSO == null) return;

        if (baseSO.SlotType != targetEquipmentSlot)
        {
            Debug.LogWarning($"[CanvasInventory] Cannot equip {item.itemName} in {targetEquipmentSlot}");
            return;
        }

        PlayerEquipment equipment = PlayerUI.Instance.playerEnt.GetComponent<PlayerEquipment>();

        if (equipment == null)
        {
            Debug.LogError("[CanvasInventory] PlayerEquipment missing on player");
            return;
        }

        equipment.CmdEquipFromInventoryIndex(card.SlotIndex);
    }

    private void RequestMove(ItemCard card, int toSlotId)
    {
        if (card == null) return;
        if (toSlotId < 0 || toSlotId >= inventorySlots.Length) return;

        PlayerInventory inv = PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>();

        if (inv == null)
        {
            Debug.LogError("[CanvasInventory] PlayerInventory missing on player");
            return;
        }

        int from = card.SlotIndex;
        int to = toSlotId;

        if (from < 0 || to < 0 || from == to) return;

        inv.CmdMoveOrSwap(from, to);
    }
}