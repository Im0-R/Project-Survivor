using UnityEngine;

public enum LootDropType
{
    Item,
    Currency,
    Gold
}

[System.Serializable]
public class LootTableEntry
{
    [Header("Drop")]
    public LootableSO drop;

    [Header("Amount")]
    public int minAmount = 1;
    public int maxAmount = 1;

    [Header("Weight")]
    public int weight = 100;
}