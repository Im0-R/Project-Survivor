using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Currency")]
public class CurrencySO : ScriptableObject
{
    public int currencyId;
    public string currencyName;
    public Sprite icon;
    public int maxStack = 999;

    [TextArea]
    public string description;
}