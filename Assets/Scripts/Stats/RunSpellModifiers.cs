using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RunSpellModifiers : NetworkBehaviour
{
    private readonly Dictionary<string, Dictionary<string, float>> modifiers = new();

    [Server]
    public void AddModifier(string spellName, string modifierName, float value)
    {
        if (!modifiers.ContainsKey(spellName))
            modifiers[spellName] = new Dictionary<string, float>();

        if (!modifiers[spellName].ContainsKey(modifierName))
            modifiers[spellName][modifierName] = 0f;

        modifiers[spellName][modifierName] += value;

        Debug.Log($"[RunSpellModifiers] {spellName} +{value} {modifierName}");
    }

    public float GetModifier(string spellName, string modifierName)
    {
        if (!modifiers.TryGetValue(spellName, out var spellMods))
            return 0f;

        return spellMods.TryGetValue(modifierName, out float value) ? value : 0f;
    }

    [Server]
    public void ClearModifiers()
    {
        modifiers.Clear();
        Debug.Log("[RunSpellModifiers] Cleared spell modifiers.");
    }
}