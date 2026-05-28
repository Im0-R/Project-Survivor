using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Loot Profile")]
public class LootProfileSO : ScriptableObject
{
    public LootTableRoll[] tableRolls;

    [Header("Global Quantity")]
    public float quantityMultiplier = 1f;
    public int additionalRolls = 0;

    [Header("Type Multipliers")]
    public float itemQuantityMultiplier = 1f;
    public float currencyQuantityMultiplier = 1f;
    public float goldQuantityMultiplier = 1f;
}