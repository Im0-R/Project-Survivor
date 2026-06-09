using UnityEngine;

public static class DamageCalculator
{
    public static float CalculateFinalDamage(StatsComponent target, DamageInfo damageInfo)
    {
        if (target == null || damageInfo.parts == null)
            return 0f;

        float totalDamage = 0f;

        for (int i = 0; i < damageInfo.parts.Count; i++)
        {
            DamagePart part = damageInfo.parts[i];

            float resistance = GetResistance(target, part.type);
            totalDamage += ApplyResistance(part.amount, resistance);
        }

        return Mathf.Max(0f, totalDamage);
    }

    private static float GetResistance(StatsComponent target, DamageType type)
    {
        return type switch
        {
            DamageType.Fire => target.Get(StatId.FireResistance),
            DamageType.Cold => target.Get(StatId.ColdResistance),
            DamageType.Lightning => target.Get(StatId.LightningResistance),
            DamageType.Chaos => target.Get(StatId.ChaosResistance),
            DamageType.Physical => 0f,
            _ => 0f
        };
    }

    private static float ApplyResistance(float damage, float resistance)
    {
        float multiplier = 1f - resistance / 100f;
        multiplier = Mathf.Clamp(multiplier, 0.05f, 2f);

        return damage * multiplier;
    }
}