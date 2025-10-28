using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
public class NetworkEntity : NetworkBehaviour
{

    // List of active spells on this entity
    protected List<Spell> activeSpells = new List<Spell>();


    // Stats to give at start
    [SerializeField] private StatsDataSO SO;

    [SyncVar] public String entityName;
    // Leveling Stats
    [SyncVar] public int level;
    [SyncVar] public float experience;
    [SyncVar] public float maxExperience;
    [SyncVar] public float expMultiPerLevel;
    // Defensive stats
    [SyncVar] public float maxHealth;
    [SyncVar] public float maxMana;
    [SyncVar] public float currentHealth;
    [SyncVar] public float currentMana;

    [SyncVar] public float healthRegen;
    [SyncVar] public float manaRegen;

    //Offensive stats
    [SyncVar] public float movementSpeedMultiplier;
    [SyncVar] public float cooldownReduction;
    [SyncVar] public float criticalStrikeChance;
    [SyncVar] public float criticalStrikeDamage;
    [SyncVar] public float projectileSpeed;
    [SyncVar] public float durationMultiplier;
    [SyncVar] public float damageMultiplier;
    [SyncVar] public float experienceGiven;

    public event Action OnDeath;
    public event Action OnLevelUp;
    protected virtual void Awake() {; }
    protected virtual void Start()
    {
        if (!isServer) return; //  block client-side execution
        InitStatsFromSO();

        OnDeath += Die;
        OnLevelUp += LevelUp;
    }

    protected virtual void Update()
    {
        if (!isServer) return;
        // Update auto casts spells server-side 
        UpdateSpells();
    }

    public void ApplyStatsFromSO(StatsDataSO statsDataSO)
    {
        if (!isServer) return; // Only Server

        var soFields = typeof(StatsDataSO).GetFields(BindingFlags.Public | BindingFlags.Instance);
        var entityFields = typeof(NetworkEntity).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var soField in soFields)
        {
            // Look for a matching field in NetworkEntity
            var entityField = entityFields.FirstOrDefault(f => f.Name == soField.Name);
            if (entityField == null) continue;

            // Get the value from the ScriptableObject
            var soValue = soField.GetValue(statsDataSO);

            // Make sure the field is not read-only
            if (entityField.IsPublic && !entityField.IsInitOnly)
            {
                // Assign the value to the NetworkEntity field
                entityField.SetValue(this, soValue);
            }
        }
        entityName = statsDataSO.stringName;
    }
    public void InitStatsFromSO()
    {
        if (SO != null)
        {
            ApplyStatsFromSO(SO);
        }
        else
        {
            Debug.LogError("StatsDataSO not assigned in Stats component on " + name);
        }
    }


    protected virtual void Die()
    {
        if (isServer) //Only Server can destroy objects
        {
            if (TryGetComponent<NetworkIdentity>(out var netIdentity))
            {
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    public void CmdApplyDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            OnDeath?.Invoke();
    }




    //------------------------------------------------Spells management side-----------------------------------------\\
    [Command]
    public void CmdCastSpell(string spellName)
    {
        // Cette méthode est exécutée sur le serveur uniquement
        Spell spell = SpellsManager.Instance.GetSpell(spellName);
        if (spell != null)
        {
            spell.ExecuteServer(this);
        }
        else
        {
            Debug.LogWarning($"Spell '{spellName}' not found for entity {name}");
        }
    }
    [Command]
    public void AddRandomSpell()
    {

    }
    public void AddSpell(Spell spell)
    {
        Debug.Log($"[NetworkEntity] Adding spell {spell.GetData().spellName} to {name}");
        spell.OnAdd(this);
        activeSpells.Add(spell);
    }

    public void RemoveSpell(Spell spell)
    {
        spell.OnRemove(this);
        activeSpells.Remove(spell);
    }

    public T GetSpell<T>() where T : Spell
    {
        foreach (Spell s in activeSpells)
            if (s is T) return (T)s;
        return null;
    }

    public Spell GetSpellByTypeName(string name)
    {
        foreach (var s in activeSpells)
            if (s.GetType().Name == name) return s;
        return null;
    }
    public Spell GetSpellByName(string spellName)
    {
        foreach (var s in activeSpells)
            if (s.GetData().spellName == spellName) return s;
        return null;
    }
    public void UpdateSpells()
    {
        foreach (var spell in activeSpells)
        {
            spell.UpdateSpell(this);
        }
    }
    public void UpgradeSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        if (spell != null)
        {
            MethodInfo upgradeMethod = spell.GetType().GetMethod("Upgrade");
            if (upgradeMethod != null)
            {
                upgradeMethod.Invoke(spell, null);
            }
            else
            {
                Debug.LogWarning($"Spell {spellName} does not have an Upgrade method.");
            }
        }
        else
        {
            Debug.LogWarning($"Spell {spellName} not found on entity.");
        }
    }
    public Spell GetRandomSpellFromActivesSpells()
    {
        if (activeSpells.Count == 0) return null;
        int index = UnityEngine.Random.Range(0, activeSpells.Count);
        return activeSpells[index];
    }
    public List<Spell> GetAllActiveSpells()
    {
        return activeSpells;
    }

    //Exp and Leveling

    public void GainExperience(float amount)
    {
        if (!isServer) return; //  block client-side execution
        experience += amount;
        while (experience >= maxExperience)
        {
            OnLevelUp?.Invoke();
        }
    }
    public void LevelUp()
    {
        if (!isServer) return; //  block client-side execution

        Debug.Log($"{name} leveled up to level {level + 1}!");
        experience -= maxExperience;
        level++;
        maxExperience *= expMultiPerLevel;
        // On level up, increase stats a bit
        maxHealth *= 1.1f;
        maxMana *= 1.1f;
        currentMana = maxMana; // Refill mana on level up
        currentHealth += maxHealth / 10f; // Heal 10 % of max health on level up

    }
    public float GetHealthPourcentage()
    {
        return (currentHealth / maxHealth) * 100f;
    }
}
