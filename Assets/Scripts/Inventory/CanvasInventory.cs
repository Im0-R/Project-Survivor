using System.Linq;
using UnityEngine;

public class CanvasInventory : MonoBehaviour
{
    public static CanvasInventory Instance { get; private set; }

    public PlayerInventory LocalInventory;
    public PlayerEquipment LocalEquipment;

    public Transform DragRoot;

    [Header("Inventory Slots ONLY")]
    [SerializeField] private BackGroundSlot[] inventorySlots;

    [Header("Equipment Slots ONLY")]
    [SerializeField] private BackGroundSlot[] equipmentSlots;

    [SerializeField] private GameObject itemCardPrefab;

    private void Awake()
    {
        Instance = this;

        if (inventorySlots == null || inventorySlots.Length == 0)
        {
            inventorySlots = GetComponentsInChildren<BackGroundSlot>(true)
                .Where(s => s.SlotType == EquipmentSlot.Any)
                .ToArray();
        }

        if (equipmentSlots == null || equipmentSlots.Length == 0)
        {
            equipmentSlots = GetComponentsInChildren<BackGroundSlot>(true)
                .Where(s => s.SlotType != EquipmentSlot.Any)
                .ToArray();
        }

        for (int i = 0; i < inventorySlots.Length; i++)
            inventorySlots[i].SetId(i);

        foreach (var equipSlot in equipmentSlots)
            equipSlot.SetId(-1);

        gameObject.SetActive(false);

        Debug.Log($"[CanvasInventory] InventorySlots={inventorySlots.Length} EquipmentSlots={equipmentSlots.Length}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            UIManager.Instance.HideSpellsRewardUI();
    }

    public void Bind(PlayerInventory inv)
    {
        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged -= RefreshAll;

        if (LocalEquipment != null)
            LocalEquipment.OnEquipmentChangedEvent -= RefreshAll;

        LocalInventory = inv;
        LocalEquipment = inv != null ? inv.GetComponent<PlayerEquipment>() : null;

        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged += RefreshAll;

        if (LocalEquipment != null)
            LocalEquipment.OnEquipmentChangedEvent += RefreshAll;

        RefreshAll();
    }

    private void RefreshAll()
    {
        PopulateInventory();
        PopulateEquipment();
    }

    private void PopulateEquipment()
    {
        ClearEquipmentCards();

        if (LocalEquipment == null) return;

        CreateEquipmentCard(EquipmentSlot.Weapon);
        CreateEquipmentCard(EquipmentSlot.Helmet);
        CreateEquipmentCard(EquipmentSlot.Chest);
        CreateEquipmentCard(EquipmentSlot.Boots);
    }

    private void CreateEquipmentCard(EquipmentSlot slot)
    {
        BackGroundSlot uiSlot = GetEquipmentUiSlot(slot);
        if (uiSlot == null) return;

        ItemInstance item = LocalEquipment.GetEquippedItem(slot);
        if (item == null || item.instanceId == 0) return;

        CreateCard(item, uiSlot.transform, -1, ItemCardSource.Equipment);
    }

    private BackGroundSlot GetEquipmentUiSlot(EquipmentSlot slot)
    {
        foreach (var s in equipmentSlots)
        {
            if (s != null && s.SlotType == slot)
                return s;
        }

        return null;
    }

    private void CreateCard(ItemInstance item, Transform parent, int slotIndex, ItemCardSource source)
    {
        GameObject cardObj = Instantiate(itemCardPrefab, parent);

        ItemCard card = cardObj.GetComponent<ItemCard>();
        card.SetItemInstance(item);
        card.SetSlotIndex(slotIndex);
        card.SetSource(source);

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void ClearEquipmentCards()
    {
        foreach (var slot in equipmentSlots)
        {
            if (slot == null) continue;

            ItemCard[] cards = slot.GetComponentsInChildren<ItemCard>(true);

            foreach (var card in cards)
                Destroy(card.gameObject);
        }
    }

    public void PopulateInventory()
    {
        ClearInventoryCards();

        if (LocalInventory == null) return;

        int count = Mathf.Min(LocalInventory.ItemsJson.Count, inventorySlots.Length);

        for (int i = 0; i < count; i++)
        {
            string json = LocalInventory.ItemsJson[i];
            if (string.IsNullOrWhiteSpace(json)) continue;

            ItemInstance item = JsonUtility.FromJson<ItemInstance>(json);

            if (item == null || item.instanceId == 0)
            {
                Debug.LogWarning($"[CanvasInventory] Invalid item at slot {i}, json={json}");
                continue;
            }

            ItemBaseSO baseSO = ItemDatabase.GetBase(item.baseId);

            if (baseSO == null)
            {
                Debug.LogWarning($"[CanvasInventory] Missing ItemBaseSO for slot {i}, baseId={item.baseId}, item={item.itemName}");
                continue;
            }

            CreateCard(item, inventorySlots[i].transform, i, ItemCardSource.Inventory);
        }
    }

    private void ClearInventoryCards()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;

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

        if (card.Source == ItemCardSource.Stash)
        {
            RequestMoveStashToInventory(card, targetSlot);
            return;
        }

        if (targetSlot.IsEquipmentSlot)
        {
            RequestEquip(card, targetSlot.SlotType);
        }
        else
        {
            RequestMoveInventory(card, targetSlot.Id);
        }
    }

    private void RequestMoveStashToInventory(ItemCard card, BackGroundSlot targetSlot)
    {
        if (targetSlot.IsEquipmentSlot)
        {
            Debug.LogWarning("[CanvasInventory] Cannot equip directly from stash.");
            return;
        }

        PlayerStash stash = PlayerUI.Instance.playerEnt.GetComponent<PlayerStash>();

        if (stash == null)
        {
            Debug.LogError("[CanvasInventory] PlayerStash missing on player.");
            return;
        }

        stash.CmdMoveCurrentStashToInventorySlot(card.SlotIndex, targetSlot.Id);
    }

    private void RequestEquip(ItemCard card, EquipmentSlot targetEquipmentSlot)
    {
        if (card.Source != ItemCardSource.Inventory)
            return;

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

    private void RequestMoveInventory(ItemCard card, int toSlotId)
    {
        if (card == null) return;
        if (card.Source != ItemCardSource.Inventory) return;
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