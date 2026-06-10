using System.Collections.Generic;
using UnityEngine;

public static class LootGenerator
{
    private static long nextInstanceId = 1;

    public static ItemInstance Generate(ItemBaseSO itemBase, int itemLevel, int seed)
    {
        System.Random rng = new System.Random(seed);
        return Generate(itemBase, itemLevel, rng);
    }

    public static ItemInstance Generate(ItemBaseSO itemBase, int itemLevel, System.Random rng)
    {
        if (itemBase == null)
        {
            Debug.LogError("[LootGenerator] Generate called with null itemBase.");
            return default;
        }

        if (rng == null)
        {
            Debug.LogError("[LootGenerator] Generate called with null rng.");
            return default;
        }

        itemLevel = Mathf.Max(1, itemLevel);

        ItemRarity rarity = RollRarity(rng);

        ItemInstance item = new ItemInstance
        {
            itemName = itemBase.BaseName,
            instanceId = nextInstanceId++,
            baseId = itemBase.BaseId,
            rarity = rarity,
            itemLevel = itemLevel,
            equipSlot = itemBase.SlotType,
            corrupted = false
        };

        item.EnsureLists();

        RollAffixesForExistingItem(item, rng);

        return item;
    }

    public static void RollSingleAffixForExistingItem(ItemInstance item, System.Random rng)
    {
        if (item == null || rng == null)
            return;

        item.EnsureLists();

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);
        if (itemBase == null)
            return;

        HashSet<int> usedAffixIds = GetUsedAffixIds(item);

        bool canRollPrefix = item.prefixes.Count < 3;
        bool canRollSuffix = item.suffixes.Count < 3;

        if (!canRollPrefix && !canRollSuffix)
            return;

        bool rollPrefix;

        if (canRollPrefix && canRollSuffix)
            rollPrefix = rng.NextDouble() < 0.5;
        else
            rollPrefix = canRollPrefix;

