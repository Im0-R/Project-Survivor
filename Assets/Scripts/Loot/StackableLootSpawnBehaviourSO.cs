using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Spawn Behaviours/Stackable Loot")]
public class StackableLootSpawnBehaviourSO : LootSpawnBehaviourSO
{
    public override void BuildPayloads(
        LootableSO lootable,
        LootTableEntry entry,
        LootSpawnContext context,
        List<LootPayload> results)
    {
        if (lootable == null || entry == null || context == null || context.rng == null || results == null)
            return;

        int min = Mathf.Min(entry.minAmount, entry.maxAmount);
        int max = Mathf.Max(entry.minAmount, entry.maxAmount);

        int baseAmount = context.rng.Next(min, max + 1);

        float multiplier = context.GetQuantityMultiplier(lootable.QuantityGroup);
        int finalAmount = Mathf.RoundToInt(baseAmount * multiplier);
        finalAmount = Mathf.Max(1, finalAmount);

        results.Add(new LootPayload
        {
            lootableId = lootable.Id,
            amount = finalAmount,
            itemJson = "",
            displayNameOverride = "",
            hasRarityColor = false
        });
    }
}