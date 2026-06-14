using System;

[Serializable]
public class LootPayload
{
    public int lootableId;
    public int amount = 1;

    public string itemJson = "";

    public string displayNameOverride = "";

    public bool hasRarityColor = false;
    public ItemRarity rarity;

    public bool IsGeneratedItem()
    {
        return !string.IsNullOrWhiteSpace(itemJson);
    }
}