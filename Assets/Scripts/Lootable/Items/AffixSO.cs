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
        affixId = Mathf.Max(0, affixId);
        weight = Mathf.Max(0, weight);

        tierCount = Mathf.Max(1, tierCount);
        maxGeneratedValue = Mathf.Max(1, maxGeneratedValue);
        itemLevelStep = Mathf.Max(1, itemLevelStep);

        if (string.IsNullOrEmpty(affixName))
            affixName = name;

        if (affixId == 0)
            affixId = GenerateID();

        if (autoGenerateTiers)
            GenerateTiers();

        ValidateTiers();
    }

    private void GenerateTiers()
    {
        if (tiers == null || tiers.Length != tierCount)
            tiers = new AffixTier[tierCount];

        for (int i = 0; i < tierCount; i++)
        {
            if (tiers[i] == null)
                tiers[i] = new AffixTier();

            int tierNumber = i + 1;
            int maxValue = Mathf.RoundToInt(maxGeneratedValue * (tierNumber / (float)tierCount));
            int minValue = Mathf.Max(1, maxValue - Mathf.CeilToInt(maxGeneratedValue / (float)tierCount));

            tiers[i].tier = tierNumber;
            tiers[i].minItemLevel = 1 + i * itemLevelStep;
            tiers[i].minValue = minValue;
            tiers[i].maxValue = Mathf.Max(minValue, maxValue);
        }
    }

    private void ValidateTiers()
    {
        if (tiers == null)
            return;

        for (int i = 0; i < tiers.Length; i++)
        {
            if (tiers[i] == null)
                tiers[i] = new AffixTier();

            tiers[i].tier = Mathf.Max(1, tiers[i].tier);
            tiers[i].minItemLevel = Mathf.Max(1, tiers[i].minItemLevel);
            tiers[i].minValue = Mathf.Max(0, tiers[i].minValue);
            tiers[i].maxValue = Mathf.Max(tiers[i].minValue, tiers[i].maxValue);
        }
    }

    private int GenerateID()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);

        if (string.IsNullOrEmpty(path))
            return 0;

        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

        if (string.IsNullOrEmpty(guid))
            return 0;

        return Mathf.Abs(guid.GetHashCode());
    }
#endif
}