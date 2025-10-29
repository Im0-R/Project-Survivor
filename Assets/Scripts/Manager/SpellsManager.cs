using System;
using System.Linq;
using UnityEngine;

public class SpellsManager : MonoBehaviour
{
    public static SpellsManager Instance { get; private set; }

    [Header("Spells Database")]
    [Tooltip("Liste des sorts disponibles dans le jeu.")]
    public SerializableDictionary<string, Spell.SpellData> spellsDictionary = new SerializableDictionary<string, Spell.SpellData>();

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
        Debug.Log($"[SpellsManager] {spellsDictionary.Count} spells enregistrés :");
        foreach (var key in spellsDictionary.Keys)
            Debug.Log($" - {key}");
    }

    /// <summary>
    /// Récupère une nouvelle instance de Spell à partir de son nom.
    /// </summary>
    public Spell GetSpell(string spellName)
    {
        if (spellsDictionary.TryGetValue(spellName, out Spell.SpellData spellData))
        {
            Type spellType = spellData.spellType.SpellType;
            if (spellType == null)
            {
                Debug.LogError($"[SpellsManager] SpellType manquant pour {spellName} !");
                return null;
            }

            Spell.SpellData clonedData = spellData.Clone();
            Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
            spellInstance.Init(clonedData);
            return spellInstance;
        }

        Debug.LogError($"[SpellsManager] Spell '{spellName}' introuvable dans le dictionnaire !");
        return null;
    }

    /// <summary>
    /// Retourne une instance aléatoire d'un spell (parmi tous les spells disponibles).
    /// </summary>
    public Spell GetRandomSpell()
    {
        if (spellsDictionary.Count == 0)
        {
            Debug.LogWarning("[SpellsManager] Aucun spell dans le dictionnaire !");
            return null;
        }

        int index = UnityEngine.Random.Range(0, spellsDictionary.Count);
        Spell.SpellData spellData = spellsDictionary.ElementAt(index).Value;

        Type spellType = spellData.spellType.SpellType;
        if (spellType == null)
        {
            Debug.LogError("[SpellsManager] SpellType manquant !");
            return null;
        }

        Spell.SpellData clonedData = spellData.Clone();
        Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
        spellInstance.Init(clonedData);
        return spellInstance;
    }

    /// <summary>
    /// Retourne une instance d'un spell aléatoire que le joueur ne possède pas encore.
    /// </summary>
    public Spell GetRandomSpellNotOwned()
    {
        var playerEnt = PlayerUI.Instance.playerEnt;
        var ownedSpells = playerEnt.GetAllActiveSpells()
            .Select(s => s.GetData().spellName)
            .ToHashSet();

        var availableSpells = spellsDictionary.Values
            .Where(sd => !ownedSpells.Contains(sd.spellName))
            .ToList();

        if (availableSpells.Count == 0)
        {
            Debug.LogWarning("[SpellsManager] Le joueur possède déjà tous les sorts !");
            return null;
        }

        int index = UnityEngine.Random.Range(0, availableSpells.Count);
        Spell.SpellData spellData = availableSpells[index];
        Type spellType = spellData.spellType.SpellType;
        if (spellType == null)
        {
            Debug.LogError("[SpellsManager] SpellType manquant !");
            return null;
        }

        Spell.SpellData clonedData = spellData.Clone();
        Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
        spellInstance.Init(clonedData);
        return spellInstance;
    }

    /// <summary>
    /// Retourne l'icône Sprite d'un spell à partir de son ID (spellName ou spellTypeID).
    /// </summary>
    public Sprite GetSpellIcon(string spellTypeID)
    {
        // 1. Essaye avec spellName
        if (spellsDictionary.TryGetValue(spellTypeID, out var dataByName))
            return dataByName.UISprite;

        // 2. Si c'est un type ID, on cherche dans les valeurs
        foreach (var kvp in spellsDictionary)
        {
            if (kvp.Value.spellTypeID == spellTypeID)
                return kvp.Value.UISprite;
        }

        Debug.LogWarning($"[SpellsManager] Icône introuvable pour '{spellTypeID}'");
        return null;
    }
}
