using System.ComponentModel;
using Unity.Collections;
using UnityEngine;

public enum ItemRarity { Normal, Magic, Rare, Unique }
public enum EquipmentSlot
{
    Weapon,
    Helmet,
    Chest,
    Boots
}


[CreateAssetMenu(menuName = "Game/ItemBase")]
public class ItemBaseSO : ScriptableObject
{
    [SerializeField] private int baseId;
    public int BaseId => baseId;

    public string baseName;
    public EquipmentSlot slot;

    [Header("Base stats")]
    public int baseAttack;
    public int baseDefense;
    public int baseVitality;

    [Header("Affix pools")]
    public AffixPoolSO prefixPool;
    public AffixPoolSO suffixPool;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (baseId == 0)
        {
            baseId = GenerateID();
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
#endif
}