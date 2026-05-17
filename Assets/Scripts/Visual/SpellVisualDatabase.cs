using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Spell Visual Database")]
public class SpellVisualDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SpellVisualId id;
        public GameObject projectilePrefab;
        public GameObject impactPrefab;
    }

    [SerializeField] private Entry[] entries;

    public Entry Get(SpellVisualId id)
    {
        foreach (Entry entry in entries)
        {
            if (entry.id == id)
                return entry;
        }

        return null;
    }
}