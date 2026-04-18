using Mirror;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{
    // On stocke le vrai contenu de l'item équipé, pas un index d'inventaire.
    [SyncVar] private string weaponJson = "";
    [SyncVar] private string helmetJson = "";
    [SyncVar] private string chestJson = "";
    [SyncVar] private string bootsJson = "";

    // Optionnel pour l'UI
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

    [Command]
    public void CmdEquipFromInventoryIndex(int inventoryIndex)
    {
        if (inv == null) return;

        ItemInstance newItem = inv.GetItemByIndex(inventoryIndex);
        if (newItem.instanceId == 0) return;

        ItemBaseSO baseSO = ItemDatabase.GetBase(newItem.baseId);
        if (baseSO == null) return;

        // Sauvegarde l'ancien item équipé dans ce slot
        ItemInstance oldEquipped = GetEquippedItem(baseSO.SlotType);

        // 1. On enlève le nouvel item de l'inventaire
        inv.RemoveAt(inventoryIndex);

        // 2. On équipe le nouvel item
        SetEquippedItem(baseSO.SlotType, newItem);

        // 3. S'il y avait déjà un item équipé, on le remet dans l'inventaire
        if (oldEquipped.instanceId != 0)
        {
            bool addedBack = inv.AddItem(oldEquipped);

            // Sécurité : si l'inventaire est plein, on annule l'équipement
            if (!addedBack)
            {
                // On enlève le nouvel item du slot d'équipement
                ClearEquippedItem(baseSO.SlotType);

                // On tente de remettre le nouvel item dans l'inventaire
                bool restoredNewItem = inv.AddItem(newItem);

                if (!restoredNewItem)
                {
                    Debug.LogError("[PlayerEquipment] Impossible de restaurer l'item après échec d'équipement, inventaire plein.");
                }

                return;
            }
        }

        RecalculateStatsServer();
    }

    [Command]
    public void CmdUnequip(EquipmentSlot slot)
    {
        if (inv == null) return;

        ItemInstance equipped = GetEquippedItem(slot);
        if (equipped.instanceId == 0) return;

        bool added = inv.AddItem(equipped);
        if (!added)
        {
            Debug.LogWarning("[PlayerEquipment] Inventaire plein, impossible de déséquiper.");
            return;
        }

        ClearEquippedItem(slot);
        RecalculateStatsServer();
    }

    // =========================
    // SERVER
    // =========================

    [Server]
    private void RecalculateStatsServer()
    {
        if (stats == null) return;

        // Mets ici les bonnes stats de base de ton perso
        float dam = GetStatSafe(StatId.DamageMult);
        float def = GetStatSafe(StatId.Armor);
        float vit = GetStatSafe(StatId.MaxHealth);

        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Weapon), ref dam, ref def, ref vit);
        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Helmet), ref dam, ref def, ref vit);
        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Chest), ref dam, ref def, ref vit);
        ApplyEquippedItem(GetEquippedItem(EquipmentSlot.Boots), ref dam, ref def, ref vit);

        TotalDamage = dam;
        TotalDefense = def;
        TotalVitality = vit;

        // Si tu as une vraie méthode pour appliquer les stats runtime, décommente/adapte ici
        // stats.SetDerived(dam, def, vit);
    }

    [Server]
    private void ApplyEquippedItem(ItemInstance inst, ref float dam, ref float def, ref float vit)
    {
        if (inst.instanceId == 0) return;

        ItemBaseSO baseSO = ItemDatabase.GetBase(inst.baseId);
        if (baseSO == null) return;

        dam += baseSO.BaseAttack;
        def += baseSO.BaseDefense;
        vit += baseSO.BaseVitality;

        if (inst.affixes == null) return;

        foreach (var a in inst.affixes)
        {
            AffixSO aff = AffixDatabase.Get(a.affixId);
            if (aff == null) continue;

            switch (aff.stat)
            {
                case StatId.SpellDamage:
                case StatId.DamageMult:
                    dam += a.value;
                    break;

                case StatId.Armor:
                    def += a.value;
                    break;

                case StatId.MaxHealth:
                    vit += a.value;
                    break;
            }
        }
    }

    // =========================
    // HELPERS
    // =========================

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
    private void ClearEquippedItem(EquipmentSlot slot)
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

    private string SerializeItem(ItemInstance item)
    {
        return JsonUtility.ToJson(item);
    }

    private ItemInstance DeserializeItem(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonUtility.FromJson<ItemInstance>(json);
    }

    private float GetStatSafe(StatId statId)
    {
        if (stats == null || stats.stats == null)
            return 0f;

        if (!stats.stats.ContainsKey(statId))
            return 0f;

        return stats.stats[statId];
    }

    // =========================
    // UTILS FOR UI
    // =========================

    public bool HasEquipped(EquipmentSlot slot)
    {
        return GetEquippedItem(slot).instanceId != 0;
    }
}