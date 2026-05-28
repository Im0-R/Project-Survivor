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
            Debug.LogError("[LootManager] GenerateDrops called but server not active!");
            return;
        }

        if (profile == null || profile.tableRolls == null || profile.tableRolls.Length == 0)
        {
            Debug.LogWarning("[LootManager] Empty loot profile.");
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

            Debug.Log($"[LootManager] Rolling table={tableRoll.table.name}, rolls={finalRolls}");

            for (int i = 0; i < finalRolls; i++)
            {
                LootTableEntry entry = tableRoll.table.RollOne(rng);

                if (entry == null)
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
        Vector3 position = GetDropPosition(centerPosition, rng);

        switch (entry.dropType)
        {
            case LootDropType.Item:
                SpawnItem(entry, itemLevel, rng, position);
                break;

            case LootDropType.Currency:
                SpawnCurrency(
                    entry,
                    profile.currencyQuantityMultiplier * extraCurrencyMultiplier,
                    rng,
                    position
                );
                break;

            case LootDropType.Gold:
                SpawnGold(
                    entry,
                    profile.goldQuantityMultiplier * extraGoldMultiplier,
                    rng,
                    position
                );
                break;
        }
    }

    private Vector3 GetDropPosition(Vector3 center, System.Random rng)
    {
        float x = (float)(rng.NextDouble() * 1.6f - 0.8f);
        float z = (float)(rng.NextDouble() * 1.6f - 0.8f);

        return center + new Vector3(x, 0f, z);
    }

    private void SpawnItem(LootTableEntry entry, int itemLevel, System.Random rng, Vector3 position)
    {
        if (entry.itemBase == null)
            return;

        if (itemObject == null)
        {
            Debug.LogError("[LootManager] itemObject prefab missing.");
            return;
        }

        ItemInstance itemInstance = LootGenerator.Generate(entry.itemBase, itemLevel, rng);

        GameObject obj = Instantiate(itemObject, position, Quaternion.identity);

        LootPickup pickup = obj.GetComponent<LootPickup>();
        if (pickup == null)
        {
            Debug.LogError("[LootManager] itemObject has no LootPickup.");
            Destroy(obj);
            return;
        }

        pickup.Init(itemInstance);
        NetworkServer.Spawn(obj);
    }

    private void SpawnCurrency(LootTableEntry entry, float multiplier, System.Random rng, Vector3 position)
    {
        if (entry.currency == null)
            return;

        if (currencyObject == null)
        {
            Debug.LogError("[LootManager] currencyObject prefab missing.");
            return;
        }

        int amount = RollAmount(entry, multiplier, rng);

        GameObject obj = Instantiate(currencyObject, position, Quaternion.identity);

        CurrencyPickup pickup = obj.GetComponent<CurrencyPickup>();
        if (pickup == null)
        {
            Debug.LogError("[LootManager] currencyObject has no CurrencyPickup.");
            Destroy(obj);
            return;
        }

        pickup.Init(entry.currency.currencyId, amount);
        NetworkServer.Spawn(obj);
    }

    private void SpawnGold(LootTableEntry entry, float multiplier, System.Random rng, Vector3 position)
    {
        int amount = RollAmount(entry, multiplier, rng);

        Debug.Log($"[LootManager] Gold dropped: {amount}");

        // Plus tard: spawn GoldPickup ici.
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
