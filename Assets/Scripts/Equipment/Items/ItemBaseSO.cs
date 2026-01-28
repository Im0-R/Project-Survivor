using System.Linq;
using UnityEngine;

public enum ItemRarity { Normal, Magic, Rare, Unique }
public enum EquipmentSlot
{
    Weapon,
    Helmet,
    Chest,
    Boots,
    Any
}


[CreateAssetMenu(menuName = "Game/ItemBase")]
public class ItemBaseSO : ScriptableObject
{
    [SerializeField] private int baseId;
    public int BaseId => baseId;

    public string baseName;
    public EquipmentSlot slot;
    public int itemLevelRequirement;
    public Sprite icon;


    [Header("Base stats")]
    public int baseAttack;
    public int baseDefense;
    public int baseVitality;

    [Header("Affix pools")]
    private AffixPoolSO prefixPool;
    private AffixPoolSO suffixPool;

    public AffixPoolSO[] additionalPrefixPools;
    public AffixPoolSO[] additionalSuffixPools;

    public AffixPoolSO GetPrefixes()
    {
        return prefixPool;
    }
    public AffixPoolSO GetSuffixes()
    {
        return suffixPool;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (baseId == 0)
        {
            baseId = GenerateID();
            MergePools();
            string newName = name;
            int underscoreIndex = name.IndexOf('_');
            if (underscoreIndex > 0)
            {
                baseName = name.Substring(underscoreIndex + 1);
            }
            else
            {
                baseName = name;
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    private int GenerateID()
    {
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(
            UnityEditor.AssetDatabase.GetAssetPath(this)
        );

        return Mathf.Abs(guid.GetHashCode());
    }

    [ContextMenu("Regenerate Base ID")]
    private void RegenerateID()
    {
        baseId = GenerateID();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void MergePools()
    {
        if (additionalPrefixPools != null && additionalPrefixPools.Length > 0)
        {
            foreach (AffixPoolSO pool in additionalPrefixPools)
            {
                foreach (AffixSO affixes in pool.affixes)
                {
                   if (prefixPool != null && !prefixPool.affixes.Contains(affixes))
                   {
                       prefixPool.affixes.Append(affixes);
                    }
                }
            }
        }
        if (additionalSuffixPools != null && additionalSuffixPools.Length > 0)
        {
            foreach (AffixPoolSO pool in additionalSuffixPools)
            {
                foreach (AffixSO affixes in pool.affixes)
                {
                   if (suffixPool != null && !suffixPool.affixes.Contains(affixes))
                   {
                       suffixPool.affixes.Append(affixes);
                    }
                }
            }
        }

    }
#endif
}