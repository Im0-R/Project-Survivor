using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central spell database & factory (shared between client and server).
/// </summary>
public class SpellsManager : MonoBehaviour
{
    public static SpellsManager Instance { get; private set; }

    [Header("Spells Database")]
    [Tooltip("All available spells (used by both client and server)")]
    public SerializableDictionary<string, Spell.SpellData> spellsDictionary = new();

    // ==========================================================
    // == INITIALIZATION ========================================
    // ==========================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if !UNITY_CLIENT
        Debug.Log($"[SpellsManager] {spellsDictionary.Count} spells registered PROUT PROUT:");
        foreach (var key in spellsDictionary.Keys)
            Debug.Log($"  - {key}");
#endif
    }

    // ==========================================================
    // == SHARED METHODS (usable by both Client & Server) =======
    // ==========================================================

    /// <summary>
    /// Instantiates a new Spell by its name.
    /// </summary>
    public Spell GetSpell(string spellName)
    {
        if (!spellsDictionary.TryGetValue(spellName, out Spell.SpellData spellData))
        {
            Debug.LogError($"[SpellsManager] Spell '{spellName}' not found in dictionary!");
            return null;
        }

        Type spellType = spellData.spellType.SpellType;
        if (spellType == null)
        {
            Debug.LogError($"[SpellsManager] Missing SpellType for '{spellName}'!");
            return null;
        }

        Spell.SpellData clonedData = spellData.Clone();
        Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
        spellInstance.Init(clonedData);
        return spellInstance;
    }

    /// <summary>
    /// Returns a random spell from the entire dictionary.
    /// </summary>
    public Spell GetRandomSpell()
    {
        if (spellsDictionary.Count == 0)
        {
            Debug.LogWarning("[SpellsManager] No spells registered!");
            return null;
        }

        int index = UnityEngine.Random.Range(0, spellsDictionary.Count);
        Spell.SpellData spellData = spellsDictionary.ElementAt(index).Value;

        if (spellData.spellType?.SpellType == null)
        {
            Debug.LogError("[SpellsManager] Invalid SpellType!");
            return null;
        }

        Spell.SpellData clonedData = spellData.Clone();
        Spell spellInstance = (Spell)Activator.CreateInstance(spellData.spellType.SpellType);
        spellInstance.Init(clonedData);
        return spellInstance;
    }

    /// <summary>
    /// Returns a random spell not already owned, given a set of owned spell names.
    /// Safe to call from the server.
    /// </summary>
    public Spell GetRandomSpellServer(System.Collections.Generic.HashSet<string> ownedSpellNames)
    {
        var available = spellsDictionary.Values
            .Where(sd => !ownedSpellNames.Contains(sd.spellName))
            .ToList();

        if (available.Count == 0)
        {
            Debug.LogWarning("[SpellsManager] No unowned spells available (Server)!");
            return null;
        }

        int index = UnityEngine.Random.Range(0, available.Count);
        Spell.SpellData spellData = available[index];

        if (spellData.spellType?.SpellType == null)
        {
            Debug.LogError("[SpellsManager] Invalid SpellType (Server)!");
            return null;
        }

        Spell.SpellData clonedData = spellData.Clone();
        Spell spellInstance = (Spell)Activator.CreateInstance(spellData.spellType.SpellType);
        spellInstance.Init(clonedData);
        return spellInstance;
    }

    // ==========================================================
    // == CLIENT-ONLY METHODS ==================================
    // ==========================================================
#if !UNITY_SERVER
    /// <summary>
    /// Returns a random spell the player does not yet own (Client only).
    /// </summary>
    public Spell GetRandomSpellClient()
    {
        if (PlayerUI.Instance == null || PlayerUI.Instance.playerEnt == null)
        {
            Debug.LogWarning("[SpellsManager] PlayerUI or playerEnt not found!");
            return null;
        }

        var owned = PlayerUI.Instance.playerEnt.GetAllActiveSpells()
            .Select(s => s.GetData().spellName)
            .ToHashSet();

        return GetRandomSpellServer(owned);
    }
#endif

    // ==========================================================
    // == UI / ICON ============================================
    // ==========================================================
    /// <summary>
    /// Returns a spell icon by name or type ID.
    /// </summary>
    public Sprite GetSpellIcon(string spellName)
    {
        // Try direct key lookup
        if (spellsDictionary.TryGetValue(spellName, out var data))
            return data.UISprite;

        // Try by internal name
        foreach (var kvp in spellsDictionary)
        {
            if (kvp.Value.spellName == spellName)
                return kvp.Value.UISprite;
        }

        Debug.LogWarning($"[SpellsManager] Icon not found for '{spellName}'");
        return null;
    }
}
