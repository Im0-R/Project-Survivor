using System.Collections.Generic;
using UnityEngine;

public abstract class LootSpawnBehaviourSO : ScriptableObject
{
    public abstract void BuildPayloads(
        LootableSO lootable,
        LootTableEntry entry,
        LootSpawnContext context,
        List<LootPayload> results
    );
}