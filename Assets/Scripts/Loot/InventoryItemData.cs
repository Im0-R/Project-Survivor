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

        LootableSO lootable = LootableDatabase.Get(payload.lootableId);

        LootableType type = LootableType.Unknown;
        string description = "";

        if (!string.IsNullOrWhiteSpace(payload.itemJson))
        {
            type = LootableType.GeneratedItem;
        }
        else if (lootable is CurrencySO currency)
        {
            type = currency.type == CurrencyType.Sigil
                ? LootableType.Sigil
                : LootableType.Currency;

            description = currency.description;
        }
        return new InventoryItemData
        {
            lootableId = payload.lootableId,
            amount = payload.amount,
            itemJson = payload.itemJson,
            displayNameOverride = !string.IsNullOrWhiteSpace(payload.displayNameOverride)
                ? payload.displayNameOverride
                : lootable != null ? lootable.DisplayName : "",

            hasRarityColor = payload.hasRarityColor,
            rarity = payload.rarity,
            lootableType = type,
            description = description
        };
    }
}