using UnityEngine;

[CreateAssetMenu(menuName = "Game/Affix")]
public class AffixSO : ScriptableObject
{
    [SerializeField] private int affixId;


    public int AffixId => affixId;

    public string affixName;

    public StatId stat;
    public int minValue = 1;
    public int maxValue = 5;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (affixId == 0)
        {
            affixId = GenerateID();
            GenerateName();
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

    // Generate name 

    private void GenerateName()
    {
        affixName = this.name;
    }

#endif
}
