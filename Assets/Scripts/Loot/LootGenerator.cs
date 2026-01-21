using System;
using System.Collections.Generic;

public static class LootGenerator
{
    private static long nextInstanceId = 1;

    public static ItemInstance Generate(ItemBaseSO itemBase, int itemLevel, int seed)
    {
        Random rng = new Random(seed);

        ItemRarity rarity = RollRarity(rng);

        int prefixCount = 0;
        int suffixCount = 0;

        switch (rarity)
        {
            case ItemRarity.Normal:
                break;

            case ItemRarity.Magic:

                if (rng.NextDouble() < 0.5) prefixCount = 1;
                else suffixCount = 1;

                if (rng.NextDouble() < 0.5)
                {
                    if (prefixCount == 0) prefixCount = 1;
                    else suffixCount = 1;
                }
                break;

            case ItemRarity.Rare:
                prefixCount = rng.Next(1, 4); // 1-3
                suffixCount = rng.Next(1, 4); // 1-3
                break;

            case ItemRarity.Unique:


                break;
        }

        List<ItemAffix> affixes = new List<ItemAffix>(prefixCount + suffixCount);

        RollFromPool(itemBase.prefixPool, prefixCount, rng, affixes);
        RollFromPool(itemBase.suffixPool, suffixCount, rng, affixes);

        return new ItemInstance
        {
            //Generate random name with the affixes?

            itemName = itemBase.baseName,
            instanceId = nextInstanceId++,
            baseId = itemBase.BaseId,
            rarity = rarity,
            itemLevel = itemLevel,
            affixes = affixes.ToArray()
        };
    }

    private static ItemRarity RollRarity(Random rng)
    {
        int roll = rng.Next(0, 1000);

        if (roll < 700) return ItemRarity.Normal;
        if (roll < 920) return ItemRarity.Magic;
        if (roll < 995) return ItemRarity.Rare;
        return ItemRarity.Unique;
    }

    private static void RollFromPool(AffixPoolSO pool, int count, Random rng, List<ItemAffix> outAffixes)
    {
        if (count <= 0) return;
        if (pool == null || pool.affixes == null || pool.affixes.Length == 0) return;

        HashSet<int> used = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            AffixSO picked = null;

            for (int tries = 0; tries < 30; tries++)
            {
                AffixSO candidate = pool.affixes[rng.Next(0, pool.affixes.Length)];
                if (candidate == null) continue;

                if (used.Add(candidate.AffixId))
                {
                    picked = candidate;
                    break;
                }
            }

            if (picked == null) continue;

            int value = rng.Next(picked.minValue, picked.maxValue + 1);

            outAffixes.Add(new ItemAffix
            {
                affixId = picked.AffixId,
                value = value
            });
        }
    }
}
