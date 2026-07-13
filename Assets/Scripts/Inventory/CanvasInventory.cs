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

        if (inventorySlots == null ||
            inventorySlots.Length == 0)
        {
            inventorySlots =
                GetComponentsInChildren<BackGroundSlot>(true)
                    .Where(slot =>
                        slot != null &&
                        slot.SlotType == EquipmentSlot.Any)
                    .ToArray();
        }

        if (equipmentSlots == null ||
            equipmentSlots.Length == 0)
        {
            equipmentSlots =
                GetComponentsInChildren<BackGroundSlot>(true)
                    .Where(slot =>
                        slot != null &&
                        slot.SlotType != EquipmentSlot.Any)
                    .ToArray();
        }

        for (int i = 0;
             i < inventorySlots.Length;
             i++)
        {
            if (inventorySlots[i] != null)
                inventorySlots[i].SetId(i);
        }

        foreach (BackGroundSlot equipmentSlot
                 in equipmentSlots)
        {
            if (equipmentSlot != null)
                equipmentSlot.SetId(-1);
        }

        gameObject.SetActive(false);

        Debug.Log(
            $"[CanvasInventory] InventorySlots=" +
            $"{inventorySlots.Length} EquipmentSlots=" +
            $"{equipmentSlots.Length}"
        );
    }

    private void OnEnable()
    {
        ClearOrphanedDragCards();

        if (LocalInventory != null)
            RefreshAll();
    }

    private void OnDisable()
    {
        CancelActiveDragAndDestroy();

        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }

    private void OnDestroy()
    {
        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged -= RefreshAll;

        if (LocalEquipment != null)
            LocalEquipment.OnEquipmentChangedEvent -= RefreshAll;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) &&
            UIManager.Instance != null)
        {
            UIManager.Instance.HideSpellsRewardUI();
        }
    }

    // =========================================================
    // Binding
    // =========================================================

    public void Bind(PlayerInventory inventory)
    {
        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged -= RefreshAll;

        if (LocalEquipment != null)
            LocalEquipment.OnEquipmentChangedEvent -= RefreshAll;

        LocalInventory = inventory;

        LocalEquipment =
            inventory != null
                ? inventory.GetComponent<PlayerEquipment>()
                : null;

        if (LocalInventory != null)
            LocalInventory.OnInventoryChanged += RefreshAll;

        if (LocalEquipment != null)
            LocalEquipment.OnEquipmentChangedEvent += RefreshAll;

        RefreshAll();
    }

    private void RefreshAll()
    {
        CancelActiveDragAndDestroy();

        PopulateInventory();
        PopulateEquipment();
    }

    // =========================================================
    // Inventory population
    // =========================================================

    public void PopulateInventory()
    {
        ClearInventoryCards();

        if (LocalInventory == null)
            return;

        int count = Mathf.Min(
            LocalInventory.ItemsJson.Count,
            inventorySlots.Length
        );

        for (int i = 0; i < count; i++)
        {
            BackGroundSlot inventorySlot =
                inventorySlots[i];

            if (inventorySlot == null)
                continue;

            string json =
                LocalInventory.ItemsJson[i];

            if (string.IsNullOrWhiteSpace(json))
                continue;

            InventoryItemData slotData =
                JsonUtility.FromJson<InventoryItemData>(json);

            if (slotData == null ||
                slotData.lootableId == 0)
            {
                Debug.LogWarning(
                    $"[CanvasInventory] Invalid slot data " +
                    $"at slot {i}, json={json}"
                );

                continue;
            }

            LootableSO lootable =
                LootableDatabase.Get(slotData.lootableId);

            if (lootable == null)
            {
                Debug.LogWarning(
                    $"[CanvasInventory] Missing LootableSO " +
                    $"id={slotData.lootableId}"
                );

                continue;
            }

            CreateLootableCard(
                lootable,
                slotData,
                inventorySlot.transform,
                i,
                ItemCardSource.Inventory
            );
        }
    }

    // =========================================================
    // Equipment population
    // =========================================================

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

    private void CreateEquipmentCard(
        EquipmentSlot slot)
    {
        BackGroundSlot uiSlot =
            GetEquipmentUiSlot(slot);

        if (uiSlot == null)
            return;

        ItemInstance item =
            LocalEquipment.GetEquippedItem(slot);

        if (item == null ||
            item.instanceId == 0)
        {
            return;
        }

        LootableSO lootable =
            LootableDatabase.Get(item.baseId);

        if (lootable == null)
            return;

        InventoryItemData slotData =
            new InventoryItemData
            {
                lootableId = item.baseId,
                lootableType = LootableType.GeneratedItem,
                amount = 1,
                itemJson = JsonUtility.ToJson(item),
                displayNameOverride = item.itemName,
                rarity = item.rarity,
                hasRarityColor = true
            };

        CreateLootableCard(
            lootable,
            slotData,
            uiSlot.transform,
            -1,
            ItemCardSource.Equipment
        );
    }

    private void CreateLootableCard(
        LootableSO lootable,
        InventoryItemData slotData,
        Transform parent,
        int slotIndex,
        ItemCardSource source)
    {
        if (itemCardPrefab == null ||
            parent == null)
        {
            return;
        }

        GameObject cardObject =
            Instantiate(itemCardPrefab, parent);

        ItemCard card =
            cardObject.GetComponent<ItemCard>();

        if (card == null)
        {
            Debug.LogError(
                "[CanvasInventory] ItemCard prefab has no " +
                "ItemCard component."
            );

            Destroy(cardObject);
            return;
        }

        card.SetLootable(lootable, slotData);
        card.SetSlotIndex(slotIndex);
        card.SetSource(source);

        RectTransform rectTransform =
            cardObject.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }
    }

    private BackGroundSlot GetEquipmentUiSlot(
        EquipmentSlot slot)
    {
        foreach (BackGroundSlot equipmentSlot
                 in equipmentSlots)
        {
            if (equipmentSlot != null &&
                equipmentSlot.SlotType == slot)
            {
                return equipmentSlot;
            }
        }

        return null;
    }

    // =========================================================
    // Card cleanup
    // =========================================================

    private void ClearInventoryCards()
    {
        foreach (BackGroundSlot slot in inventorySlots)
        {
            if (slot == null)
                continue;

            ItemCard[] cards =
                slot.GetComponentsInChildren<ItemCard>(true);

            foreach (ItemCard card in cards)
            {
                if (card == null)
                    continue;

                card.gameObject.SetActive(false);
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

            ItemCard[] cards =
                slot.GetComponentsInChildren<ItemCard>(true);

            foreach (ItemCard card in cards)
            {
                if (card == null)
                    continue;

                card.gameObject.SetActive(false);
                Destroy(card.gameObject);
            }
        }
    }

    // =========================================================
    // Drag management
    // =========================================================

    public void RegisterActiveDrag(ItemCard card)
    {
        if (card == null)
            return;

        if (activeDraggedCard != null &&
            activeDraggedCard != card)
        {
            activeDraggedCard.DestroyDragVisual();
        }

        activeDraggedCard = card;
    }

    public void UnregisterActiveDrag(ItemCard card)
    {
        if (activeDraggedCard == card)
            activeDraggedCard = null;
    }

    public void CancelActiveDragAndDestroy()
    {
        ItemCard cardToDestroy =
            activeDraggedCard;

        activeDraggedCard = null;

        if (cardToDestroy != null)
            cardToDestroy.DestroyDragVisual();

        ClearOrphanedDragCards();
    }

    public void CancelActiveDrag()
    {
        CancelActiveDragAndDestroy();
    }

    private void ClearOrphanedDragCards()
    {
        if (DragRoot == null)
            return;

        for (int i = DragRoot.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                DragRoot.GetChild(i);

            if (child == null)
                continue;

            ItemCard card =
                child.GetComponent<ItemCard>();

            if (card == null)
                continue;

            card.DestroyDragVisual();
        }
    }

    // =========================================================
    // Drops
    // =========================================================

    public void RequestDrop(
      ItemCard card,
      BackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null)
            return;

        Debug.Log(
            $"[CanvasInventory] RequestDrop | " +
            $"source={card.Source} | " +
            $"target={targetSlot.Id} | " +
            $"targetType={targetSlot.SlotType}"
        );

        // Stash vers inventaire.
        if (card.Source == ItemCardSource.Stash)
        {
            RequestMoveStashToInventory(
                card,
                targetSlot
            );

            return;
        }

        // Équipement vers inventaire.
        if (card.Source == ItemCardSource.Equipment)
        {
            if (targetSlot.IsEquipmentSlot)
                return;

            RequestUnequipToInventory(
                card,
                targetSlot.Id
            );

            return;
        }

        // Inventaire vers équipement.
        if (targetSlot.IsEquipmentSlot)
        {
            RequestEquip(
                card,
                targetSlot.SlotType
            );

            return;
        }

        // Inventaire vers inventaire.
        RequestMoveInventory(
            card,
            targetSlot.Id
        );
    }

    // =========================================================
    // Unequip request
    // =========================================================

    private void RequestUnequipToInventory(
       ItemCard card,
       int targetInventorySlot)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Equipment)
            return;

        if (targetInventorySlot < 0 ||
            targetInventorySlot >= inventorySlots.Length)
        {
            Debug.LogWarning(
                $"[CanvasInventory] Cannot unequip: " +
                $"invalid inventory slot={targetInventorySlot}, " +
                $"slots={inventorySlots.Length}."
            );

            return;
        }

        ItemInstance equippedItem =
            card.GetItemInstance();

        if (equippedItem == null ||
            equippedItem.instanceId == 0)
        {
            Debug.LogWarning(
                "[CanvasInventory] Cannot unequip: " +
                "the card has no valid ItemInstance."
            );

            return;
        }

        ItemBaseSO itemBase =
            ItemDatabase.GetBase(equippedItem.baseId);

        if (itemBase == null)
        {
            Debug.LogWarning(
                $"[CanvasInventory] Cannot unequip: " +
                $"missing ItemBaseSO for baseId=" +
                $"{equippedItem.baseId}."
            );

            return;
        }

        EquipmentSlot equipmentSlot =
            itemBase.SlotType;

        if (equipmentSlot == EquipmentSlot.None ||
            equipmentSlot == EquipmentSlot.Any)
        {
            Debug.LogWarning(
                $"[CanvasInventory] Cannot unequip: " +
                $"invalid equipment slot={equipmentSlot}."
            );

            return;
        }

        /*
         * On utilise le PlayerEquipment déjà relié
         * à cet inventaire par Bind().
         */
        PlayerEquipment equipment = LocalEquipment;

        if (equipment == null &&
            LocalInventory != null)
        {
            equipment =
                LocalInventory.GetComponent<PlayerEquipment>();
        }

        if (equipment == null &&
            Mirror.NetworkClient.localPlayer != null)
        {
            equipment =
                Mirror.NetworkClient.localPlayer
                    .GetComponent<PlayerEquipment>();
        }

        if (equipment == null)
        {
            Debug.LogError(
                "[CanvasInventory] Cannot unequip: " +
                "no local PlayerEquipment was found."
            );

            return;
        }

        if (!equipment.isOwned)
        {
            Debug.LogError(
                $"[CanvasInventory] Cannot send unequip command: " +
                $"PlayerEquipment is not owned by this client. " +
                $"object={equipment.name}"
            );

            return;
        }

        Debug.Log(
            $"[CanvasInventory] Sending unequip command | " +
            $"equipmentSlot={equipmentSlot} | " +
            $"inventorySlot={targetInventorySlot} | " +
            $"item={equippedItem.itemName}"
        );

        equipment.CmdUnequipToInventoryIndex(
            equipmentSlot,
            targetInventorySlot
        );
    }

    // =========================================================
    // Stash
    // =========================================================

    private void RequestMoveStashToInventory(
        ItemCard card,
        BackGroundSlot targetSlot)
    {
        if (card == null ||
            targetSlot == null)
        {
            return;
        }

        if (targetSlot.IsEquipmentSlot)
        {
            Debug.LogWarning(
                "[CanvasInventory] Cannot equip " +
                "directly from stash."
            );

            return;
        }

        if (targetSlot.Id < 0 ||
            targetSlot.Id >= inventorySlots.Length)
        {
            return;
        }

        PlayerStash stash =
            PlayerUI.Instance.playerEnt
                .GetComponent<PlayerStash>();

        if (stash == null)
        {
            Debug.LogError(
                "[CanvasInventory] PlayerStash " +
                "missing on player."
            );

            return;
        }

        stash.CmdMoveCurrentStashToInventorySlot(
            card.SlotIndex,
            targetSlot.Id
        );
    }

    // =========================================================
    // Delete
    // =========================================================

    public void RequestDelete(ItemCard card)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Inventory)
        {
            Debug.LogWarning(
                "[CanvasInventory] Only inventory " +
                "items can be deleted."
            );

            return;
        }

        if (card.SlotIndex < 0)
            return;

        if (PlayerUI.Instance == null ||
            PlayerUI.Instance.playerEnt == null)
        {
            return;
        }

        PlayerInventory inventory =
            PlayerUI.Instance.playerEnt
                .GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError(
                "[CanvasInventory] PlayerInventory " +
                "missing on player."
            );

            return;
        }

        inventory.CmdDeleteItem(card.SlotIndex);
    }

    // =========================================================
    // Equipment request
    // =========================================================

    private void RequestEquip(
        ItemCard card,
        EquipmentSlot targetEquipmentSlot)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Inventory)
            return;

        LootableSO lootable =
            card.GetLootable();

        if (lootable is not ItemBaseSO itemBase)
        {
            Debug.LogWarning(
                "[CanvasInventory] This lootable " +
                "cannot be equipped."
            );

            return;
        }

        ItemInstance item =
            card.GetItemInstance();

        if (item == null ||
            item.instanceId == 0)
        {
            Debug.LogWarning(
                "[CanvasInventory] Equipment item " +
                "has no valid ItemInstance."
            );

            return;
        }

        if (itemBase.SlotType != targetEquipmentSlot)
        {
            Debug.LogWarning(
                $"[CanvasInventory] Cannot equip " +
                $"{itemBase.DisplayName} in " +
                $"{targetEquipmentSlot}"
            );

            return;
        }

        PlayerEquipment equipment = LocalEquipment;

        if (equipment == null &&
            LocalInventory != null)
        {
            equipment =
                LocalInventory.GetComponent<PlayerEquipment>();
        }

        if (equipment == null)
        {
            Debug.LogError(
                "[CanvasInventory] Local PlayerEquipment is missing."
            );

            return;
        }

        if (equipment == null)
        {
            Debug.LogError(
                "[CanvasInventory] PlayerEquipment " +
                "missing on player."
            );

            return;
        }

        equipment.CmdEquipFromInventoryIndex(
            card.SlotIndex
        );
    }

    // =========================================================
    // Inventory movement
    // =========================================================

    private void RequestMoveInventory(
        ItemCard card,
        int targetSlotId)
    {
        if (card == null)
            return;

        if (card.Source != ItemCardSource.Inventory)
            return;

        if (targetSlotId < 0 ||
            targetSlotId >= inventorySlots.Length)
        {
            return;
        }

        if (PlayerUI.Instance == null ||
            PlayerUI.Instance.playerEnt == null)
        {
            return;
        }

        PlayerInventory inventory =
            PlayerUI.Instance.playerEnt
                .GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError(
                "[CanvasInventory] PlayerInventory " +
                "missing on player."
            );

            return;
        }

        int fromSlot = card.SlotIndex;
        int toSlot = targetSlotId;

        if (fromSlot < 0 ||
            fromSlot == toSlot)
        {
            return;
        }

        inventory.CmdMoveOrSwap(
            fromSlot,
            toSlot
        );
    }
}