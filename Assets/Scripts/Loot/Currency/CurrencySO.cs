using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Currency")]
public class CurrencySO : ScriptableObject
{
    [Header("Identity")]
    public int currencyId;
    public string currencyName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public CurrencyType type = CurrencyType.Sigil;
    public CurrencyUse use = CurrencyUse.Item;

    [Header("Stack")]
    public int maxStack = 20;

    [Header("Drop")]
    public int dropWeight = 100;

    [Header("Effect")]
    public CurrencyEffectSO effect;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (currencyId == 0)
            currencyId = GenerateID();

        if (string.IsNullOrWhiteSpace(currencyName))
            currencyName = name;
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