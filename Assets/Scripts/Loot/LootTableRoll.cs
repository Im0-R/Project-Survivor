using UnityEngine;

[System.Serializable]
public class LootTableRoll
{
    public LootTableSO table;

    [Header("Proc Chance")]
    [Range(0f, 100f)]
    public float chanceToRoll = 100f;

    [Header("Rolls")]
    public int minRolls = 1;
    public int maxRolls = 1;

    [Header("Quantity")]
    public float quantityMultiplier = 1f;
}