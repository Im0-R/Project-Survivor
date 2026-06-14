using System;
using System.Collections.Generic;

[Serializable]
public enum AffixSlot
{
    Prefix,
    Suffix
}

[Serializable]
public struct ItemAffix
{
    public int affixId;
    public int tier;
    public int value;
    public AffixSlot slot;
}

[Serializable]
public class ItemInstance
{
    public string itemName;
    public long instanceId;
    public int baseId;
    public ItemRarity rarity;
    public int itemLevel;
    public EquipmentSlot equipSlot;

    public bool corrupted;

    public List<ItemAffix> prefixes = new();
    public List<ItemAffix> suffixes = new();

    public int PrefixCount => prefixes?.Count ?? 0;
    public int SuffixCount => suffixes?.Count ?? 0;
    public int TotalAffixCount => PrefixCount + SuffixCount;

    public void EnsureLists()
    {
        prefixes ??= new List<ItemAffix>();
        suffixes ??= new List<ItemAffix>();
    }
}