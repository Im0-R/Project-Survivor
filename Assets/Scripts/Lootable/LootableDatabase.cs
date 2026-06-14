using System.Collections.Generic;
using UnityEngine;

public static class LootableDatabase
{
    private static Dictionary<int, LootableSO> lootablesById;
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized)
            return;

        lootablesById = new Dictionary<int, LootableSO>();

        LootableSO[] allLootables = Resources.LoadAll<LootableSO>("");

        foreach (LootableSO lootable in allLootables)
        {
            if (lootable == null)
                continue;

            if (lootable.Id == 0)
            {
                Debug.LogError($"[LootableDatabase] Lootable has id 0: {lootable.name}");
                continue;
            }

            if (lootablesById.ContainsKey(lootable.Id))
            {
                Debug.LogError($"[LootableDatabase] Duplicate id={lootable.Id} on {lootable.name}");
                continue;
            }

            lootablesById.Add(lootable.Id, lootable);
        }

        initialized = true;

        Debug.Log($"[LootableDatabase] Loaded {lootablesById.Count} lootables.");
    }

    public static LootableSO Get(int id)
    {
        if (!initialized)
            Initialize();

        lootablesById.TryGetValue(id, out LootableSO lootable);
        return lootable;
    }
}