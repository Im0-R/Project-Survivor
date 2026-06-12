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

    [Header("Item")]
    public ItemBaseSO itemBase;

    [Header("Currency")]
    public CurrencySO sigil;

    [Header("Amount")]
    public int minAmount = 1;
    public int maxAmount = 1;

    [Header("Weight")]
    public int weight = 100;
}