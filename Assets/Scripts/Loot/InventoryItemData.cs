using System;
public enum LootableType
{
    Unknown,
    GeneratedItem,
    Sigil,
    Currency
}
[Serializable]
public class InventoryItemData
{
    public int lootableId;
    public int amount = 1;

    public string itemJson = "";
    public string displayNameOverride = "";

    public bool hasRarityColor = false;
    public ItemRarity rarity;

    public LootableType lootableType = LootableType.Unknown;

    public string description = "";

    public bool IsGeneratedItem()
    {
        return !string.IsNullOrWhiteSpace(itemJson);
    }

    public static InventoryItemData FromPayload(LootPayload payload)
    {
        if (payload == null)
            return null;

        return new InventoryItemData
        {
            lootableId = payload.lootableId,
            amount = payload.amount,
            itemJson = payload.itemJson,
            displayNameOverride = payload.displayNameOverride,
            hasRarityColor = payload.hasRarityColor,
            rarity = payload.rarity,

            lootableType = !string.IsNullOrWhiteSpace(payload.itemJson)
                ? LootableType.GeneratedItem
                : LootableType.Unknown,

            description = ""
        };
    }
}