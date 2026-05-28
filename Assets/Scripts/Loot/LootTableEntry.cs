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
    public LootDropType dropType;

    [Min(1)]
    public int weight = 1;

    [Header("Item")]
    public ItemBaseSO itemBase;

    [Header("Currency")]
    public CurrencySO currency;

    [Header("Amount")]
    public int minAmount = 1;
    public int maxAmount = 1;
}