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

        int prefixCount = 0;
        int suffixCount = 0;

        switch (rarity)
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

        AffixSO[] mergedPrefixes = itemBase.GetMergedPrefixes();
        AffixSO[] mergedSuffixes = itemBase.GetMergedSuffixes();

        List<ItemAffix> rolledAffixes = new List<ItemAffix>(prefixCount + suffixCount);
        HashSet<int> usedAffixIds = new HashSet<int>();

        RollFromArray(mergedPrefixes, prefixCount, itemLevel, rng, rolledAffixes, usedAffixIds);
        RollFromArray(mergedSuffixes, suffixCount, itemLevel, rng, rolledAffixes, usedAffixIds);

        return new ItemInstance
        {
            itemName = itemBase.BaseName,
            instanceId = nextInstanceId++,
            baseId = itemBase.BaseId,
            rarity = rarity,
            itemLevel = itemLevel,
            affixes = rolledAffixes.ToArray(),
            equipSlot = itemBase.SlotType
        };
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
        HashSet<int> usedAffixIds)
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
                value = value
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
        if (affix.tiers == null || affix.tiers.Length == 0)
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
}