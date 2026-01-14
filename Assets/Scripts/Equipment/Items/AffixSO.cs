using UnityEngine;

public enum StatType { Attack, Defense, Vitality }

[CreateAssetMenu(menuName = "Game/Affix")]
public class AffixSO : ScriptableObject
{
    public int affixId;
    public string affixName;

    public StatType stat;
    public int minValue = 1;
    public int maxValue = 5;
}