using Mirror;
using System;
using UnityEngine;

public enum StatId : ushort
{
    //------------------------Basic Stats-------------------------
    //Health
    MaxHealth,
    CurrentHealth,
    HealthRegen,

    //Mana
    MaxMana,
    CurrentMana,
    ManaRegen,

    //------------------------Levelling Stats-------------------------
    Experience,
    MaxExperience,
    ExpMultiPerLevel,
    ExperienceGiven,

    //------------------------Defensive Stats-------------------------



    // Elemental Resistance 

    FireResistance,
    ColdResistance,
    LightningResistance,
    ChaosResistance,

    Armor,
    Evasion,

    //------------------------Offensive Stats-------------------------

    //Damage

    SpellDamage,

    //Elemental Damage
    FireDamage,
    ColdDamage,
    LightningDamage,
    ChaosDamage,

    //Cooldowns
    CooldownReduction,

    //Critical
    CritChance,
    CritDamage,

    //Projectile
    ProjectileSpeed,

    // Durations 
    DurationMult,

    //Damage
    DamageMult,

    //------------------------Movement Stats-------------------------
    MoveSpeedMult,
}

[Serializable]
public class StatsComponent : NetworkBehaviour
{

    // Non-float / identité
    [SyncVar] public int level;

    [SyncVar] public string Name;

    // Toutes les stats float scalables
    public readonly SyncDictionary<StatId, float> stats = new();


    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        stats.OnChange += OnStatChanged;
    }

    public override void OnStopClient()
    {
        stats.OnChange -= OnStatChanged;
        base.OnStopClient();
    }

    [Server]
    public void InitFromSO_Server(StatsDataSO so)
    {
        stats.Clear();

        level = so.level;

        foreach (var entry in so.baseStats)
            stats[entry.id] = entry.value;
    }


    // ====== API propre ======
    public float Get(StatId id) => stats.TryGetValue(id, out var v) ? v : 0f;

    [Server] public void Set(StatId id, float value) => stats[id] = value;

    [Server] public void Add(StatId id, float delta) => stats[id] = Get(id) + delta;

    // ====== Events client (UI) ======
    void OnStatChanged(SyncDictionary<StatId, float>.Operation op, StatId key, float value)
    {
    }

    // ====== Server-side gameplay ======
    [Server]
    public void TakeDamage(float amount)
    {
        Set(StatId.CurrentHealth, Mathf.Max(0f, Get(StatId.CurrentHealth) - amount));
    }

    [Server]
    public void Heal(float amount)
    {
        Set(StatId.CurrentHealth, Mathf.Min(Get(StatId.MaxHealth), Get(StatId.CurrentHealth) + amount));
    }

    [Server]
    public void UseMana(float amount)
    {
        Set(StatId.CurrentMana, Mathf.Max(0f, Get(StatId.CurrentMana) - amount));
    }

    [Server]
    public void RestoreMana(float amount)
    {
        Set(StatId.CurrentMana, Mathf.Min(Get(StatId.MaxMana), Get(StatId.CurrentMana) + amount));
    }

    [Server]
    public void GainExperience(float amount)
    {
        Add(StatId.Experience, amount);
        while (Get(StatId.Experience) >= Get(StatId.MaxExperience))
            LevelUp_Server();
    }

    [Server]
    void LevelUp_Server()
    {
        Add(StatId.Experience, -Get(StatId.MaxExperience));
        level++;

        Set(StatId.MaxExperience, Get(StatId.MaxExperience) * Get(StatId.ExpMultiPerLevel));

        // exemples de scaling
        Set(StatId.MaxHealth, Get(StatId.MaxHealth) * 1.1f);
        Set(StatId.MaxMana, Get(StatId.MaxMana) * 1.1f);

        // refill partiel
        Set(StatId.CurrentMana, Get(StatId.MaxMana));
        Set(StatId.CurrentHealth, Mathf.Min(Get(StatId.MaxHealth), Get(StatId.CurrentHealth) + Get(StatId.MaxHealth) / 10f));
    }
}
