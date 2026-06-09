using System;
using System.Collections.Generic;

public enum DamageType
{
    Physical,
    Fire,
    Cold,
    Lightning,
    Chaos
}

[Serializable]
public struct DamagePart
{
    public DamageType type;
    public float amount;

    public DamagePart(DamageType type, float amount)
    {
        this.type = type;
        this.amount = amount;
    }
}

[Serializable]
public struct DamageInfo
{
    public List<DamagePart> parts;
    public bool isCrit;

    public DamageInfo(bool isCrit = false)
    {
        parts = new List<DamagePart>(1);
        this.isCrit = isCrit;
    }

    public void Add(DamageType type, float amount)
    {
        if (amount <= 0f)
            return;

        parts ??= new List<DamagePart>(1);
        parts.Add(new DamagePart(type, amount));
    }
}