using UnityEngine;

[System.Serializable]
public class AffixTier
{
    public int tier = 1;
    public int minItemLevel = 1;
    public int minValue = 1;
    public int maxValue = 5;
}

[CreateAssetMenu(menuName = "Game/Affix")]
public class AffixSO : ScriptableObject
{
    [Header("Auto")]
    public int affixId;
    public string affixName;

    [Header("Stat")]
    public StatId stat;

    [Header("Affix Weight")]
    public int weight = 100;

    [Header("Auto Tier Generation")]
    public bool autoGenerateTiers = true;
    public int tierCount = 5;
    public int maxGeneratedValue = 50;
    public int itemLevelStep = 10;

    [Header("Tiers")]
    public AffixTier[] tiers;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (affixId == 0)
            affixId = GenerateID();

        affixName = name;
        weight = Mathf.Max(0, weight);

        tierCount = Mathf.Max(1, tierCount);
        maxGeneratedValue = Mathf.Max(1, maxGeneratedValue);
        itemLevelStep = Mathf.Max(1, itemLevelStep);

        if (autoGenerateTiers)
            GenerateTiers();

        ValidateTiers();

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void GenerateTiers()
    {
        tiers = new AffixTier[tierCount];

        float step = maxGeneratedValue / (float)tierCount;

        for (int i = 0; i < tierCount; i++)
        {
            int minValue = Mathf.FloorToInt(step * i) + 1;
            int maxValue = Mathf.FloorToInt(step * (i + 1));

            tiers[i] = new AffixTier
            {
                tier = tierCount - i,
                minItemLevel = i * itemLevelStep + 1,
                minValue = minValue,
                maxValue = Mathf.Max(minValue, maxValue)
            };
        }
    }

    private void ValidateTiers()
    {
        if (tiers == null)
            return;

        foreach (AffixTier tier in tiers)
        {
            if (tier == null)
                continue;

            tier.minItemLevel = Mathf.Max(1, tier.minItemLevel);
            tier.minValue = Mathf.Max(0, tier.minValue);

            if (tier.minValue > tier.maxValue)
                tier.maxValue = tier.minValue;
        }
    }

    private int GenerateID()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

        if (string.IsNullOrEmpty(guid))
            return 0;

        return Mathf.Abs(guid.GetHashCode());
    }
#endif
}