using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatId : ushort
{
    MaxHealth,
    CurrentHealth,
    HealthRegen,
    MaxMana,
    CurrentMana,
    ManaRegen,
    MoveSpeedMult,
    CooldownReduction,
    CritChance,
    CritDamage,
    ProjectileSpeed,
    DurationMult,
    DamageMult,
    Experience,
    MaxExperience,
    ExpMultiPerLevel,
    ExperienceGiven
}
public class StatsComponent : NetworkBehaviour
{
    [SerializeField] private StatsDataSO SO;

    // Non-float / identité
    [SyncVar] public int level;

    // Toutes les stats float scalables
    public readonly SyncDictionary<StatId, float> stats = new();
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        InitFromSO_Server();
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
    void InitFromSO_Server()
    {
        if (SO == null)
        {
            Debug.LogError($"[{name}] StatsDataSO manquant");
            return;
        }

        level = SO.level;

        stats[StatId.MaxHealth] = SO.maxHealth;
        stats[StatId.CurrentHealth] = SO.currentHealth;
        stats[StatId.HealthRegen] = SO.healthRegen;

        stats[StatId.MaxMana] = SO.maxMana;
        stats[StatId.CurrentMana] = SO.currentMana;
        stats[StatId.ManaRegen] = SO.manaRegen;

        stats[StatId.MoveSpeedMult] = SO.movementSpeedMultiplier;
        stats[StatId.CooldownReduction] = SO.cooldownReduction;
        stats[StatId.CritChance] = SO.criticalStrikeChance;
        stats[StatId.CritDamage] = SO.criticalStrikeDamage;
        stats[StatId.ProjectileSpeed] = SO.projectileSpeed;
        stats[StatId.DurationMult] = SO.durationMultiplier;
        stats[StatId.DamageMult] = SO.damageMultiplier;

        stats[StatId.Experience] = SO.experience;
        stats[StatId.MaxExperience] = SO.maxExperience;
        stats[StatId.ExpMultiPerLevel] = SO.expMultiPerLevel;

        stats[StatId.ExperienceGiven] = SO.experienceGiven;
    }

    // ====== API propre ======
    public float Get(StatId id) => stats.TryGetValue(id, out var v) ? v : 0f;

    [Server] public void Set(StatId id, float value) => stats[id] = value;

    [Server] public void Add(StatId id, float delta) => stats[id] = Get(id) + delta;

    // ====== Events client (UI) ======
    void OnStatChanged(SyncDictionary<StatId, float>.Operation op, StatId key, float value)
    {
        stats[key] = value;
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
