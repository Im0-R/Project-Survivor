using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Stats for entity (Player, Enemy, etc.)
/// </summary
[System.Serializable]
public class Stats : NetworkBehaviour
{
    [SerializeField] private StatsDataSO SO;

    // Leveling Stats
    public NetworkVariable<int> level = new NetworkVariable<int>(1);
    public NetworkVariable<float> experience = new NetworkVariable<float>(0);
    public NetworkVariable<float> maxExperience = new NetworkVariable<float>(100);
    public NetworkVariable<float> expMultiPerLevel = new NetworkVariable<float>(1.2f);
    // Defensive stats
    public NetworkVariable<float> maxHealth;
    public NetworkVariable<float> maxMana;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(0);
    public NetworkVariable<float> currentMana = new NetworkVariable<float>(0);

    public NetworkVariable<float> healthRegen = new NetworkVariable<float>(0f);
    public NetworkVariable<float> manaRegen = new NetworkVariable<float>(0f);

    //Offensive stats
    public NetworkVariable<float> movementSpeedMultiplier = new NetworkVariable<float>(0f);
    public NetworkVariable<float> cooldownReduction = new NetworkVariable<float>(1f);
    public NetworkVariable<float> criticalStrikeChance = new NetworkVariable<float>(0.1f);
    public NetworkVariable<float> criticalStrikeDamage = new NetworkVariable<float>(1.5f);
    public NetworkVariable<float> projectileSpeed = new NetworkVariable<float>(0f);
    public NetworkVariable<float> durationMultiplier = new NetworkVariable<float>(0f);
    public NetworkVariable<float> damageMultiplier = new NetworkVariable<float>(0f);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Stats] OnNetworkSpawn for {name}");
        if (IsServer)
        {
            Debug.Log($"[Stats] OnNetworkSpawn Server for {name}");
            // On initialise côté serveur
        }
        // On s'abonne aux changements pour MAJ l'UI côté client
        currentHealth.OnValueChanged += OnHealthChanged;
        currentMana.OnValueChanged += OnManaChanged;
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log($"{name} HP: {oldValue} -> {newValue}");
        // Ici tu peux mettre à jour ton UI si c'est ton joueur local
    }

    private void OnManaChanged(float oldValue, float newValue)
    {
        Debug.Log($"{name} Mana: {oldValue} -> {newValue}");
    }

    // Server side methods to modify stats

    [ServerRpc]
    public void TakeDamageServerRpc(float amount)
    {
        currentHealth.Value = Mathf.Max(0, currentHealth.Value - amount);
    }

    [ServerRpc]
    public void UseManaServerRpc(float amount)
    {
        currentMana.Value = Mathf.Max(0, currentMana.Value - amount);
    }

    [ServerRpc]
    public void HealServerRpc(float amount)
    {
        currentHealth.Value = Mathf.Min(maxHealth.Value, currentHealth.Value + amount);
    }

    public void GainExperience(int amount)
    {
        experience.Value += amount;
        if (experience.Value >= maxExperience.Value)
        {
            LevelUp();
        }
    }
    private void LevelUp()
    {
        level.Value++;
        experience.Value -= maxExperience.Value;
        maxExperience.Value = maxExperience.Value * expMultiPerLevel.Value;
        cooldownReduction.Value += 0.05f;
        criticalStrikeDamage.Value += 0.1f;
        Debug.Log($"Leveled up to {level}! New max XP: {maxExperience}, Speed Multiplier: {movementSpeedMultiplier}, Damage Multiplier: {movementSpeedMultiplier}");
    }

}
