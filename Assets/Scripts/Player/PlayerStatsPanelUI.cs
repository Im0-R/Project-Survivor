using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerStatsPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private StatRowUI statRowPrefab;

    private readonly Dictionary<StatId, StatRowUI> rows = new();

    private StatsComponent stats;

    public void Bind(StatsComponent targetStats)
    {
        if (stats != null)
            stats.OnStatsChanged -= RefreshAll;

        stats = targetStats;

        if (stats != null)
        {
            stats.OnStatsChanged += RefreshAll;
            BuildRows();
            RefreshAll();
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnStatsChanged -= RefreshAll;
    }

    private void BuildRows()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        rows.Clear();

        foreach (StatId statId in System.Enum.GetValues(typeof(StatId)))
        {
            StatRowUI row = Instantiate(statRowPrefab, contentParent);
            rows[statId] = row;
        }
    }

    private void RefreshAll()
    {
        if (stats == null) return;

        foreach (StatId statId in System.Enum.GetValues(typeof(StatId)))
        {
            if (!rows.TryGetValue(statId, out StatRowUI row))
                continue;

            float value = stats.Get(statId);
            row.Set(GetDisplayName(statId), value);
        }
    }

    private string GetDisplayName(StatId statId)
    {
        return SplitCamelCase(statId.ToString());
    }

    private string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(input, "(\\B[A-Z])", " $1");
    }
}