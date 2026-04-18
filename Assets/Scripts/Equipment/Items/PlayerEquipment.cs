using Mirror;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{
    //Storing the index in the inventory (PlayerInventory.ItemsJson)
    [SyncVar] public int weaponIndex = -1;
    [SyncVar] public int helmetIndex = -1;
    [SyncVar] public int chestIndex = -1;
    [SyncVar] public int bootsIndex = -1;

    //Optional : synced final stats for UI (otherwise read them on PlayerStats)
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

    // =========================
    // CLIENT -> SERVER
    // =========================

    //Called by UI (Equip Item from inventory)
    [Command]
    public void CmdEquipFromInventoryIndex(int inventoryIndex)
    {
        if (inv == null) return;

        var inst = inv.GetItemByIndex(inventoryIndex);
        if (inst.instanceId == 0) return;

        var baseSO = ItemDatabase.GetBase(inst.baseId);
        if (baseSO == null) return;

        switch (baseSO.SlotType)
        {
            case EquipmentSlot.Weapon: weaponIndex = inventoryIndex; break;
            case EquipmentSlot.Helmet: helmetIndex = inventoryIndex; break;
            case EquipmentSlot.Chest: chestIndex = inventoryIndex; break;
            case EquipmentSlot.Boots: bootsIndex = inventoryIndex; break;
        }

        RecalculateStatsServer();
    }

    [Command]
    public void CmdUnequip(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: weaponIndex = -1; break;
            case EquipmentSlot.Helmet: helmetIndex = -1; break;
            case EquipmentSlot.Chest: chestIndex = -1; break;
            case EquipmentSlot.Boots: bootsIndex = -1; break;
        }

        RecalculateStatsServer();
    }

    // =========================
    // SERVER
    // =========================

    [Server]
    private void RecalculateStatsServer()
    {
        if (stats == null || inv == null) return;

        float dam = stats.stats[StatId.DamageMult];
        float def = stats.stats[StatId.CurrentHealth];
        float vit = stats.stats[StatId.MaxHealth];

        ApplyEquippedIndex(weaponIndex, ref dam, ref def, ref vit);
        ApplyEquippedIndex(helmetIndex, ref dam, ref def, ref vit);
        ApplyEquippedIndex(chestIndex, ref dam, ref def, ref vit);
        ApplyEquippedIndex(bootsIndex, ref dam, ref def, ref vit);

        TotalDamage = dam;
        TotalDefense = def;
        TotalVitality = vit;

        // Update runtime stats
        //stats.SetDerived(dam, def, vit);
    }

    [Server]
    private void ApplyEquippedIndex(int index, ref float dam, ref float def, ref float vit)
    {
        if (index < 0) return;

        ItemInstance inst = inv.GetItemByIndex(index);
        if (inst.instanceId == 0) return;

        ItemBaseSO baseSO = ItemDatabase.GetBase(inst.baseId);
        if (baseSO == null) return;

        // base stats
        dam += baseSO.BaseAttack;
        def += baseSO.BaseDefense;
        vit += baseSO.BaseVitality;

        // affixes
        if (inst.affixes == null) return;

        foreach (var a in inst.affixes)
        {
            AffixSO aff = AffixDatabase.Get(a.affixId);
            if (aff == null) continue;

            switch (aff.stat)
            {
                case StatId.SpellDamage: dam += a.value; break;
                case StatId.Armor: def += a.value; break;
                case StatId.MaxHealth: vit += a.value; break;
            }
        }
    }
}
