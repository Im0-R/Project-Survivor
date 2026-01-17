using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class StatEntry
{
    public StatId id;
    public float value;
}

[CreateAssetMenu(fileName = "NewStatsData", menuName = "Stats/StatsData")]
public class StatsDataSO : ScriptableObject
{
    public string stringName = "Unnamed Entity";

    [Header("Base Stats")]
    public StatEntry[] baseStats;

    [Header("Leveling")]
    public int level = 1;

#if UNITY_EDITOR
    private void OnValidate()
    {

        Dictionary<StatId, float> dict = new Dictionary<StatId, float>();

        if (baseStats != null)
        {
            foreach (var entry in baseStats)
            {
                if (!dict.ContainsKey(entry.id))
                    dict.Add(entry.id, entry.value);
            }
        }

        // Create a complete list with default values for stats

        foreach (StatId id in Enum.GetValues(typeof(StatId)))
        {
            if (!dict.ContainsKey(id))
                dict[id] = GetDefaultValue(id);
        }

        // Rebuild the array

        baseStats = new StatEntry[dict.Count];
        int i = 0;
        foreach (var kvp in dict)
        {
            baseStats[i++] = new StatEntry
            {
                id = kvp.Key,
                value = kvp.Value
            };
        }
    }

    private float GetDefaultValue(StatId id)
    {
        return id switch
        {
            StatId.MaxHealth => 100f,
            StatId.CurrentHealth => 100f,
            StatId.MaxMana => 50f,
            StatId.CurrentMana => 50f,
            StatId.MoveSpeedMult => 3.5f,
            StatId.ExpMultiPerLevel => 1.5f,
            StatId.CritChance => 5f,
            StatId.MaxExperience => 100f,
            _ => 0f
        };
    }
#endif
}