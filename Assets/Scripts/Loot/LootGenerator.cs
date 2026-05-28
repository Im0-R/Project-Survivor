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

        Debug.Log($"[LootGenerator] item={itemBase.BaseName}, rarity={rarity}, requestedPrefixes={prefixCount}, requestedSuffixes={suffixCount}");
        Debug.Log($"[LootGenerator] mergedPrefixes={(mergedPrefixes != null ? mergedPrefixes.Length : 0)}, mergedSuffixes={(mergedSuffixes != null ? mergedSuffixes.Length : 0)}");

        RollFromArray(mergedPrefixes, prefixCount, rng, rolledAffixes);
        RollFromArray(mergedSuffixes, suffixCount, rng, rolledAffixes);

        Debug.Log($"[LootGenerator] finalAffixCount={rolledAffixes.Count}");

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

    private static void RollFromArray(AffixSO[] pool, int count, System.Random rng, List<ItemAffix> outAffixes)
    {
        if (count <= 0)
            return;

        if (pool == null || pool.Length == 0)
            return;

        HashSet<int> usedAffixIds = new HashSet<int>();

        foreach (ItemAffix existing in outAffixes)
            usedAffixIds.Add(existing.affixId);

        for (int i = 0; i < count; i++)
        {
            AffixSO picked = null;

            for (int tries = 0; tries < 30; tries++)
            {
                AffixSO candidate = pool[rng.Next(0, pool.Length)];

                if (candidate == null)
                    continue;

                if (usedAffixIds.Add(candidate.affixId))
                {
                    picked = candidate;
                    break;
                }
            }

            if (picked == null)
                continue;

            int value = rng.Next(picked.minValue, picked.maxValue + 1);

            outAffixes.Add(new ItemAffix
            {
                affixId = picked.affixId,
                value = value
            });
        }
    }
}