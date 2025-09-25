using System;
using System.Linq;
using UnityEngine;
public class SpellsManager : MonoBehaviour
{
    public static SpellsManager Instance { get; private set; }

    [Header("Spells")]
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
    public Spell GetSpell(string spellName)
    {
        if (spellsDictionary.TryGetValue(spellName, out Spell.SpellData spellData))
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
    /// <summary>
    /// Return a random spell from the spellsDictionary
    /// </summary>
    public Spell GetRandomSpell()
    {
        if (spellsDictionary.Count == 0)
        {
            Debug.LogWarning("Aucun spellLinked dans le dictionnaire !");
            return null;
        }

        int index = UnityEngine.Random.Range(0, spellsDictionary.Count);
        Spell.SpellData spellData = spellsDictionary.ElementAt(index).Value;

        Type spellType = spellData.spellType.SpellType;
        if (spellType == null)
        {
            Debug.LogError("SpellType missing in SpellData !");
            return null;
        }
        Debug.Log($"Random spell selected: {spellData.spellName} of type {spellType.Name}");

        Spell spellInstance = (Spell)Activator.CreateInstance(spellType);

        spellInstance.Init(spellData);
        return spellInstance;
    }
    public Spell GetRandomSpellNotOwned()
    {
        var playerEnt = PlayerUI.Instance.playerEnt;
        var ownedSpellsNames = playerEnt.GetAllActiveSpells().Select(s => s.GetData().spellName).ToHashSet();
        var availableSpells = spellsDictionary.Values.Where(sd => !ownedSpellsNames.Contains(sd.spellName)).ToList();
        if (availableSpells.Count == 0)
        {
            Debug.LogWarning("Player already owns all spells !");
            return null;
        }
        int index = UnityEngine.Random.Range(0, availableSpells.Count);
        Spell.SpellData spellData = availableSpells[index];
        Type spellType = spellData.spellType.SpellType;
        if (spellType == null)
        {
            Debug.LogError("SpellType missing in SpellData !");
            return null;
        }
        Debug.Log($"Random not owned spell selected: {spellData.spellName} of type {spellType.Name}");
        Spell spellInstance = (Spell)Activator.CreateInstance(spellType);
        spellInstance.Init(spellData);
        return spellInstance;
    }
}
