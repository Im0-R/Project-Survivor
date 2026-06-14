using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Spawn Behaviours/Generated Item")]
public class GeneratedItemSpawnBehaviourSO : LootSpawnBehaviourSO
{
    public override void BuildPayloads(
        LootableSO lootable,
        LootTableEntry entry,
        LootSpawnContext context,
        List<LootPayload> results)
    {
        if (lootable == null || context == null || context.rng == null || results == null)
            return;

        ItemBaseSO itemBase = lootable as ItemBaseSO;

        if (itemBase == null)
        {
            Debug.LogError($"[GeneratedItemSpawnBehaviour] {lootable.name} is not an ItemBaseSO.");
            return;
        }

        float multiplier = context.GetTypeQuantityMultiplier(lootable.QuantityGroup);
        int count = context.RollCountFromMultiplier(multiplier);

        for (int i = 0; i < count; i++)
        {
            ItemInstance item = LootGenerator.Generate(
                itemBase,
                context.itemLevel,
                context.rng
            );

            if (item == null)
                continue;

            results.Add(new LootPayload
            {
                lootableId = lootable.Id,
                amount = 1,
                itemJson = JsonUtility.ToJson(item),
                displayNameOverride = item.itemName,
                hasRarityColor = true,
                rarity = item.rarity
            });
        }
    }
}