using UnityEngine;

public enum ItemRarity { Normal, Magic, Rare, Unique }
public enum EquipmentSlot
{
    Weapon,
    Helmet,
    Chest,
    Boots
}

[CreateAssetMenu(menuName = "Game/ItemBase")]
public class ItemBaseSO : ScriptableObject
{
    public int baseId;
    public string baseName;
    public EquipmentSlot slot;

    [Header("Base stats")]
    public int baseAttack;
    public int baseDefense;
    public int baseVitality;

    [Header("Affix pools")]
    public AffixPoolSO prefixPool;
    public AffixPoolSO suffixPool;
}