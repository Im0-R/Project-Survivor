using UnityEngine;

public class LootSpawnContext
{
    public LootProfileSO profile;
    public int itemLevel;
    public System.Random rng;

    public float extraQuantityMultiplier = 1f;
    public float extraCurrencyMultiplier = 1f;
    public float extraGoldMultiplier = 1f;

    public float GetQuantityMultiplier(LootQuantityGroup group)
    {
        if (profile == null)
            return extraQuantityMultiplier;

        float multiplier = profile.quantityMultiplier * extraQuantityMultiplier;

        switch (group)
        {
            case LootQuantityGroup.Item:
                multiplier *= profile.itemQuantityMultiplier;
                break;

            case LootQuantityGroup.Currency:
                multiplier *= profile.currencyQuantityMultiplier;
                multiplier *= extraCurrencyMultiplier;
                break;

            case LootQuantityGroup.Gold:
                multiplier *= profile.goldQuantityMultiplier;
                multiplier *= extraGoldMultiplier;
                break;
        }

        return Mathf.Max(0f, multiplier);
    }

    public int RollCountFromMultiplier(float multiplier)
    {
        if (rng == null)
            return Mathf.Max(0, Mathf.RoundToInt(multiplier));

        int guaranteed = Mathf.FloorToInt(multiplier);
        float chanceForExtra = multiplier - guaranteed;

        int count = guaranteed;

        if (rng.NextDouble() <= chanceForExtra)
            count++;

        return Mathf.Max(0, count);
    }
}