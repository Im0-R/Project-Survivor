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
public class ItemBaseSO : LootableSO
{
    [SerializeField] private int baseId;
    public int BaseId => baseId;

    [Header("Identity")]
    [SerializeField] private string baseName;
    public string BaseName => baseName;

    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.None;
    public EquipmentSlot SlotType => slotType;

    [SerializeField] private int itemLevel;
    public int ItemLevel => itemLevel;

    [Header("Base stats")]
    [SerializeField] private int baseAttack;
    public int BaseAttack => baseAttack;

    [SerializeField] private int baseDefense;
    public int BaseDefense => baseDefense;

    [SerializeField] private int baseVitality;
    public int BaseVitality => baseVitality;

    [Header("Affix pools")]
    [SerializeField] private AffixPoolSO[] prefixPools;
    [SerializeField] private AffixPoolSO[] suffixPools;

    public AffixSO[] GetMergedPrefixes()
    {
        return MergeAffixes(prefixPools);
    }

    public AffixSO[] GetMergedSuffixes()
    {
        return MergeAffixes(suffixPools);
    }

    private AffixSO[] MergeAffixes(AffixPoolSO[] pools)
    {
        HashSet<AffixSO> merged = new HashSet<AffixSO>();

        if (pools == null)
            return merged.ToArray();

        foreach (AffixPoolSO pool in pools)
        {
            if (pool == null || pool.affixes == null)
                continue;

            foreach (AffixSO affix in pool.affixes)
            {
                if (affix != null)
                    merged.Add(affix);
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