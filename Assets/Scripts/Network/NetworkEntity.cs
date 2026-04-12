using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// TODO:
// - extraire la gestion des sorts dans un composant dédié EntitySpellbook
// - garder NetworkEntity comme base légère commune
#region  Mirror custom writer/reader registration

public static class MirrorWritersRegistration
{
    [RuntimeInitializeOnLoadMethod]
    static void RegisterCustomWriters()
    {
        // Writer for SpellSyncData
        Writer<SpellSyncData>.write = (writer, value) =>
        {
            writer.WriteString(value.spellName);
            writer.WriteString(value.description);
            writer.WriteInt(value.manaCost);
            writer.WriteFloat(value.cooldown);
            writer.WriteFloat(value.damage);
            writer.WriteFloat(value.range);
            writer.WriteFloat(value.speed);
            writer.WriteInt(value.currentLevel);
            writer.WriteInt(value.maxLevel);
        };

        // Reader for SpellSyncData
        Reader<SpellSyncData>.read = reader =>
        {
            SpellSyncData data = new SpellSyncData();
            data.spellName = reader.ReadString();
            data.description = reader.ReadString();
            data.manaCost = reader.ReadInt();
            data.cooldown = reader.ReadFloat();
            data.damage = reader.ReadFloat();
            data.range = reader.ReadFloat();
            data.speed = reader.ReadFloat();
            data.currentLevel = reader.ReadInt();
            data.maxLevel = reader.ReadInt();
            return data;
        };

        Debug.Log("[Mirror] Custom Writers/Readers for SpellSyncData registered successfully.");
    }
}

#endregion

// ========================================================================== //
// ==============================  NetworkEntity  =========================== //
// ========================================================================== //
[RequireComponent(typeof(StatsComponent))]
public class NetworkEntity : NetworkBehaviour
{
    // ----------------------- Spells ----------------------- //
    protected List<Spell> activeSpells = new List<Spell>();

    // Client list of spells for UI synchronization
    public readonly SyncList<SpellSyncData> syncedSpells = new SyncList<SpellSyncData>();

    [Header("Config")]
    [SerializeField] private StatsDataSO SO;

    public StatsComponent StatComp;

    public event Action OnDeath;

    protected virtual void Awake()
    {
        StatComp = GetComponent<StatsComponent>();

    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitStatsFromSO();


        OnDeath -= Die;
        OnDeath += Die;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Listen to changes in the syncedSpells list
        syncedSpells.Callback += OnSyncedSpellsChanged;

        // 2) initial sync (if the client arrives late)
        RebuildLocalSpellsFromSynced();
    }

    public override void OnStopClient()
    {
        // proprement se désabonner
        syncedSpells.Callback -= OnSyncedSpellsChanged;
        base.OnStopClient();
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        if (!isServer) return;
        UpdateSpells();
    }

    // ----------------------- Stats ----------------------- //

    public void InitStatsFromSO()
    {
        if (SO != null) StatComp.InitFromSO_Server(SO);
        else Debug.LogError("StatsDataSO not assigned in Stats component on " + name);
    }

    protected virtual void Die()
    {
        if (!isServer) return;

        if (TryGetComponent<NetworkIdentity>(out NetworkIdentity netIdentity))
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    [Server]
    public void ApplyDamageServer(float amount)
    {
        StatComp.stats[StatId.CurrentHealth] -= amount;
        if (StatComp.stats[StatId.CurrentHealth] <= 0f)
            OnDeath?.Invoke();
    }
    public void RequestDeathServer()
    {
        //need work
        if (!isServer) return;
    }
    // ----------------------- Spells ----------------------- //

    [Command]
    public void CmdCastSpell(string spellName)
    {
        var spell = GetSpellByName(spellName);
        if (spell == null) { Debug.LogWarning($"[SERVER] {spellName} introuvable"); return; }

        spell.ExecuteServer(this);
        RpcCastSpell(spellName); // tous les clients, y compris l’initiateur
    }

    [ClientRpc]
    void RpcCastSpell(string spellName)
    {
        var spell = GetSpellByName(spellName);
        spell?.ExecuteClient(this); // VFX / anim côté client
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
        Spell template = SpellsManager.Instance.GetSpell(spellName);
        if (template == null)
        {
            Debug.LogWarning($"[SERVER] AddSpell failed: spell '{spellName}' not found.");
            return;
        }

        Spell newSpell = (Spell)Activator.CreateInstance(template.GetType());
        Spell.SpellData newData = template.GetData().Clone();
        newSpell.Init(newData);

        activeSpells.Add(newSpell);
        newSpell.OnAdd(this);

        SpellSyncData syncData = new SpellSyncData(
            newData.spellName,
            newData.description,
            newData.manaCost,
            newData.cooldown,
            newData.damage,
            newData.range,
            newData.speed,
            newData.currentLevel,
            newData.maxLevel
        );

        syncedSpells.Add(syncData);
        Debug.Log($"[SERVER] Spell ajouté: {newData.spellName} à {StatComp.Name}");
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
        foreach (var spell in activeSpells)
            spell.UpdateSpell(this);
    }

    public void UpgradeSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        if (spell != null)
        {
            var upgradeMethod = spell.GetType().GetMethod("LevelUp");
            if (upgradeMethod != null)
            {
                upgradeMethod.Invoke(spell, null);
                Debug.Log($"[SERVER] {spellName} upgraded for {StatComp.Name}");
            }
            else
            {
                Debug.LogWarning($"[SERVER] Spell {spellName} does not have an Upgrade method.");
            }
        }
        else
        {
            Debug.LogWarning($"[SERVER] Spell {spellName} not found on {StatComp.Name}.");
        }
    }

