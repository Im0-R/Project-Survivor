using UnityEngine;

public abstract class SigilEffectSO : ScriptableObject
{
    public abstract bool CanApply(ItemInstance item);
    public abstract void Apply(ItemInstance item, System.Random rng);
}

[CreateAssetMenu(menuName = "Game/Sigils/Sigil")]
public class SigilSO : ScriptableObject
{
    [Header("Identity")]
    public int sigilId;
    public string sigilName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Stack")]
    public int maxStack = 20;

    [Header("Drop")]
    public int dropWeight = 100;

    [Header("Effect")]
    public SigilEffectSO effect;

    public bool CanUseOn(ItemInstance item)
    {
        return item != null && effect != null && effect.CanApply(item);
    }

    public bool UseOn(ItemInstance item, System.Random rng)
    {
        if (!CanUseOn(item))
            return false;

        effect.Apply(item, rng);
        return true;
    }
}