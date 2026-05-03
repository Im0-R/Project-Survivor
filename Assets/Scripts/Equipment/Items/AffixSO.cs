using UnityEngine;

[CreateAssetMenu(menuName = "Game/Affix")]
public class AffixSO : ScriptableObject
{
    [Header("Auto")]
    public int affixId;
    public string affixName;

    [Header("Stats")]
    public StatId stat;
    public int minValue = 1;
    public int maxValue = 5;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (affixId == 0)
            affixId = GenerateID();

        affixName = name;

        if (minValue > maxValue)
            maxValue = minValue;

        UnityEditor.EditorUtility.SetDirty(this);
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