        if (rollPrefix)
        {
            RollFromArray(
                itemBase.GetMergedPrefixes(),
                1,
                item.itemLevel,
                rng,
                item.prefixes,
                usedAffixIds,
                AffixSlot.Prefix);
        }
        else
        {
            RollFromArray(
                itemBase.GetMergedSuffixes(),
                1,
                item.itemLevel,
                rng,
                item.suffixes,
                usedAffixIds,
                AffixSlot.Suffix);
        }
    }

    public static void RollAffixesForExistingItem(ItemInstance item, System.Random rng)
    {
        if (item == null || rng == null)
            return;

        item.EnsureLists();

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);
        if (itemBase == null)
            return;

        item.prefixes.Clear();
        item.suffixes.Clear();

        int prefixCount = 0;
        int suffixCount = 0;

        switch (item.rarity)
        {
            case ItemRarity.Normal:
                break;

            case ItemRarity.Magic:
                if (rng.NextDouble() < 0.5)
                    prefixCount = 1;
                else
                    suffixCount = 1;

                if (rng.NextDouble() < 0.5)
                {
                    if (prefixCount == 0)
                        prefixCount = 1;
                    else
                        suffixCount = 1;
                }
                break;

            case ItemRarity.Rare:
                prefixCount = rng.Next(1, 4);
                suffixCount = rng.Next(1, 4);
                break;

            case ItemRarity.Unique:
                break;
        }

        HashSet<int> usedAffixIds = new HashSet<int>();

        RollFromArray(
            itemBase.GetMergedPrefixes(),
            prefixCount,
            item.itemLevel,
            rng,
            item.prefixes,
            usedAffixIds,
            AffixSlot.Prefix);

        RollFromArray(
            itemBase.GetMergedSuffixes(),
            suffixCount,
            item.itemLevel,
            rng,
            item.suffixes,
            usedAffixIds,
            AffixSlot.Suffix);
    }

    public static void RerollAffixValues(ItemInstance item, System.Random rng)
    {
        if (item == null || rng == null)
            return;

        item.EnsureLists();

        RerollAffixListValues(item.prefixes, item.itemLevel, rng);
        RerollAffixListValues(item.suffixes, item.itemLevel, rng);
    }

    private static ItemRarity RollRarity(System.Random rng)
    {
        int roll = rng.Next(0, 1000);

        if (roll < 700) return ItemRarity.Normal;
        if (roll < 850) return ItemRarity.Magic;
        if (roll < 975) return ItemRarity.Rare;

        return ItemRarity.Unique;
    }

    private static void RollFromArray(
        AffixSO[] pool,
        int count,
        int itemLevel,
        System.Random rng,
        List<ItemAffix> outAffixes,
        HashSet<int> usedAffixIds,
        AffixSlot slot)
    {
        if (count <= 0)
            return;

        if (pool == null || pool.Length == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            AffixSO pickedAffix = RollAffix(pool, itemLevel, rng, usedAffixIds);

            if (pickedAffix == null)
                continue;

            AffixTier pickedTier = RollTier(pickedAffix, itemLevel, rng);

            if (pickedTier == null)
                continue;

            usedAffixIds.Add(pickedAffix.affixId);

            int value = rng.Next(pickedTier.minValue, pickedTier.maxValue + 1);

            outAffixes.Add(new ItemAffix
            {
                affixId = pickedAffix.affixId,
                tier = pickedTier.tier,
                value = value,
                slot = slot
            });
        }
    }

    private static AffixSO RollAffix(
        AffixSO[] pool,
        int itemLevel,
        System.Random rng,
        HashSet<int> usedAffixIds)
    {
        int totalWeight = 0;

        for (int i = 0; i < pool.Length; i++)
        {
            AffixSO affix = pool[i];

            if (!IsAffixValid(affix, itemLevel, usedAffixIds))
                continue;

            totalWeight += affix.weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = rng.Next(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < pool.Length; i++)
        {
            AffixSO affix = pool[i];

            if (!IsAffixValid(affix, itemLevel, usedAffixIds))
                continue;

            currentWeight += affix.weight;

            if (roll < currentWeight)
                return affix;
        }

        return null;
    }

    private static bool IsAffixValid(
        AffixSO affix,
        int itemLevel,
        HashSet<int> usedAffixIds)
    {
        if (affix == null)
            return false;

        if (affix.weight <= 0)
            return false;

        if (usedAffixIds.Contains(affix.affixId))
            return false;

        return HasValidTier(affix, itemLevel);
    }

    private static bool HasValidTier(AffixSO affix, int itemLevel)
    {
        if (affix == null || affix.tiers == null || affix.tiers.Length == 0)
            return false;

        for (int i = 0; i < affix.tiers.Length; i++)
        {
            if (IsTierValid(affix.tiers[i], itemLevel))
                return true;
        }

        return false;
    }

    private static AffixTier RollTier(AffixSO affix, int itemLevel, System.Random rng)
    {
        if (affix == null || affix.tiers == null || affix.tiers.Length == 0)
            return null;

        List<AffixTier> validTiers = new List<AffixTier>();

        for (int i = 0; i < affix.tiers.Length; i++)
        {
            AffixTier tier = affix.tiers[i];

            if (IsTierValid(tier, itemLevel))
                validTiers.Add(tier);
        }

        if (validTiers.Count == 0)
            return null;

        return validTiers[rng.Next(0, validTiers.Count)];
    }

    private static bool IsTierValid(AffixTier tier, int itemLevel)
    {
        if (tier == null)
            return false;

        if (itemLevel < tier.minItemLevel)
            return false;

        if (tier.minValue > tier.maxValue)
            return false;

        return true;
    }

    private static void RerollAffixListValues(
        List<ItemAffix> affixes,
        int itemLevel,
        System.Random rng)
    {
        if (affixes == null)
            return;

        for (int i = 0; i < affixes.Count; i++)
        {
            ItemAffix affixInstance = affixes[i];

            AffixSO affixSO = AffixDatabase.Get(affixInstance.affixId);
            if (affixSO == null)
                continue;

            AffixTier tier = GetTierByNumber(affixSO, affixInstance.tier);

            if (tier == null || !IsTierValid(tier, itemLevel))
                continue;

            affixInstance.value = rng.Next(tier.minValue, tier.maxValue + 1);
            affixes[i] = affixInstance;
        }
    }

    private static AffixTier GetTierByNumber(AffixSO affix, int tierNumber)
    {
        if (affix == null || affix.tiers == null)
            return null;

        for (int i = 0; i < affix.tiers.Length; i++)
        {
            AffixTier tier = affix.tiers[i];

            if (tier != null && tier.tier == tierNumber)
                return tier;
        }

        return null;
    }

    private static HashSet<int> GetUsedAffixIds(ItemInstance item)
    {
        HashSet<int> usedAffixIds = new HashSet<int>();

        if (item == null)
            return usedAffixIds;

        item.EnsureLists();

        for (int i = 0; i < item.prefixes.Count; i++)
            usedAffixIds.Add(item.prefixes[i].affixId);

        for (int i = 0; i < item.suffixes.Count; i++)
            usedAffixIds.Add(item.suffixes[i].affixId);

        return usedAffixIds;
    }
}