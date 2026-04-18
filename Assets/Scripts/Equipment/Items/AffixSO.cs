using UnityEngine;

[CreateAssetMenu(menuName = "Game/Affix")]
public class AffixSO : ScriptableObject
{
    [SerializeField] private int affixId;
    public int AffixId => affixId;

    [SerializeField] private string affixName;
    public string AffixName => affixName;

    public StatId stat;
    public int minValue = 1;
    public int maxValue = 5;

#if UNITY_EDITOR
    private void OnValidate()
    {
        bool changed = false;

        if (affixId == 0)
        {
            affixId = GenerateID();
            changed = true;
        }

        if (affixName != name)
        {
            affixName = name;
            changed = true;
        }

        if (minValue > maxValue)
        {
            maxValue = minValue;
            changed = true;
        }

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
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