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

    private void OnEquipmentChanged(string oldValue, string newValue)
    {
        OnEquipmentChangedEvent?.Invoke();
    }

    [Command]
    public void CmdEquipFromInventoryIndex(int inventoryIndex)
    {
        if (inv == null) return;

        ItemInstance newItem = inv.GetItemByIndex(inventoryIndex);
        if (newItem == null || newItem.instanceId == 0) return;

        ItemBaseSO baseSO = ItemDatabase.GetBase(newItem.baseId);
        if (baseSO == null) return;

        EquipmentSlot slot = baseSO.SlotType;

        ItemInstance oldEquipped = GetEquippedItem(slot);

        SetEquippedItem(slot, newItem);

        if (oldEquipped != null && oldEquipped.instanceId != 0)
            inv.SetSlot(inventoryIndex, oldEquipped);
        else
            inv.SetSlot(inventoryIndex, null);

        RecalculateStatsServer();

        Debug.Log($"[PlayerEquipment] Equipped {newItem.itemName} in {slot} from inventory slot {inventoryIndex}");
    }

    [Command]
    public void CmdUnequip(EquipmentSlot slot)
    {
        Debug.LogWarning($"[PlayerEquipment] CmdUnequip CALLED slot={slot} by connection={connectionToClient?.connectionId}");

        if (inv == null) return;

        ItemInstance equipped = GetEquippedItem(slot);
        if (equipped == null || equipped.instanceId == 0) return;

        bool added = inv.AddItem(equipped);
        if (!added)
        {
            Debug.LogWarning("[PlayerEquipment] Inventaire plein, impossible de déséquiper.");
            return;
        }

        ClearEquippedItem(slot);
        RecalculateStatsServer();

        Debug.Log($"[PlayerEquipment] Unequipped {equipped.itemName} from {slot}");
    }

    [Server]
    private void RecalculateStatsServer()
    {
        if (stats == null)
            stats = GetComponent<StatsComponent>();

        if (stats == null) return;

        stats.RecalculateFinalStatsServer(this);

        TotalDamage = stats.Get(StatId.DamageMult);
        TotalDefense = stats.Get(StatId.Armor);
        TotalVitality = stats.Get(StatId.MaxHealth);
    }

    [Server]
    public void ApplyEquipmentStatsToServer(StatsComponent targetStats)
    {
        if (targetStats == null) return;

        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Weapon), targetStats);
        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Helmet), targetStats);
        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Chest), targetStats);
        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Boots), targetStats);
    }

    [Server]
    private void ApplyEquippedItem(ItemInstance inst, StatsComponent targetStats)
    {
        if (inst == null || inst.instanceId == 0) return;

        ItemBaseSO baseSO = ItemDatabase.GetBase(inst.baseId);
        if (baseSO == null) return;

        targetStats.AddFinalStatServer(StatId.DamageMult, baseSO.BaseAttack);
        targetStats.AddFinalStatServer(StatId.Armor, baseSO.BaseDefense);
        targetStats.AddFinalStatServer(StatId.MaxHealth, baseSO.BaseVitality);

        ApplyAffixStats(inst.prefixes, targetStats);
        ApplyAffixStats(inst.suffixes, targetStats);
    }

    public ItemInstance GetEquippedItem(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => DeserializeItem(weaponJson),
            EquipmentSlot.Helmet => DeserializeItem(helmetJson),
            EquipmentSlot.Chest => DeserializeItem(chestJson),
            EquipmentSlot.Boots => DeserializeItem(bootsJson),
            _ => default
        };
    }

    [Server]
    private void SetEquippedItem(EquipmentSlot slot, ItemInstance item)
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
    public void ClearEquipmentServer()
    {
        weaponJson = "";
        helmetJson = "";
        chestJson = "";
        bootsJson = "";

        RecalculateStatsServer();

        OnEquipmentChangedEvent?.Invoke();

        Debug.Log("[PlayerEquipment] Equipment cleared.");
    }

    [Server]
    private void ClearEquippedItem(EquipmentSlot slot)
    {
        Debug.LogWarning($"[PlayerEquipment] ClearEquippedItem CALLED slot={slot}\n{System.Environment.StackTrace}");

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

    private string SerializeItem(ItemInstance item)
    {
        if (item == null || item.instanceId == 0)
            return "";

        return JsonUtility.ToJson(item);
    }

    private ItemInstance DeserializeItem(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonUtility.FromJson<ItemInstance>(json);
    }

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
    private void ApplyAffixStats(
    System.Collections.Generic.List<ItemAffix> affixes,
    StatsComponent targetStats)
    {
        if (affixes == null)
            return;

        foreach (ItemAffix affixInstance in affixes)
        {
            AffixSO affixSO = AffixDatabase.Get(affixInstance.affixId);
            if (affixSO == null)
                continue;

            targetStats.AddFinalStatServer(affixSO.stat, affixInstance.value);
        }
    }
    [Server]
    public void LoadSaveData(PlayerEquipmentData data)
    {
        Debug.LogWarning($"[PlayerEquipment] LoadSaveData CALLED dataNull={data == null}\n{System.Environment.StackTrace}");

        if (data == null)
        {
            weaponJson = "";
            helmetJson = "";
            chestJson = "";
            bootsJson = "";

            RecalculateStatsServer();
            return;
        }

        weaponJson = data.weaponJson ?? "";
        helmetJson = data.helmetJson ?? "";
        chestJson = data.chestJson ?? "";
        bootsJson = data.bootsJson ?? "";

        RecalculateStatsServer();

        Debug.Log("[PlayerEquipment] Equipment loaded from save.");
    }

    public bool HasEquipped(EquipmentSlot slot)
    {
        ItemInstance item = GetEquippedItem(slot);
        return item != null && item.instanceId != 0;
    }
}