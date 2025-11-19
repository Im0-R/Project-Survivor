using Mirror;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnWeaponChanged))] public int weaponItemId;
    [SyncVar(hook = nameof(OnHelmetChanged))] public int helmetItemId;
    [SyncVar(hook = nameof(OnChestChanged))] public int chestItemId;
    [SyncVar(hook = nameof(OnBootsChanged))] public int bootsItemId;
    private PlayerStats stats;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void Equip(ItemDataSO item)
    {
        switch (item.slot)
        {
            case EquipmentSlot.Weapon:
                weaponItemId = item.itemId;
                break;
            case EquipmentSlot.Helmet:
                helmetItemId = item.itemId;
                break;
            case EquipmentSlot.Chest:
                chestItemId = item.itemId;
                break;
            case EquipmentSlot.Boots:
                bootsItemId = item.itemId;
                break;

        }

        RecalculateStats();
    }

    private void OnWeaponChanged(int oldV, int newV) => RecalculateStats();
    private void OnHelmetChanged(int oldV, int newV) => RecalculateStats();
    private void OnChestChanged(int oldV, int newV) => RecalculateStats();
    private void OnBootsChanged(int oldV, int newV) => RecalculateStats();
    public void RecalculateStats()
    {
        //stats.ResetToBase();

        // Weapon
        if (weaponItemId != 0)
        {
            var item = ItemDatabase.GetItem(weaponItemId);
            //stats.attack += item.attack;
        }

        // Helmet
        if (helmetItemId != 0)
        {
            var item = ItemDatabase.GetItem(helmetItemId);
            //stats += item.defense;
        }

        // Update synced stats
        //stats.SyncAll();
    }
}
