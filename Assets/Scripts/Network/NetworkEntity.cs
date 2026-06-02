using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

#region Mirror custom writer/reader registration

public static class MirrorWritersRegistration
{
    [RuntimeInitializeOnLoadMethod]
    static void RegisterCustomWriters()
    {
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

[RequireComponent(typeof(StatsComponent))]
public class NetworkEntity : NetworkBehaviour
{
    protected List<Spell> activeSpells = new List<Spell>();

    public readonly SyncList<SpellSyncData> syncedSpells = new SyncList<SpellSyncData>();

    protected Dictionary<string, List<RuneSO>> attachedRunes = new();

    [SyncVar]
    private bool spellsEnabled = true;

    [SyncVar]
    private bool isDead;

    [Header("Config")]
    public StatsComponent StatComp;

    public event Action OnDeath;
    public event Action OnSpellsChanged;

    protected virtual void Awake()
    {
        StatComp = GetComponent<StatsComponent>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        isDead = false;

        OnDeath -= Die;
        OnDeath += Die;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        syncedSpells.Callback += OnSyncedSpellsChanged;
        RebuildLocalSpellsFromSynced();
    }

    public override void OnStopClient()
    {
        syncedSpells.Callback -= OnSyncedSpellsChanged;
        base.OnStopClient();
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        if (!isServer)
            return;

        if (ServerTimeManager.IsPaused)
            return;

        if (!spellsEnabled)
            return;

        if (isDead)
            return;

        UpdateSpells();
    }

    protected virtual void Die()
    {
        if (!isServer)
            return;

        if (TryGetComponent<NetworkIdentity>(out NetworkIdentity netIdentity))
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    [Server]
    public void EnableSpells()
    {
        spellsEnabled = true;
    }

    [Server]
    public void DisableSpells()
    {
        spellsEnabled = false;
    }

    [Server]
    public void AddArcanaWithRunes(string arcanaName, string[] runeIds)
    {
        Spell newSpell = SpellsManager.Instance.GetArcanaWithRunes(arcanaName, runeIds);

        if (newSpell == null)
        {
            Debug.LogWarning($"[SERVER] AddArcanaWithRunes failed: Arcana '{arcanaName}' not found.");
            return;
        }

        Spell.SpellData newData = newSpell.GetData();

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

        Debug.Log($"[SERVER] Arcana added: {newData.spellName} with {runeIds?.Length ?? 0} rune(s) to {StatComp.Name}");
    }

    [Server]
    public void ApplyDamageServer(float amount, bool isCrit = false)
    {
        if (StatComp == null)
            return;

        if (isDead)
            return;

        StatComp.TakeDamage(amount, isCrit);

        if (StatComp.Get(StatId.CurrentHealth) <= 0f)
        {
            if (isDead)
                return;

            isDead = true;
            DisableSpells();

            if (this is PlayerEntity player)
            {
                player.TargetSetDeadState(player.connectionToClient, true);
                player.ShowDeathCanvasServer();
                return;
            }

            OnDeath?.Invoke();
        }
    }

    public void RequestDeathServer()
    {
        if (!isServer)
            return;
    }

    [Command]
    public void CmdCastSpell(string spellName)
    {
        if (isDead)
            return;

        Spell spell = GetSpellByName(spellName);

        if (spell == null)
        {
            Debug.LogWarning($"[SERVER] {spellName} introuvable");
            return;
        }

        spell.ExecuteServer(this);
        RpcCastSpell(spellName);
    }

    [ClientRpc]
    public void RpcTriggerSpellCooldown(string spellName, float cooldown)
    {
        if (!isLocalPlayer)
            return;

        if (SpellsSlotsUI.Instance != null)
            SpellsSlotsUI.Instance.TriggerCooldown(spellName, cooldown);
    }

    [ClientRpc]
    private void RpcCastSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);
        spell?.ExecuteClient(this);

        if (!isLocalPlayer)
            return;

        if (spell == null || spell.GetData() == null)
            return;

        if (SpellsSlotsUI.Instance != null)
            SpellsSlotsUI.Instance.TriggerCooldown(spellName, spell.GetData().cooldown);
    }

    [Server]
    public void ClearRuntimeArcana()
    {
        foreach (Spell spell in activeSpells)
        {
            if (spell != null)
                spell.OnRemove(this);
        }

        activeSpells.Clear();
        syncedSpells.Clear();

        Debug.Log($"[SERVER] Runtime Arcana cleared for {StatComp.Name}");
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

        if (attachedRunes.TryGetValue(newData.spellName, out List<RuneSO> runes))
        {
            foreach (RuneSO rune in runes)
                rune.ApplyTo(newData);
        }

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
        foreach (Spell spell in activeSpells)
        {
            if (spell is T typedSpell)
                return typedSpell;
        }

        return null;
    }

    public Spell GetSpellByTypeName(string name)
    {
        foreach (Spell spell in activeSpells)
        {
            if (spell.GetType().Name == name)
                return spell;
        }

        return null;
    }

    public Spell GetSpellByName(string spellName)
    {
        foreach (Spell spell in activeSpells)
        {
            if (spell == null || spell.GetData() == null)
                continue;

            if (spell.GetData().spellName == spellName)
                return spell;
        }

        return null;
    }

    public void UpdateSpells()
    {
        foreach (Spell spell in activeSpells)
        {
            if (spell != null)
                spell.UpdateSpell(this);
        }
    }

    [Server]
    public void UpgradeSpell(string spellName)
    {
        Spell spell = GetSpellByName(spellName);

        if (spell != null)
        {
            System.Reflection.MethodInfo upgradeMethod = spell.GetType().GetMethod("LevelUp");

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

    [Server]
    public bool AddRuneToArcana(string arcanaName, RuneSO rune)
    {
        if (rune == null)
            return false;

        Spell spell = GetSpellByName(arcanaName);

        if (spell == null)
        {
            Debug.LogWarning($"[Arcana] Cannot attach rune, arcana not found: {arcanaName}");
            return false;
        }

        Spell.SpellData data = spell.GetData();

        if (!rune.CanApplyTo(data))
        {
            Debug.LogWarning($"[Arcana] Rune {rune.runeName} incompatible with {arcanaName}");
            return false;
        }

        if (!attachedRunes.ContainsKey(arcanaName))
            attachedRunes[arcanaName] = new List<RuneSO>();

        attachedRunes[arcanaName].Add(rune);

        rune.ApplyTo(data);

        Debug.Log($"[Arcana] Added rune {rune.runeName} to {arcanaName}");

        return true;
    }

    public Spell GetRandomSpellFromActivesSpells()
    {
        if (activeSpells.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, activeSpells.Count);
        return activeSpells[index];
    }

    private void OnSyncedSpellsChanged(
        SyncList<SpellSyncData>.Operation op,
        int index,
        SpellSyncData oldItem,
        SpellSyncData newItem)
    {
        RebuildLocalSpellsFromSynced();
    }

    private void RebuildLocalSpellsFromSynced()
    {
        if (!isClient) return;

        if (SpellsManager.Instance == null)
        {
            Debug.LogWarning("[CLIENT] SpellsManager non initialisé, impossible de rebuild les spells maintenant.");
            return;
        }

        activeSpells.Clear();

        foreach (SpellSyncData syncedSpell in syncedSpells)
        {
            Spell template = SpellsManager.Instance.GetSpell(syncedSpell.spellName);

            if (template == null)
            {
                Debug.LogWarning($"[CLIENT] Spell '{syncedSpell.spellName}' introuvable dans SpellsManager.");
                continue;
            }

            Spell newSpell = (Spell)Activator.CreateInstance(template.GetType());
            Spell.SpellData data = template.GetData().Clone();

            data.spellName = syncedSpell.spellName;
            data.description = syncedSpell.description;
            data.manaCost = syncedSpell.manaCost;
            data.cooldown = syncedSpell.cooldown;
            data.damage = syncedSpell.damage;
            data.range = syncedSpell.range;
            data.speed = syncedSpell.speed;
            data.currentLevel = syncedSpell.currentLevel;
            data.maxLevel = syncedSpell.maxLevel;

            newSpell.Init(data);
            activeSpells.Add(newSpell);
        }

        OnSpellsChanged?.Invoke();

        foreach (Spell spell in activeSpells)
        {
            Debug.Log($"[CLIENT] ActiveSpell = {spell.GetData().spellName}");
        }

        if (isLocalPlayer && CanvasArcana.Instance != null)
        {
            CanvasArcana.Instance.Refresh();

            if (SpellsSlotsUI.Instance != null)
                SpellsSlotsUI.Instance.Bind(this);
        }

        Debug.Log($"[CLIENT] Rebuild spells OK, {activeSpells.Count} spell(s) pour {name}.");
    }

    [Server]
    public void GainExperience(float amount)
    {
        StatComp.GainExperience(amount);
    }

    public List<Spell> GetAllActiveSpells()
    {
        return activeSpells;
    }

    public float GetHealthPourcentage()
    {
        return (StatComp.stats[StatId.CurrentHealth] / StatComp.stats[StatId.MaxHealth]) * 100f;
    }
}