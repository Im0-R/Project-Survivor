using UnityEngine;

[CreateAssetMenu(menuName = "Arcana/Rune")]
public class RuneSO : ScriptableObject
{
    [Header("Identity")]
    public string runeId;
    public string runeName;
    public Sprite icon;

    [Header("Compatibility")]
    public SpellTag[] requiredTags;

    [Header("Modifiers")]
    public float damageMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float rangeMultiplier = 1f;

    public int additionalProjectiles = 0;
    public float projectileSpreadAngle = 0;

    public int additionalPierce = 0;

    public bool CanApplyTo(Spell.SpellData spellData)
    {
        if (spellData == null || spellData.tags == null)
            return false;

        if (requiredTags == null || requiredTags.Length == 0)
            return true;

        foreach (SpellTag requiredTag in requiredTags)
        {
            bool hasTag = false;

            foreach (SpellTag spellTag in spellData.tags)
            {
                if (spellTag == requiredTag)
                {
                    hasTag = true;
                    break;
                }
            }

            if (!hasTag)
                return false;
        }

        return true;
    }

    public void ApplyTo(Spell.SpellData data)
    {
        if (data == null) return;

        data.damage *= damageMultiplier;
        data.cooldown *= cooldownMultiplier;
        data.speed *= speedMultiplier;
        data.range *= rangeMultiplier;

        data.projectileCount += additionalProjectiles;
        data.projectileSpreadAngle = projectileSpreadAngle;
        data.pierceCount += additionalPierce;
    }
}