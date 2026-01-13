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
    [SyncVar] public float TotalAttack;
    [SyncVar] public float TotalDefense;
    [SyncVar] public float TotalVitality;

    private PlayerStats stats;
    private PlayerInventory inv;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
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

        switch (baseSO.slot)
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

        float atk = stats.stats.damageMultiplier;
        float def = stats.stats.currentHealth;
        float vit = stats.stats.maxHealth;

        ApplyEquippedIndex(weaponIndex, ref atk, ref def, ref vit);
        ApplyEquippedIndex(helmetIndex, ref atk, ref def, ref vit);
        ApplyEquippedIndex(chestIndex, ref atk, ref def, ref vit);
        ApplyEquippedIndex(bootsIndex, ref atk, ref def, ref vit);

        TotalAttack = atk;
        TotalDefense = def;
        TotalVitality = vit;

        // Update runtime stats
        //stats.SetDerived(atk, def, vit);
    }

    [Server]
    private void ApplyEquippedIndex(int index, ref float atk, ref float def, ref float vit)
    {
        if (index < 0) return;

        var inst = inv.GetItemByIndex(index);
        if (inst.instanceId == 0) return;

        var baseSO = ItemDatabase.GetBase(inst.baseId);
        if (baseSO == null) return;

        // base stats
        atk += baseSO.baseAttack;
        def += baseSO.baseDefense;
        vit += baseSO.baseVitality;

        // affixes
        if (inst.affixes == null) return;

        foreach (var a in inst.affixes)
        {
            var aff = AffixDatabase.Get(a.affixId);
            if (aff == null) continue;

            switch (aff.stat)
            {
                case StatType.Attack: atk += a.value; break;
                case StatType.Defense: def += a.value; break;
                case StatType.Vitality: vit += a.value; break;
            }
        }
    }
}
