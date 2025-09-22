using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

public class NetworkEntity : NetworkBehaviour
{

    // List of active spells on this entity
    protected List<Spell> activeSpells = new List<Spell>();


    // Stats to give at start
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

    protected virtual void Awake()
    {
        if (SO == null)
        {
            Debug.LogError("StatsDataSO not assigned on " + name);
            return;
        }
    }
    protected virtual void Start()
    {
        // Exemple : abonner un UI ou effet sur la vie
        currentHealth.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"{name} health: {oldValue} → {newValue}");
        };
    }

    protected virtual void Update()
    {
        if (IsServer)
        {
            // Update auto casts spells server-side 
            UpdateSpells();
        }
    }

    public void ApplyStatsFromSO(StatsDataSO statsDataSO)
    {
        if (!IsServer) return; //  block client-side execution

        Debug.Log($"Applying stats from SO to {name}");
        var soFields = typeof(StatsDataSO).GetFields(BindingFlags.Public | BindingFlags.Instance);
        var entityFields = typeof(NetworkEntity).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var soField in soFields)
        {
            //Looking for a field with the same name in NetworkEntity
            var entityField = entityFields.FirstOrDefault(f => f.Name == soField.Name);
            if (entityField == null) continue;

            var soValue = soField.GetValue(statsDataSO);
            var entityValue = entityField.GetValue(this);

            //Check if it's a NetworkVariable<>
            var entityType = entityField.FieldType;
            if (entityType.IsGenericType && entityType.GetGenericTypeDefinition() == typeof(NetworkVariable<>))
            {
                //Getting the value of the NetworkVariable
                var valueProp = entityType.GetProperty("Value");
                valueProp.SetValue(entityValue, soValue);
            }
        }
    }
    public void InitFromSO()
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
    public override void OnNetworkSpawn()
    {
        Debug.Log($"[NetworkEntity] OnNetworkSpawn for {name}");
        if (IsServer)
        {
            Debug.Log($"[NetworkEntity] OnNetworkSpawn Server for {name}");
            // On initialise côté serveur
            InitFromSO();
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void CastSpellServerRpc(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        spell?.ExecuteServer(this);
        Debug.Log($"{name} cast spell {spellName}");
    }

    [ServerRpc(RequireOwnership = true)]
    public void ApplyDamageServerRpc(float amount)
    {
        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} died!");
        if (TryGetComponent<NetworkObject>(out var netObj))
            netObj.Despawn(true);
        else
            Destroy(gameObject);
    }

    //Spells management side
    public void AddRandomSpell()
    {

    }
    public void AddSpell(Spell spell)
    {
        spell.OnAdd(this);
        activeSpells.Add(spell);
        Debug.Log($"Spell {spell.GetType().Name} added to entity.");
    }

    public void RemoveSpell(Spell spell)
    {
        spell.OnRemove(this);
        activeSpells.Remove(spell);
        Debug.Log($"Spell {spell.GetType().Name} removed from entity.");
    }

    public T GetSpell<T>() where T : Spell
    {
        foreach (Spell s in activeSpells)
            if (s is T) return (T)s;
        return null;
    }

    public Spell GetSpellByName(string name)
    {
        foreach (var s in activeSpells)
            if (s.GetType().Name == name) return s;
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
            // Assuming Spell has a method to upgrade itself
            MethodInfo upgradeMethod = spell.GetType().GetMethod("Upgrade");
            if (upgradeMethod != null)
            {
                upgradeMethod.Invoke(spell, null);
                Debug.Log($"Spell {spellName} upgraded.");
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
}
