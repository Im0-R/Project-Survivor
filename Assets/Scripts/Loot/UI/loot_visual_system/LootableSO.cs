using System;
using UnityEngine;

public abstract class LootableSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] protected int id;
    [SerializeField] protected string displayName;
    [SerializeField] protected Sprite icon;

    [Header("Loot")]
    [SerializeField] protected LootQuantityGroup quantityGroup = LootQuantityGroup.Default;
    [SerializeField] protected LootSpawnBehaviourSO spawnBehaviour;

    [Header("Inventory")]
    [SerializeField] protected bool stackable = false;
    [SerializeField] protected int maxStack = 1;

    [Header("Trade")]
    [SerializeField] protected bool tradeable = true;

    // Kept temporarily so existing ScriptableObject assets do not lose serialized data.
    // The new visual system no longer reads this value.
    [SerializeField, HideInInspector] protected Color labelColor = Color.white;

    public int Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;

    public LootQuantityGroup QuantityGroup => quantityGroup;
    public LootSpawnBehaviourSO SpawnBehaviour => spawnBehaviour;

    public bool Stackable => stackable;
    public int MaxStack => maxStack;

    public bool Tradeable => tradeable;

    [Obsolete("Use LootVisualManager and LootVisualThemeSO instead.")]
    public Color LabelColor => labelColor;
}