using Mirror;
using UnityEngine;

/// <summary>
/// Stats for entity (Player, Enemy, etc.)
/// </summary
[System.Serializable]
public class Stats : NetworkBehaviour
{
    [SerializeField] private StatsDataSO SO;

    // Leveling Stats
    [SyncVar] public int level = 1;
    [SyncVar] public float experience = 0;
    [SyncVar] public float maxExperience = 100;
    [SyncVar] public float expMultiPerLevel = 1.2f;
    // Defensive stats
    [SyncVar] public float maxHealth;
    [SyncVar] public float maxMana;
    [SyncVar] public float currentHealth = 0;
    [SyncVar] public float currentMana = 0;

    [SyncVar] public float healthRegen = 0f;
    [SyncVar] public float manaRegen = 0f;

    //Offensive stats
    [SyncVar] public float movementSpeedMultiplier = 0f;
    [SyncVar] public float cooldownReduction = 1f;
    [SyncVar] public float criticalStrikeChance = 0.1f;
    [SyncVar] public float criticalStrikeDamage = 1.5f;
    [SyncVar] public float projectileSpeed = 0f;
    [SyncVar] public float durationMultiplier = 0f;
    [SyncVar] public float damageMultiplier = 0f;

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log($"{name} HP: {oldValue} -> {newValue}");
        // Ici tu peux mettre à jour ton UI si c'est ton joueur local
    }

    private void OnManaChanged(float oldValue, float newValue)
    {
        Debug.Log($"{name} Mana: {oldValue} -> {newValue}");
    }

    // Command methods to modify stats
    [Command]
    void CmdTakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
    }
    [Command]
    void CmdUseManag(float amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
    }
    [Command]
    public void CmdHeal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
    [Command]
    public void CmdRestoreMana(float amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }
    public void GainExperience(int amount)
    {
        experience += amount;
        if (experience >= maxExperience)
        {
            LevelUp();
        }
    }
    private void LevelUp()
    {
        level++;
        experience -= maxExperience;
        maxExperience = maxExperience * expMultiPerLevel;
        cooldownReduction += 0.05f;
        criticalStrikeDamage += 0.1f;
        Debug.Log($"Leveled up to {level}! New max XP: {maxExperience}, Speed Multiplier: {movementSpeedMultiplier}, Damage Multiplier: {movementSpeedMultiplier}");
    }

}
