using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SpellCasterServer : NetworkBehaviour
{
    private readonly Dictionary<SpellCastMode, ISpellExecutor> executors = new();

    private void Awake()
    {
        executors[SpellCastMode.Projectile] = new ProjectileSpellExecutor(this);
    }

    [Server]
    public void Cast(Spell.SpellData data, NetworkEntity owner)
    {
        if (data == null || owner == null) return;

        if (!executors.TryGetValue(data.castMode, out ISpellExecutor executor))
        {
            Debug.LogWarning($"[SpellCasterServer] No executor for castMode={data.castMode}");
            return;
        }

        executor.Execute(data, owner);
    }
}