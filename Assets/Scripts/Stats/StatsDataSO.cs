using System;
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
    public float expMultiPerLevel = 1.5f;
}
