using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStatsData", menuName = "Stats/StatsData")]

public class StatsDataSO : ScriptableObject
{
    [Header("Name")]
    public string stringName = "Unnamed Entity";

    [Header("Leveling Stats")]
    public int level = 1;
    public float experience = 0f;
    public float maxExperience = 100f;
    public float expMultiPerLevel = 1.5f;

    [Header("Defensive Stats")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegen = 0f;

    public float maxMana = 40f;
    public float currentMana = 40f;
    public float manaRegen = 1f;

    [Header("Offensive Stats")]
    public float movementSpeedMultiplier = 1f;
    public float cooldownReduction = 0f;
    public float criticalStrikeChance = 3f;
    public float criticalStrikeDamage = 150f;
    public float projectileSpeed = 0f;
    public float durationMultiplier = 0f;
    public float damageMultiplier = 1f;

    [Header("Rewards")]
    public float experienceGiven = 10f;
}
