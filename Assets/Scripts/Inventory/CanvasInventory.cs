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

    private ItemCard activeDraggedCard;

    private void Awake()
    {
        Instance = this;

        if (inventorySlots == null || inventorySlots.Length == 0)
        {
            inventorySlots = GetComponentsInChildren<BackGroundSlot>(true)
                .Where(s => s != null && s.SlotType == EquipmentSlot.Any)
                .ToArray();
        }

        if (equipmentSlots == null || equipmentSlots.Length == 0)
        {
            equipmentSlots = GetComponentsInChildren<BackGroundSlot>(true)
                .Where(s => s != null && s.SlotType != EquipmentSlot.Any)
                .ToArray();
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
                inventorySlots[i].SetId(i);
        }

        foreach (BackGroundSlot equipSlot in equipmentSlots)
        {
            if (equipSlot != null)
                equipSlot.SetId(-1);
        }

        gameObject.SetActive(false);

        Debug.Log($"[CanvasInventory] InventorySlots={inventorySlots.Length} EquipmentSlots={equipmentSlots.Length}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && UIManager.Instance != null)
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
        CancelActiveDrag();

        PopulateInventory();
        PopulateEquipment();
    }

    public void PopulateInventory()
    {
        ClearInventoryCards();

        if (LocalInventory == null)
            return;

        int count = Mathf.Min(LocalInventory.ItemsJson.Count, inventorySlots.Length);

        for (int i = 0; i < count; i++)
        {
            if (inventorySlots[i] == null)
                continue;

            string json = LocalInventory.ItemsJson[i];

            if (string.IsNullOrWhiteSpace(json))
                continue;

            InventoryItemData slotData = JsonUtility.FromJson<InventoryItemData>(json);

            if (slotData == null || slotData.lootableId == 0)
            {
                Debug.LogWarning($"[CanvasInventory] Invalid slot data at slot {i}, json={json}");
                continue;
            }

            LootableSO lootable = LootableDatabase.Get(slotData.lootableId);

            if (lootable == null)
            {
                Debug.LogWarning($"[CanvasInventory] Missing LootableSO id={slotData.lootableId}");
                continue;
            }

            CreateLootableCard(lootable, slotData, inventorySlots[i].transform, i, ItemCardSource.Inventory);
        }
    }

    private void PopulateEquipment()
    {
        ClearEquipmentCards();

        if (LocalEquipment == null)
            return;

        CreateEquipmentCard(EquipmentSlot.Weapon);
        CreateEquipmentCard(EquipmentSlot.Helmet);
        CreateEquipmentCard(EquipmentSlot.Chest);
        CreateEquipmentCard(EquipmentSlot.Boots);
    }

    private void CreateEquipmentCard(EquipmentSlot slot)
    {
        BackGroundSlot uiSlot = GetEquipmentUiSlot(slot);

        if (uiSlot == null)
            return;

        ItemInstance item = LocalEquipment.GetEquippedItem(slot);

        if (item == null || item.instanceId == 0)
            return;

        LootableSO lootable = LootableDatabase.Get(item.baseId);

        if (lootable == null)
            return;

        InventoryItemData slotData = new InventoryItemData
        {
            lootableId = item.baseId,
            amount = 1,
            itemJson = JsonUtility.ToJson(item),
            displayNameOverride = item.itemName,
            rarity = item.rarity,
            hasRarityColor = true
        };

        CreateLootableCard(lootable, slotData, uiSlot.transform, -1, ItemCardSource.Equipment);
    }

    private void CreateLootableCard(
        LootableSO lootable,
        InventoryItemData slotData,
        Transform parent,
        int slotIndex,
        ItemCardSource source)
    {
        if (itemCardPrefab == null || parent == null)
            return;

        GameObject cardObj = Instantiate(itemCardPrefab, parent);

        ItemCard card = cardObj.GetComponent<ItemCard>();

        if (card == null)
        {
            Debug.LogError("[CanvasInventory] ItemCard prefab has no ItemCard component.");
            Destroy(cardObj);
            return;
        }

        card.SetLootable(lootable, slotData);
        card.SetSlotIndex(slotIndex);
        card.SetSource(source);

        RectTransform rt = cardObj.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private BackGroundSlot GetEquipmentUiSlot(EquipmentSlot slot)
    {
        foreach (BackGroundSlot s in equipmentSlots)
        {
            if (s != null && s.SlotType == slot)
                return s;
        }

        return null;
    }

    private void ClearInventoryCards()
    {
        foreach (BackGroundSlot slot in inventorySlots)
        {
            if (slot == null)
                continue;

            ItemCard[] cards = slot.GetComponentsInChildren<ItemCard>(true);

            foreach (ItemCard card in cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
        }
    }

    private void ClearEquipmentCards()
    {
        foreach (BackGroundSlot slot in equipmentSlots)
        {
            if (slot == null)
                continue;

            ItemCard[] cards = slot.GetComponentsInChildren<ItemCard>(true);

            foreach (ItemCard card in cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
        }
    }

    public void RequestDrop(ItemCard card, BackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null)
            return;

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
            return;
        }

        RequestMoveInventory(card, targetSlot.Id);
    }

    private void RequestMoveStashToInventory(ItemCard card, BackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null)
            return;

        if (targetSlot.IsEquipmentSlot)
        {
            Debug.LogWarning("[CanvasInventory] Cannot equip directly from stash.");
            return;
        }

        if (targetSlot.Id < 0 || targetSlot.Id >= inventorySlots.Length)
            return;

        PlayerStash stash = PlayerUI.Instance.playerEnt.GetComponent<PlayerStash>();

        if (stash == null)
        {
            Debug.LogError("[CanvasInventory] PlayerStash missing on player.");
            return;
        }

        stash.CmdMoveCurrentStashToInventorySlot(card.SlotIndex, targetSlot.Id);
    }

    public void RequestDelete(ItemCard card)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Inventory)
        {
            Debug.LogWarning("[CanvasInventory] Only inventory items can be deleted.");
            return;
        }

        if (card.SlotIndex < 0)
            return;

        if (PlayerUI.Instance == null || PlayerUI.Instance.playerEnt == null)
            return;

        PlayerInventory inv = PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>();

        if (inv == null)
        {
            Debug.LogError("[CanvasInventory] PlayerInventory missing on player.");
            return;
        }

        inv.CmdDeleteItem(card.SlotIndex);
    }

    private void RequestEquip(ItemCard card, EquipmentSlot targetEquipmentSlot)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Inventory)
            return;

        LootableSO lootable = card.GetLootable();

        if (lootable is not ItemBaseSO itemBase)
        {
            Debug.LogWarning("[CanvasInventory] This lootable cannot be equipped.");
            return;
        }

        ItemInstance item = card.GetItemInstance();

        if (item == null || item.instanceId == 0)
        {
            Debug.LogWarning("[CanvasInventory] Equipment item has no valid ItemInstance.");
            return;
        }

        if (itemBase.SlotType != targetEquipmentSlot)
        {
            Debug.LogWarning($"[CanvasInventory] Cannot equip {itemBase.DisplayName} in {targetEquipmentSlot}");
            return;
        }

        PlayerEquipment equipment = PlayerUI.Instance.playerEnt.GetComponent<PlayerEquipment>();

        if (equipment == null)
        {
            Debug.LogError("[CanvasInventory] PlayerEquipment missing on player.");
            return;
        }

        equipment.CmdEquipFromInventoryIndex(card.SlotIndex);
    }

    private void RequestMoveInventory(ItemCard card, int toSlotId)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Inventory)
            return;

        if (toSlotId < 0 || toSlotId >= inventorySlots.Length)
            return;

        if (PlayerUI.Instance == null || PlayerUI.Instance.playerEnt == null)
            return;

        PlayerInventory inv = PlayerUI.Instance.playerEnt.GetComponent<PlayerInventory>();

        if (inv == null)
        {
            Debug.LogError("[CanvasInventory] PlayerInventory missing on player.");
            return;
        }

        int from = card.SlotIndex;
        int to = toSlotId;

        if (from < 0 || from == to)
            return;

        inv.CmdMoveOrSwap(from, to);
    }
    public void RegisterActiveDrag(ItemCard card)
    {
        if (card == null)
            return;

        if (activeDraggedCard != null && activeDraggedCard != card)
            activeDraggedCard.CancelDrag();

        activeDraggedCard = card;
    }

    public void UnregisterActiveDrag(ItemCard card)
    {
        if (activeDraggedCard == card)
            activeDraggedCard = null;
    }

    public void CancelActiveDrag()
    {
        ItemCard cardToCancel = activeDraggedCard;
        activeDraggedCard = null;

        if (cardToCancel != null)
            cardToCancel.CancelDrag();

        ClearDetachedDragCards();
    }

    private void ClearDetachedDragCards()
    {
        if (DragRoot == null)
            return;

        for (int i = DragRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = DragRoot.GetChild(i);
            ItemCard card = child.GetComponent<ItemCard>();

            if (card == null)
                continue;

            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
    }
}