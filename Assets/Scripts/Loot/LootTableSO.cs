using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Loot Table")]
public class LootTableSO : ScriptableObject
{
    public LootTableEntry[] entries;

    public LootTableEntry RollOne(System.Random rng)
    {
        if (rng == null || entries == null || entries.Length == 0)
            return null;

        int totalWeight = 0;

        foreach (LootTableEntry entry in entries)
        {
            if (entry == null)
                continue;

            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
            return null;

        int roll = rng.Next(0, totalWeight);
        int current = 0;

        foreach (LootTableEntry entry in entries)
        {
            if (entry == null)
                continue;

            current += Mathf.Max(0, entry.weight);

            if (roll < current)
                return entry;
        }

        return null;
    }
}