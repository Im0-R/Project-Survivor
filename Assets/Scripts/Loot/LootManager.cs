using Mirror;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject lootPickupPrefab;

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

                SpawnDrop(
                    entry,
                    profile,
                    itemLevel,
                    rng,
                    position,
                    extraCurrencyMultiplier,
                    extraGoldMultiplier
                );
            }
        }
    }

    private void SpawnDrop(
        LootTableEntry entry,
        LootProfileSO profile,
        int itemLevel,
        System.Random rng,
        Vector3 centerPosition,
        float extraCurrencyMultiplier,
        float extraGoldMultiplier)
    {
        Vector3 position = GetDropPosition(centerPosition, rng);

        if (entry.drop is ItemBaseSO itemBase)
        {
            ItemInstance itemInstance = LootGenerator.Generate(itemBase, itemLevel, rng);
            SpawnLootPickup(position, pickup => pickup.InitItem(itemInstance));

            Debug.Log($"[LOOT][SpawnItem] Spawned {itemInstance.itemName}");
            return;
        }

        if (entry.drop is CurrencySO currency)
        {
            float multiplier = profile.currencyQuantityMultiplier * extraCurrencyMultiplier;
            int amount = RollAmount(entry, multiplier, rng);

            SpawnLootPickup(position, pickup => pickup.InitCurrency(currency.CurrencyId, amount));

            Debug.Log($"[LOOT][SpawnCurrency] Spawned {currency.CurrencyName} x{amount}");
            return;
        }

        Debug.LogWarning($"[LOOT] Unsupported drop type: {entry.drop.GetType().Name}");
    }

    private void SpawnLootPickup(Vector3 position, System.Action<LootPickup> initAction)
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

        initAction?.Invoke(pickup);

        NetworkServer.Spawn(obj);
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

    private int RollAmount(LootTableEntry entry, float multiplier, System.Random rng)
    {
        int min = Mathf.Min(entry.minAmount, entry.maxAmount);
        int max = Mathf.Max(entry.minAmount, entry.maxAmount);

        int amount = rng.Next(min, max + 1);
        amount = Mathf.RoundToInt(amount * multiplier);

        return Mathf.Max(1, amount);
    }
}