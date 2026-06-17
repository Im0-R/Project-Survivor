using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Currency")]
public class CurrencySO : LootableSO
{
    [Header("Type")]
    public CurrencyType type = CurrencyType.Sigil;
    public CurrencyUse use = CurrencyUse.Item;

    [Header("Drop")]
    public int dropWeight = 100;

    [Header("Effect")]
    public CurrencyEffectSO effect;

    [Header("UI")]
    [TextArea(3, 8)]
    public string description;

    public int CurrencyId => Id;
    public string CurrencyName => DisplayName;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (id == 0)
            id = GenerateID();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;
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