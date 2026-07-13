using Mirror;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnEquipmentChanged))]
    private string weaponJson = "";

    [SyncVar(hook = nameof(OnEquipmentChanged))]
    private string helmetJson = "";

    [SyncVar(hook = nameof(OnEquipmentChanged))]
    private string chestJson = "";

    [SyncVar(hook = nameof(OnEquipmentChanged))]
    private string bootsJson = "";

    public event System.Action OnEquipmentChangedEvent;

    [SyncVar] public float TotalDamage;
    [SyncVar] public float TotalDefense;
    [SyncVar] public float TotalVitality;

    private StatsComponent stats;
    private PlayerInventory inv;

    private void Awake()
    {
        stats = GetComponent<StatsComponent>();
        inv = GetComponent<PlayerInventory>();
    }

    private void OnEquipmentChanged(
        string oldValue,
        string newValue)
    {
        OnEquipmentChangedEvent?.Invoke();
    }

    // =========================================================
    // Equip
    // =========================================================

    [Command]
    public void CmdEquipFromInventoryIndex(int inventoryIndex)
    {
        if (inv == null)
            return;

        if (inventoryIndex < 0 ||
            inventoryIndex >= inv.Count)
        {
            return;
        }

        ITradeInventory tradeInventory =
            inv as ITradeInventory;

        if (tradeInventory != null &&
            tradeInventory.IsTradeSlotLockedServer(inventoryIndex))
        {
            Debug.LogWarning(
                "[PlayerEquipment] Cannot equip item: " +
                "inventory slot is locked by trade."
            );

            return;
        }

        ItemInstance newItem =
            inv.GetItemByIndex(inventoryIndex);

        if (newItem == null ||
            newItem.instanceId == 0)
        {
            return;
        }

        ItemBaseSO baseSO =
            ItemDatabase.GetBase(newItem.baseId);

        if (baseSO == null)
            return;

        EquipmentSlot slot = baseSO.SlotType;

        if (slot == EquipmentSlot.None ||
            slot == EquipmentSlot.Any)
        {
            Debug.LogWarning(
                $"[PlayerEquipment] Cannot equip " +
                $"{newItem.itemName}: invalid slot={slot}"
            );

            return;
        }

        ItemInstance oldEquipped =
            GetEquippedItem(slot);

        bool inventoryUpdated;

        if (oldEquipped != null &&
            oldEquipped.instanceId != 0)
        {
            inventoryUpdated =
                inv.SetSlot(inventoryIndex, oldEquipped);
        }
        else
        {
            inventoryUpdated =
                inv.SetSlot(inventoryIndex, null);
        }

        if (!inventoryUpdated)
        {
            Debug.LogWarning(
                "[PlayerEquipment] Equip cancelled: " +
                "failed to update inventory slot."
            );

            return;
        }

        SetEquippedItem(slot, newItem);

        RecalculateStatsServer();

        Debug.Log(
            $"[PlayerEquipment] Equipped " +
            $"{newItem.itemName} in {slot} " +
            $"from inventory slot {inventoryIndex}"
        );
    }

    // =========================================================
    // Unequip to first free slot
    // =========================================================

    [Command]
    public void CmdUnequip(EquipmentSlot slot)
    {
        Debug.Log(
            $"[PlayerEquipment] CmdUnequip called " +
            $"slot={slot} connection=" +
            $"{connectionToClient?.connectionId}"
        );

        if (inv == null)
            return;

        if (slot == EquipmentSlot.None ||
            slot == EquipmentSlot.Any)
        {
            return;
        }

        ItemInstance equipped =
            GetEquippedItem(slot);

        if (equipped == null ||
            equipped.instanceId == 0)
        {
            return;
        }

        bool added = inv.AddItem(equipped);

        if (!added)
        {
            Debug.LogWarning(
                "[PlayerEquipment] Inventory full, " +
                "cannot unequip."
            );

            return;
        }

        ClearEquippedItem(slot);
        RecalculateStatsServer();

        Debug.Log(
            $"[PlayerEquipment] Unequipped " +
            $"{equipped.itemName} from {slot}"
        );
    }

    // =========================================================
    // Unequip to selected inventory slot
    // =========================================================

    [Command]
    public void CmdUnequipToInventoryIndex(
        EquipmentSlot slot,
        int inventoryIndex)
    {
        if (inv == null)
            return;

        if (slot == EquipmentSlot.None ||
            slot == EquipmentSlot.Any)
        {
            return;
        }

        if (inventoryIndex < 0 ||
            inventoryIndex >= inv.Count)
        {
            Debug.LogWarning(
                $"[PlayerEquipment] Invalid inventory " +
                $"index={inventoryIndex}."
            );

            return;
        }

        ITradeInventory tradeInventory =
            inv as ITradeInventory;

        if (tradeInventory != null &&
            tradeInventory.IsTradeSlotLockedServer(inventoryIndex))
        {
            Debug.LogWarning(
                "[PlayerEquipment] Cannot unequip item: " +
                "target inventory slot is locked by trade."
            );

            return;
        }

        ItemInstance equippedItem =
            GetEquippedItem(slot);

        if (equippedItem == null ||
            equippedItem.instanceId == 0)
        {
            return;
        }

        ItemInstance inventoryItem =
            inv.GetItemByIndex(inventoryIndex);

        bool targetOccupied =
            inventoryItem != null &&
            inventoryItem.instanceId != 0;

        /*
         * Si la case ciblée contient déjà un objet,
         * l'objet doit pouvoir être équipé dans le slot
         * que l'on est en train de vider.
         */
        if (targetOccupied)
        {
            ItemBaseSO inventoryItemBase =
                ItemDatabase.GetBase(inventoryItem.baseId);

            if (inventoryItemBase == null)
            {
                Debug.LogWarning(
                    "[PlayerEquipment] Cannot swap: " +
                    "target item base is missing."
                );

                return;
            }

            if (inventoryItemBase.SlotType != slot)
            {
                Debug.LogWarning(
                    $"[PlayerEquipment] Cannot swap " +
                    $"{slot} with an item of type " +
                    $"{inventoryItemBase.SlotType}."
                );

                return;
            }
        }

        /*
         * On place d'abord l'ancien équipement dans
         * l'inventaire. Si cette opération échoue,
         * l'équipement n'est pas modifié.
         */
        bool inventoryUpdated =
            inv.SetSlot(inventoryIndex, equippedItem);

        if (!inventoryUpdated)
        {
            Debug.LogWarning(
                "[PlayerEquipment] Unequip cancelled: " +
                "failed to update inventory slot."
            );

            return;
        }

        if (targetOccupied)
        {
            // Échange avec un équipement compatible.
            SetEquippedItem(slot, inventoryItem);
        }
        else
        {
            // Déplacement vers une case vide.
            ClearEquippedItem(slot);
        }

        RecalculateStatsServer();

        Debug.Log(
            $"[PlayerEquipment] Moved equipped item " +
            $"{equippedItem.itemName} from {slot} " +
            $"to inventory slot {inventoryIndex}."
        );
    }

    // =========================================================
    // Stats
    // =========================================================

    [Server]
    private void RecalculateStatsServer()
    {
        if (stats == null)
            stats = GetComponent<StatsComponent>();

        if (stats == null)
            return;

        stats.RecalculateFinalStatsServer(this);

        TotalDamage =
            stats.Get(StatId.DamageMult);

        TotalDefense =
            stats.Get(StatId.Armor);

        TotalVitality =
            stats.Get(StatId.MaxHealth);
    }

    [Server]
    public void ApplyEquipmentStatsToServer(
        StatsComponent targetStats)
    {
        if (targetStats == null)
            return;

        ApplyEquippedItem(
            GetEquippedItem(EquipmentSlot.Weapon),
            targetStats
        );

        ApplyEquippedItem(
            GetEquippedItem(EquipmentSlot.Helmet),
            targetStats
        );

        ApplyEquippedItem(
            GetEquippedItem(EquipmentSlot.Chest),
            targetStats
        );

        ApplyEquippedItem(
            GetEquippedItem(EquipmentSlot.Boots),
            targetStats
        );
    }

    [Server]
    private void ApplyEquippedItem(
        ItemInstance inst,
        StatsComponent targetStats)
    {
        if (inst == null ||
            inst.instanceId == 0)
        {
            return;
        }

        ItemBaseSO baseSO =
            ItemDatabase.GetBase(inst.baseId);

        if (baseSO == null)
            return;

        targetStats.AddFinalStatServer(
            StatId.DamageMult,
            baseSO.BaseAttack
        );

        targetStats.AddFinalStatServer(
            StatId.Armor,
            baseSO.BaseDefense
        );

        targetStats.AddFinalStatServer(
            StatId.MaxHealth,
            baseSO.BaseVitality
        );

        ApplyAffixStats(
            inst.prefixes,
            targetStats
        );

        ApplyAffixStats(
            inst.suffixes,
            targetStats
        );
    }

    [Server]
    private void ApplyAffixStats(
        System.Collections.Generic.List<ItemAffix> affixes,
        StatsComponent targetStats)
    {
        if (affixes == null)
            return;

        foreach (ItemAffix affixInstance in affixes)
        {
            AffixSO affixSO =
                AffixDatabase.Get(affixInstance.affixId);

            if (affixSO == null)
                continue;

            targetStats.AddFinalStatServer(
                affixSO.stat,
                affixInstance.value
            );
        }
    }

    // =========================================================
    // Equipment access
    // =========================================================

    public ItemInstance GetEquippedItem(
        EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon =>
                DeserializeItem(weaponJson),

            EquipmentSlot.Helmet =>
                DeserializeItem(helmetJson),

            EquipmentSlot.Chest =>
                DeserializeItem(chestJson),

            EquipmentSlot.Boots =>
                DeserializeItem(bootsJson),

            _ => default
        };
    }

    [Server]
    private void SetEquippedItem(
        EquipmentSlot slot,
        ItemInstance item)
    {
        string json = SerializeItem(item);

        switch (slot)
        {
            case EquipmentSlot.Weapon:
                weaponJson = json;
                break;

            case EquipmentSlot.Helmet:
                helmetJson = json;
                break;

            case EquipmentSlot.Chest:
                chestJson = json;
                break;

            case EquipmentSlot.Boots:
                bootsJson = json;
                break;
        }
    }

    [Server]
    private void ClearEquippedItem(
        EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                weaponJson = "";
                break;

            case EquipmentSlot.Helmet:
                helmetJson = "";
                break;

            case EquipmentSlot.Chest:
                chestJson = "";
                break;

            case EquipmentSlot.Boots:
                bootsJson = "";
                break;
        }
    }

    public bool HasEquipped(
        EquipmentSlot slot)
    {
        ItemInstance item =
            GetEquippedItem(slot);

        return item != null &&
               item.instanceId != 0;
    }

    // =========================================================
    // Serialization
    // =========================================================

    private string SerializeItem(
        ItemInstance item)
    {
        if (item == null ||
            item.instanceId == 0)
        {
            return "";
        }

        return JsonUtility.ToJson(item);
    }

    private ItemInstance DeserializeItem(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        ItemInstance item =
            JsonUtility.FromJson<ItemInstance>(json);

        item?.EnsureLists();

        return item;
    }

    // =========================================================
    // Save
    // =========================================================

    [Server]
    public PlayerEquipmentData GetSaveData()
    {
        return new PlayerEquipmentData
        {
            weaponJson = weaponJson,
            helmetJson = helmetJson,
            chestJson = chestJson,
            bootsJson = bootsJson
        };
    }

    [Server]
    public void LoadSaveData(
        PlayerEquipmentData data)
    {
        Debug.Log(
            $"[PlayerEquipment] LoadSaveData called " +
            $"dataNull={data == null}"
        );

        if (data == null)
        {
            weaponJson = "";
            helmetJson = "";
            chestJson = "";
            bootsJson = "";

            RecalculateStatsServer();
            return;
        }

        weaponJson =
            data.weaponJson ?? "";

        helmetJson =
            data.helmetJson ?? "";

        chestJson =
            data.chestJson ?? "";

        bootsJson =
            data.bootsJson ?? "";

        RecalculateStatsServer();

        Debug.Log(
            "[PlayerEquipment] Equipment loaded from save."
        );
    }

    [Server]
    public void ClearEquipmentServer()
    {
        weaponJson = "";
        helmetJson = "";
        chestJson = "";
        bootsJson = "";

        RecalculateStatsServer();

        OnEquipmentChangedEvent?.Invoke();

        Debug.Log(
            "[PlayerEquipment] Equipment cleared."
        );
    }
}