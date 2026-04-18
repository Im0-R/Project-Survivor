using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemRarity
{
    Normal,
    Magic,
    Rare,
    Unique
}

public enum EquipmentSlot
{
    None = 0,
    Weapon = 1,
    Helmet = 2,
    Chest = 3,
    Boots = 4,
    Any = 5
}

[CreateAssetMenu(menuName = "Game/ItemBase")]
public class ItemBaseSO : ScriptableObject
{
    [SerializeField] private int baseId;
    public int BaseId => baseId;

    [Header("Identity")]
    [SerializeField] private string baseName;
    public string BaseName => baseName;

    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.None;
    public EquipmentSlot SlotType => slotType;

    [SerializeField] private int itemLevelRequirement;
    public int ItemLevelRequirement => itemLevelRequirement;

    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;

    [Header("Base stats")]
    [SerializeField] private int baseAttack;
    public int BaseAttack => baseAttack;

    [SerializeField] private int baseDefense;
    public int BaseDefense => baseDefense;

    [SerializeField] private int baseVitality;
    public int BaseVitality => baseVitality;

    [Header("Main affix pools")]
    [SerializeField] private AffixPoolSO prefixPool;
    [SerializeField] private AffixPoolSO suffixPool;

    [Header("Additional affix pools")]
    [SerializeField] private AffixPoolSO[] additionalPrefixPools;
    [SerializeField] private AffixPoolSO[] additionalSuffixPools;

    public AffixPoolSO GetPrefixes()
    {
        return prefixPool;
    }

    public AffixPoolSO GetSuffixes()
    {
        return suffixPool;
    }

    public AffixSO[] GetMergedPrefixes()
    {
        return MergeAffixes(prefixPool, additionalPrefixPools);
    }

    public AffixSO[] GetMergedSuffixes()
    {
        return MergeAffixes(suffixPool, additionalSuffixPools);
    }

    private AffixSO[] MergeAffixes(AffixPoolSO mainPool, AffixPoolSO[] additionalPools)
    {
        HashSet<AffixSO> merged = new HashSet<AffixSO>();

        if (mainPool != null && mainPool.affixes != null)
        {
            foreach (AffixSO affix in mainPool.affixes)
            {
                if (affix != null)
                    merged.Add(affix);
            }
        }

        if (additionalPools != null)
        {
            foreach (AffixPoolSO pool in additionalPools)
            {
                if (pool == null || pool.affixes == null)
                    continue;

                foreach (AffixSO affix in pool.affixes)
                {
                    if (affix != null)
                        merged.Add(affix);
                }
            }
        }

        return merged.ToArray();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bool changed = false;

        if (baseId == 0)
        {
            baseId = GenerateID();
            changed = true;
        }

        string wantedName = ExtractBaseNameFromAssetName();
        if (baseName != wantedName)
        {
            baseName = wantedName;
            changed = true;
        }

        if (slotType == EquipmentSlot.None)
        {
            Debug.LogWarning($"[ItemBaseSO] {name} n'a pas de slot défini.", this);
        }

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    private string ExtractBaseNameFromAssetName()
    {
        int underscoreIndex = name.IndexOf('_');

        if (underscoreIndex > 0 && underscoreIndex < name.Length - 1)
            return name.Substring(underscoreIndex + 1);

        return name;
    }

    private int GenerateID()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

        if (string.IsNullOrEmpty(guid))
            return 0;

        return Mathf.Abs(guid.GetHashCode());
    }

    [ContextMenu("Regenerate Base ID")]
    private void RegenerateID()
    {
        baseId = GenerateID();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}