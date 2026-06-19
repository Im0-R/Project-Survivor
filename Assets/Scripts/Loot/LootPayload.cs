using System;
using UnityEngine;

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

    public bool IsValid()
    {
        return lootableId != 0 && amount > 0;
    }

    public LootPayload Clone()
    {
        return new LootPayload
        {
            lootableId = lootableId,
            amount = Mathf.Max(1, amount),
            itemJson = itemJson ?? "",
            displayNameOverride = displayNameOverride ?? "",
            hasRarityColor = hasRarityColor,
            rarity = rarity
        };
    }

    public LootPayload CloneWithAmount(int newAmount)
    {
        LootPayload clone = Clone();
        clone.amount = Mathf.Max(1, newAmount);
        return clone;
    }
}