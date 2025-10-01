using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewStatsData", menuName = "Stats/StatsData")]
public class StatsDataSO : ScriptableObject
{
    // Leveling Stats
    public FixedString32Bytes entityName = "Entity";
    public int level = 1;
    public float experience = 0f;
    public float maxExperience = 100f;
    public float expMultiPerLevel = 1.5f;
    // Defensive stats
    public float maxHealth = 100f;
    public float maxMana = 40f;
    public float currentHealth = 100f;
    public float currentMana = 40f ;

    public float healthRegen = 0f;
    public float manaRegen = 1f;

    //Offensive stats
    public float movementSpeedMultiplier = 1f;
    public float cooldownReduction = 0f;
    public float criticalStrikeChance = 3f;
    public float criticalStrikeDamage = 150f;
    public float projectileSpeed = 0f;
    public float durationMultiplier = 0f;
    public float damageMultiplier = 1f;
    public float experienceGiven = 10f;
}
