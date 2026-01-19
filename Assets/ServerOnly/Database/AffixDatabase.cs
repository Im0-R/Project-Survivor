using System.Collections.Generic;
using UnityEngine;

public static class AffixDatabase
{
    private static Dictionary<int, AffixSO> affixesById;
    private static bool initialized = false;

    // =========================
    // INIT
    // =========================
    public static void Initialize()
    {
        if (initialized) return;

        affixesById = new Dictionary<int, AffixSO>();

        //Load all AffixSO from Resources
        AffixSO[] allAffixes = Resources.LoadAll<AffixSO>("");

        if (allAffixes == null || allAffixes.Length == 0)
        {
            Debug.LogWarning("[AffixDatabase] +No AffixSO found! Place them in Resources/.");
            initialized = true;
            return;
        }

        foreach (var affix in allAffixes)
        {
            if (affix == null) continue;

            if (affixesById.ContainsKey(affix.AffixId))
            {
                Debug.LogError($"[AffixDatabase] Duplicate affixId {affix.AffixId} ({affix.name})");
                continue;
            }

            affixesById[affix.AffixId] = affix;
        }

        initialized = true;
        Debug.Log($"[AffixDatabase] Loaded {affixesById.Count} affixes.");
    }

    // =========================
    // GETTERS
    // =========================
    public static AffixSO Get(int affixId)
    {
        if (!initialized) Initialize();

        affixesById.TryGetValue(affixId, out var affix);
        if (affix == null)
            Debug.LogWarning($"[AffixDatabase] Affix not found (id={affixId})");

        return affix;
    }

    public static bool Exists(int affixId)
    {
        if (!initialized) Initialize();
        return affixesById.ContainsKey(affixId);
    }

    public static IReadOnlyCollection<AffixSO> GetAll()
    {
        if (!initialized) Initialize();
        return affixesById.Values;
    }
}
