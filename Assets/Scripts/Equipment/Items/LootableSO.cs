using UnityEngine;

public abstract class LootableSO : ScriptableObject
{
    [SerializeField] protected int id;
    [SerializeField] protected string displayName;
    [SerializeField] protected Sprite icon;

    public int Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
}