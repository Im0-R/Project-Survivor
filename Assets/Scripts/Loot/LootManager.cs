using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject lootPickupPrefab;

    private readonly List<LootPayload> payloadBuffer = new List<LootPayload>();

    private void Awake()
    {
        Instance = this;
    }

    public void GenerateDrops(
        LootProfileSO profile,
        int itemLevel,
        int seed,
        Vector3 position,
        float extraQuantityMultiplier = 1f,
        float extraCurrencyMultiplier = 1f,
        float extraGoldMultiplier = 1f)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("[LOOT][GenerateDrops] Server not active!");
            return;
        }

        if (profile == null || profile.tableRolls == null || profile.tableRolls.Length == 0)
        {
            Debug.LogError("[LOOT][GenerateDrops] Invalid loot profile.");
            return;
        }

        System.Random rng = new System.Random(seed);

        LootSpawnContext context = new LootSpawnContext
        {
            profile = profile,
            itemLevel = Mathf.Max(1, itemLevel),
            rng = rng,
            extraCurrencyMultiplier = extraCurrencyMultiplier,
            extraGoldMultiplier = extraGoldMultiplier
        };

        foreach (LootTableRoll tableRoll in profile.tableRolls)
        {
            if (tableRoll == null || tableRoll.table == null)
                continue;

            if (!RollChance(rng, tableRoll.chanceToRoll))
                continue;

            int baseRolls = rng.Next(tableRoll.minRolls, tableRoll.maxRolls + 1);

            float quantity =
                profile.quantityMultiplier *
                tableRoll.quantityMultiplier *
                extraQuantityMultiplier;

            int finalRolls = Mathf.RoundToInt(baseRolls * quantity);
            finalRolls += profile.additionalRolls;
            finalRolls = Mathf.Max(0, finalRolls);

            for (int i = 0; i < finalRolls; i++)
            {
                LootTableEntry entry = tableRoll.table.RollOne(rng);

                if (entry == null || entry.drop == null)
                    continue;

                SpawnDrop(entry, context, position);
            }
        }
    }

    private void SpawnDrop(
        LootTableEntry entry,
        LootSpawnContext context,
        Vector3 centerPosition)
    {
        LootableSO lootable = entry.drop;

        if (lootable.SpawnBehaviour == null)
        {
            Debug.LogError($"[LOOT] Missing SpawnBehaviour on {lootable.name}");
            return;
        }

        payloadBuffer.Clear();

        lootable.SpawnBehaviour.BuildPayloads(
            lootable,
            entry,
            context,
            payloadBuffer
        );

        if (payloadBuffer.Count == 0)
            return;

        for (int i = 0; i < payloadBuffer.Count; i++)
        {
            LootPayload payload = payloadBuffer[i];

            if (payload == null || payload.lootableId == 0)
                continue;

            Vector3 position = GetDropPosition(centerPosition, context.rng);
            SpawnLootPickup(payload, position);
        }
    }

    private void SpawnLootPickup(LootPayload payload, Vector3 position)
    {
        if (lootPickupPrefab == null)
        {
            Debug.LogError("[LOOT][SpawnLootPickup] lootPickupPrefab missing.");
            return;
        }

        GameObject obj = Instantiate(lootPickupPrefab, position, Quaternion.identity);

        LootPickup pickup = obj.GetComponent<LootPickup>();

        if (pickup == null)
        {
            Debug.LogError("[LOOT][SpawnLootPickup] LootPickup missing on prefab.");
            Destroy(obj);
            return;
        }

        pickup.Init(payload);

        NetworkServer.Spawn(obj);

        Debug.Log($"[LOOT] Spawned {pickup.GetDisplayName()}");
    }

    private bool RollChance(System.Random rng, float chance)
    {
        if (chance <= 0f)
            return false;

        if (chance >= 100f)
            return true;

        float roll = (float)(rng.NextDouble() * 100f);
        return roll <= chance;
    }

    private Vector3 GetDropPosition(Vector3 center, System.Random rng)
    {
        float x = (float)(rng.NextDouble() * 1.6f - 0.8f);
        float z = (float)(rng.NextDouble() * 1.6f - 0.8f);

        return center + new Vector3(x, 0f, z);
    }
}