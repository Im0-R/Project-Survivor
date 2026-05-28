using UnityEngine;

public class LootBonusContext
{
    public float quantityMultiplier = 1f;
    public float currencyQuantityMultiplier = 1f;
    public float itemRarityMultiplier = 1f;
}

[CreateAssetMenu(menuName = "Difficulty/Difficulty Scaling")]
public class DifficultyScalingSO : ScriptableObject
{
    [Header("Stats per Difficulty Point")]
    public float healthPercentPerPoint = 10f;
    public float damagePercentPerPoint = 5f;
    public float moveSpeedPercentPerPoint = 2f;
    public float experiencePercentPerPoint = 5f;

    [Header("Loot per Difficulty Point")]
    public float lootQuantityPercentPerPoint = 8f;
    public float currencyQuantityPercentPerPoint = 3f;
    public float goldQuantityPercentPerPoint = 5f;
}