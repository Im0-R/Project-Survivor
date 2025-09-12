using System;
using UnityEngine;
using static Spell;

public class SpellsManager : MonoBehaviour
{
    public static SpellsManager Instance { get; private set; }

    [Header("Spells")]
    public SerializableDictionary<string, SpellData> spellsDictionary = new SerializableDictionary<string, SpellData>();

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

    /// <summary>
    /// Return a random spell from the spellsDictionary
    /// </summary>
    public Spell GetRandomSpell()
    {
        if (spellsDictionary.Count == 0)
        {
            Debug.LogWarning("Aucun spell dans le dictionnaire !");
            return null;
        }

        int index = UnityEngine.Random.Range(0, spellsDictionary.Count);
        var spellData = spellsDictionary.ElementAt(index).Value;

        Type spellType = spellData.spellType.SpellType;
        if (spellType == null)
        {
            Debug.LogError("SpellType missing in SpellData !");
            return null;
        }

        Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
        spellInstance.Init(spellData);
        return spellInstance;
    }
    public Spell GetSpell(string spellName)
    {
        if (spellsDictionary.TryGetValue(spellName, out SpellData spellData))
        {
            Type spellType = spellData.spellType.SpellType;
            if (spellType == null)
            {
                Debug.LogError("SpellType missing in SpellData !");
                return null;
            }
            Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
            spellInstance.Init(spellData);
            return spellInstance;

        }
        Debug.LogError($"Spell '{spellName}' not found in dictionary !");
        return null;
    }
}
