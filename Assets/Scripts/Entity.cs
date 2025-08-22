using System.Collections.Generic;
using UnityEngine;

public class Entity
{
    public NetworkEntity NetEntity { get; private set; }
    protected List<Spell> activeSpells = new List<Spell>();

    public Entity(NetworkEntity netEntity = null)
    {
        NetEntity = netEntity;
    }
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

    public virtual void Update()
    {
        foreach (var spell in activeSpells)
        {
            spell.UpdateSpell(this);
        }
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
}
