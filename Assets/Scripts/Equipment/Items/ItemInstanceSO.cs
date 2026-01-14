using System;

[Serializable]
public struct ItemAffix
{
    public int affixId;
    public int value;
}

[Serializable]
public class ItemInstance
{
    public int instanceId;   //unique ID for this specific item instance
    public int baseId;       //ID referring to the base item type
    public ItemRarity rarity;
    public int itemLevel;

    public ItemAffix[] affixes; // rolls (can be null)
}
