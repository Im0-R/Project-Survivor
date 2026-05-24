using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RunStatsComponent : NetworkBehaviour
{
    private readonly Dictionary<StatId, float> runStatBonuses = new();

    public float GetBonus(StatId stat)
    {
        return runStatBonuses.TryGetValue(stat, out float value) ? value : 0f;
    }

    public float GetCurrentStat(StatsComponent stats, StatId stat)
    {
        if (stats == null)
            return 0f;

        return stats.Get(stat) + GetBonus(stat);
    }

    [Server]
    public void AddRunStatBonus(StatId stat, float value)
    {
        if (!runStatBonuses.ContainsKey(stat))
            runStatBonuses[stat] = 0f;

        runStatBonuses[stat] += value;

        Debug.Log($"[RunStats] +{value} {stat}");
    }

    [Server]
    public void ClearRunStats()
    {
        runStatBonuses.Clear();
        Debug.Log("[RunStats] Cleared run stat bonuses.");
    }
}