using Mirror;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject itemObject;
    [SerializeField] private GameObject currencyObject;

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

                Debug.Log(
                    $"[LOOT][Entry] " +
                    $"drop={entry.drop.DisplayName} " +
                    $"minAmount={entry.minAmount} " +
                    $"maxAmount={entry.maxAmount}"
                );

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

    private bool RollChance(System.Random rng, float chance)
    {
        if (chance <= 0f)
            return false;

        if (chance >= 100f)
            return true;

        float roll = (float)(rng.NextDouble() * 100f);
        return roll <= chance;
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
        if (entry == null || entry.drop == null)
            return;

        Vector3 position = GetDropPosition(centerPosition, rng);

        if (entry.drop is LootableSO itemBase)
        {
            SpawnItem(itemBase, itemLevel, rng, position);
            return;
        }

        if (entry.drop is CurrencySO currency)
        {
            float multiplier = profile.currencyQuantityMultiplier * extraCurrencyMultiplier;
            int amount = RollAmount(entry, multiplier, rng);

            SpawnCurrency(currency, amount, position);
            return;
        }

        Debug.LogWarning($"[LOOT] Unsupported drop type: {entry.drop.GetType().Name}");
    }

    private Vector3 GetDropPosition(Vector3 center, System.Random rng)
    {
        float x = (float)(rng.NextDouble() * 1.6f - 0.8f);
        float z = (float)(rng.NextDouble() * 1.6f - 0.8f);

        return center + new Vector3(x, 0f, z);
    }

    private void SpawnItem(
        ItemBaseSO itemBase,
        int itemLevel,
        System.Random rng,
        Vector3 position)
    {
        if (itemBase == null)
            return;

        if (itemObject == null)
        {
            Debug.LogError("[LOOT][SpawnItem] itemObject prefab missing.");
            return;
        }

        ItemInstance itemInstance = LootGenerator.Generate(itemBase, itemLevel, rng);

        GameObject obj = Instantiate(itemObject, position, Quaternion.identity);

        LootPickup pickup = obj.GetComponent<LootPickup>();

        if (pickup == null)
        {
            Debug.LogError("[LOOT][SpawnItem] LootPickup missing on prefab.");
            Destroy(obj);
            return;
        }

        pickup.InitItem(itemInstance);

        NetworkServer.Spawn(obj);

        Debug.Log($"[LOOT][SpawnItem] Spawned {itemInstance.itemName}");
    }

    private void SpawnCurrency(
        CurrencySO currency,
        int amount,
        Vector3 position)
    {
        if (currency == null)
            return;

        if (currencyObject == null)
        {
            Debug.LogError("[LOOT][SpawnCurrency] currencyObject prefab missing.");
            return;
        }

        GameObject obj = Instantiate(currencyObject, position, Quaternion.identity);

        LootPickup pickup = obj.GetComponent<LootPickup>();

        if (pickup == null)
        {
            Debug.LogError("[LOOT][SpawnCurrency] LootPickup missing on currency prefab.");
            Destroy(obj);
            return;
        }

        pickup.InitCurrency(currency.CurrencyId, amount);

        NetworkServer.Spawn(obj);

        Debug.Log($"[LOOT][SpawnCurrency] Spawned {currency.CurrencyName} x{amount}");
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