    public Spell GetRandomSpellFromActivesSpells()
    {
        if (activeSpells.Count == 0) return null;
        int index = UnityEngine.Random.Range(0, activeSpells.Count);
        return activeSpells[index];
    }
    // Callback commun à TOUTES les opérations (add, removeAt, clear, set, insert)
    private void OnSyncedSpellsChanged(Mirror.SyncList<SpellSyncData>.Operation op, int index, SpellSyncData oldItem, SpellSyncData newItem)
    {
        // Peu importe l'opération, on régénère proprement la table locale
        RebuildLocalSpellsFromSynced();
    }

    // Reconstruit activeSpells côté CLIENT à partir de syncedSpells + SpellsManager
    private void RebuildLocalSpellsFromSynced()
    {
        if (!isClient) return;
        if (SpellsManager.Instance == null)
        {
            Debug.LogError("[CLIENT] SpellsManager non initialisé !");
            return;
        }

        // On jette l’ancien cache local et on repart de la vérité réseau
        activeSpells.Clear();

        foreach (var s in syncedSpells)
        {
            // Récupération depuis la banque (ta “DB” locale)
            Spell spell = SpellsManager.Instance.GetSpell(s.spellName);
            if (spell == null)
            {
                Debug.LogWarning($"[CLIENT] Spell '{s.spellName}' introuvable dans SpellsManager.");
                continue;
            }

            // Appliquer les champs dynamiques syncés
            var d = spell.GetData();
            d.currentLevel = s.currentLevel;
            d.maxLevel = s.maxLevel;
            d.cooldown = s.cooldown;
            d.manaCost = s.manaCost;
            d.damage = s.damage;
            d.description = s.description;
            spell.Init(d);

            activeSpells.Add(spell);
        }

        //Debug.Log($"[CLIENT] Rebuild spells OK, {activeSpells.Count} sorts pour {entityName}.");
    }

    private void LocalAddSpell(SpellSyncData data)
    {
        if (SpellsManager.Instance == null)
        {
            Debug.LogError("[CLIENT] SpellsManager non initialisé !");
            return;
        }

        // On récupère la définition depuis la banque (et pas un nouveau type inventé)
        Spell spell = SpellsManager.Instance.GetSpell(data.spellName);
        if (spell == null)
        {
            Debug.LogWarning($"[CLIENT] Spell '{data.spellName}' introuvable dans SpellsManager !");
            return;
        }

        // Appliquer les stats synchronisées (niveau, cooldown, etc.)
        var spellData = spell.GetData();
        spellData.currentLevel = data.currentLevel;
        spellData.maxLevel = data.maxLevel;
        spellData.cooldown = data.cooldown;
        spellData.manaCost = data.manaCost;
        spellData.damage = data.damage;
        spellData.description = data.description;
        spell.Init(spellData);

        activeSpells.Add(spell);
        Debug.Log($"[CLIENT] Spell '{spellData.spellName}' ajouté localement à {StatComp.Name}");
    }

    private void LocalRemoveSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        if (spell != null)
        {
            activeSpells.Remove(spell);
            Debug.Log($"[CLIENT] Spell '{spellName}' retiré localement de {StatComp.Name}");
        }
    }

    [Server]
    public void GainExperience(float amount)
    {
        StatComp.GainExperience(amount);
    }

    public List<Spell> GetAllActiveSpells() => activeSpells;
    public float GetHealthPourcentage() => (StatComp.stats[StatId.CurrentHealth] / StatComp.stats[StatId.MaxHealth]) * 100f;
}
