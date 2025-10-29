using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class NetworkEntity : NetworkBehaviour
{
    // ----------------------- Spells ----------------------- //
    protected List<Spell> activeSpells = new List<Spell>();


    // Client list of spells for UI synchronization
    public readonly SyncList<SpellSyncData> syncedSpells = new SyncList<SpellSyncData>();



    [SerializeField] private StatsDataSO SO;

    [SyncVar] public string entityName;

    // Leveling stats
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

    // Offensive stats
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

    protected virtual void Awake() { }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitStatsFromSO();
        OnDeath += Die;
        OnLevelUp += LevelUp;

        // 👇 Ici tu peux attribuer un ou plusieurs spells par défaut au spawn si tu veux :
        // AddSpell(SpellsManager.Instance.GetSpell("FireballSpell"));
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
    }
    protected virtual void Start()
    {
        // Rien ici — toute la logique d'initialisation est côté serveur
    }

    protected virtual void Update()
    {
        if (!isServer) return;
        UpdateSpells();
    }

    // ----------------------- Stats ----------------------- //

    public void ApplyStatsFromSO(StatsDataSO statsDataSO)
    {
        if (!isServer) return;

        var soFields = typeof(StatsDataSO).GetFields(BindingFlags.Public | BindingFlags.Instance);
        var entityFields = typeof(NetworkEntity).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var soField in soFields)
        {
            var entityField = entityFields.FirstOrDefault(f => f.Name == soField.Name);
            if (entityField == null) continue;

            var soValue = soField.GetValue(statsDataSO);
            if (entityField.IsPublic && !entityField.IsInitOnly)
                entityField.SetValue(this, soValue);
        }

        entityName = statsDataSO.stringName;
    }

    public void InitStatsFromSO()
    {
        if (SO != null) ApplyStatsFromSO(SO);
        else Debug.LogError("StatsDataSO not assigned in Stats component on " + name);
    }

    protected virtual void Die()
    {
        if (!isServer) return;

        if (TryGetComponent<NetworkIdentity>(out var netIdentity))
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    public void CmdApplyDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            OnDeath?.Invoke();
    }

    // ----------------------- Spells ----------------------- //

    [Command]
    public void CmdCastSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        if (spell != null)
        {
            spell.ExecuteServer(this);
            Debug.Log($"[SERVER] CmdCastSpell: {entityName} cast {spellName}");
        }
        else
        {
            Debug.LogWarning($"[SERVER] Spell '{spellName}' not found on {entityName}");
        }
    }

    [Command]
    public void CmdAddSpell(string spellName)
    {
        Spell spell = SpellsManager.Instance.GetSpell(spellName);
        if (spell == null)
        {
            Debug.LogWarning($"[SERVER] CmdAddSpell failed: spell '{spellName}' not found.");
            return;
        }

        AddSpell(spellName);
    }

    [Server]
    public void AddSpell(string spellName)
    {
        // 1. Récupérer le modèle dans SpellsManager
        Spell template = SpellsManager.Instance.GetSpell(spellName);
        if (template == null)
        {
            Debug.LogWarning($"[SERVER] AddSpell failed: spell '{spellName}' not found.");
            return;
        }

        // 2. Créer une instance du spell
        Spell newSpell = (Spell)Activator.CreateInstance(template.GetType());
        Spell.SpellData newData = template.GetData().Clone();
        newSpell.Init(newData);

        // 3. Ajouter à la liste serveur
        activeSpells.Add(newSpell);
        newSpell.OnAdd(this);

        // 4. Remplir le SpellSyncData pour le client
        SpellSyncData syncData = new SpellSyncData(
            newData.spellName,
            newData.spellTypeID,
            newData.description,
            newData.manaCost,
            newData.cooldown,
            newData.damage,
            newData.range,
            newData.speed,
            newData.currentLevel,
            newData.maxLevel
        );

        // 5. Ajouter dans la SyncList
        syncedSpells.Add(syncData);

        Debug.Log($"[SERVER] Spell ajouté: {newData.spellName} à {entityName}");
    }

    [Server]
    public void RemoveSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        if (spell == null) return;

        activeSpells.Remove(spell);
        spell.OnRemove(this);

        int index = syncedSpells.FindIndex(s => s.spellName == spellName);
        if (index >= 0)
            syncedSpells.RemoveAt(index);
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
        Debug.Log($"[SERVER] {entityName} has {activeSpells.Count} active spells");
        foreach (var spell in activeSpells)
            spell.UpdateSpell(this);
    }

    public void UpgradeSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        if (spell != null)
        {
            var upgradeMethod = spell.GetType().GetMethod("Upgrade");
            if (upgradeMethod != null)
            {
                upgradeMethod.Invoke(spell, null);
                Debug.Log($"[SERVER] {spellName} upgraded for {entityName}");
            }
            else
            {
                Debug.LogWarning($"[SERVER] Spell {spellName} does not have an Upgrade method.");
            }
        }
        else
        {
            Debug.LogWarning($"[SERVER] Spell {spellName} not found on {entityName}.");
        }
    }

    public Spell GetRandomSpellFromActivesSpells()
    {
        if (activeSpells.Count == 0) return null;
        int index = UnityEngine.Random.Range(0, activeSpells.Count);
        return activeSpells[index];
    }

    public List<Spell> GetAllActiveSpells() => activeSpells;

    // ----------------------- XP & Level ----------------------- //

    public void GainExperience(float amount)
    {
        if (!isServer) return;
        experience += amount;
        while (experience >= maxExperience)
            OnLevelUp?.Invoke();
    }

    public void LevelUp()
    {
        if (!isServer) return;

        Debug.Log($"{entityName} leveled up to level {level + 1}!");
        experience -= maxExperience;
        level++;
        maxExperience *= expMultiPerLevel;
        maxHealth *= 1.1f;
        maxMana *= 1.1f;
        currentMana = maxMana;
        currentHealth += maxHealth / 10f;
    }

    public float GetHealthPourcentage() => (currentHealth / maxHealth) * 100f;
}
