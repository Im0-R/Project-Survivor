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

    Experience,
    MaxExperience,
    ExpMultiPerLevel,
    ExperienceGiven,

    FireResistance,
    ColdResistance,
    LightningResistance,
    ChaosResistance,

    Armor,
    Evasion,

    SpellDamage,

    FireDamage,
    ColdDamage,
    LightningDamage,
    ChaosDamage,

    CooldownReduction,

    CritChance,
    CritDamage,

    ProjectileSpeed,

    DurationMult,

    DamageMult,

    MoveSpeedMult,
}

[Serializable]
public class StatsComponent : NetworkBehaviour
{
    [Header("Base Data")]
    [SerializeField] private StatsDataSO statsData;

    [SyncVar] public int level;
    [SyncVar] public string Name;

    public readonly SyncDictionary<StatId, float> stats = new();

    private readonly Dictionary<StatId, float> baseStatsServer = new();

    public event Action OnStatsChanged;
    public event Action<int> OnLevelUpServer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitFromSO_Server();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        stats.OnChange += OnStatChanged;

        OnStatsChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        stats.OnChange -= OnStatChanged;
        base.OnStopClient();
    }

    private void OnStatChanged(SyncDictionary<StatId, float>.Operation op, StatId key, float value)
    {
        OnStatsChanged?.Invoke();
    }

    [Server]
    public void InitFromSO_Server()
    {
        if (statsData == null)
        {
            Debug.LogError($"[StatsComponent] StatsDataSO missing on {name}");
            return;
        }

        baseStatsServer.Clear();
        stats.Clear();

        level = statsData.level;
        Name = statsData.stringName;

        foreach (var entry in statsData.baseStats)
        {
            baseStatsServer[entry.id] = entry.value;
            stats[entry.id] = entry.value;
        }
    }

    public float Get(StatId id)
    {
        return stats.TryGetValue(id, out float value) ? value : 0f;
    }

    [Server]
    public void SetBaseStatServer(StatId id, float value)
    {
        baseStatsServer[id] = value;
        stats[id] = value;
    }

    [Server]
    public void AddBaseStatServer(StatId id, float delta)
    {
        SetBaseStatServer(id, GetBaseStatServer(id) + delta);
    }

    [Server]
    public float GetBaseStatServer(StatId id)
    {
        return baseStatsServer.TryGetValue(id, out float value) ? value : 0f;
    }

    [Server]
    public void SetFinalStatServer(StatId id, float value)
    {
        stats[id] = value;
    }

    [Server]
    public void AddFinalStatServer(StatId id, float delta)
    {
        stats[id] = Get(id) + delta;
    }

    [Server]
    public void RecalculateFinalStatsServer(PlayerEquipment equipment)
    {
        float oldHealth = Get(StatId.CurrentHealth);
        float oldMana = Get(StatId.CurrentMana);

        stats.Clear();

        foreach (var kvp in baseStatsServer)
            stats[kvp.Key] = kvp.Value;

        if (equipment != null)
            equipment.ApplyEquipmentStatsToServer(this);

        float maxHealth = Get(StatId.MaxHealth);
        float maxMana = Get(StatId.MaxMana);

        stats[StatId.CurrentHealth] = Mathf.Clamp(oldHealth, 0f, maxHealth);
        stats[StatId.CurrentMana] = Mathf.Clamp(oldMana, 0f, maxMana);
    }
    [Server]
    public void TakeDamage(DamageInfo damageInfo)
    {
        float finalDamage = DamageCalculator.CalculateFinalDamage(this, damageInfo);

        float oldHealth = Get(StatId.CurrentHealth);
        float newHealth = Mathf.Max(0f, oldHealth - finalDamage);

        SetFinalStatServer(StatId.CurrentHealth, newHealth);

        RpcShowDamagePopup(
            transform.position,
            Mathf.RoundToInt(finalDamage),
            damageInfo.isCrit
        );
    }
    [Server]
    public void Heal(float amount)
    {
        SetFinalStatServer(
            StatId.CurrentHealth,
            Mathf.Min(Get(StatId.MaxHealth), Get(StatId.CurrentHealth) + amount)
        );
    }

    [Server]
    public void UseMana(float amount)
    {
        SetFinalStatServer(
            StatId.CurrentMana,
            Mathf.Max(0f, Get(StatId.CurrentMana) - amount)
        );
    }

    [Server]
    public void RestoreMana(float amount)
    {
        SetFinalStatServer(
            StatId.CurrentMana,
            Mathf.Min(Get(StatId.MaxMana), Get(StatId.CurrentMana) + amount)
        );
    }

    [Server]
    public void GainExperience(float amount)
    {
        AddFinalStatServer(StatId.Experience, amount);

        while (Get(StatId.Experience) >= Get(StatId.MaxExperience))
            LevelUp_Server();
    }

    [Server]
    private void LevelUp_Server()
    {
        AddFinalStatServer(StatId.Experience, -Get(StatId.MaxExperience));

        level++;

        SetBaseStatServer(StatId.MaxExperience, GetBaseStatServer(StatId.MaxExperience) * GetBaseStatServer(StatId.ExpMultiPerLevel));
        SetBaseStatServer(StatId.MaxHealth, GetBaseStatServer(StatId.MaxHealth) * 1.1f);
        SetBaseStatServer(StatId.MaxMana, GetBaseStatServer(StatId.MaxMana) * 1.1f);

        PlayerEquipment equipment = GetComponent<PlayerEquipment>();
        RecalculateFinalStatsServer(equipment);

        SetFinalStatServer(StatId.CurrentMana, Get(StatId.MaxMana));
        SetFinalStatServer(
            StatId.CurrentHealth,
            Mathf.Min(Get(StatId.MaxHealth), Get(StatId.CurrentHealth) + Get(StatId.MaxHealth) / 10f)
        );

        Debug.Log($"[StatsComponent] {name} leveled up to {level}");
        OnLevelUpServer?.Invoke(level);
    }

    [ClientRpc]
    private void RpcShowDamagePopup(Vector3 position, int damage, bool isCrit)
    {
        if (DamagePopupManager.Instance == null)
            return;

        Vector3 offset = new Vector3(
            UnityEngine.Random.Range(-0.4f, 0.4f),
            UnityEngine.Random.Range(1.2f, 1.8f),
            0f
        );

        DamagePopupManager.Instance.ShowDamage(position + offset, damage, isCrit);
    }
}