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
        Debug.Log(
            $"[LOOT][GenerateDrops] " +
            $"serverActive={NetworkServer.active} " +
            $"profile={(profile != null ? profile.name : "NULL")} " +
            $"itemLevel={itemLevel} " +
            $"seed={seed} " +
            $"position={position}"
        );

        if (!NetworkServer.active)
        {
            Debug.LogError("[LOOT][GenerateDrops] Server not active!");
            return;
        }

        if (profile == null)
        {
            Debug.LogError("[LOOT][GenerateDrops] profile NULL");
            return;
        }

        if (profile.tableRolls == null)
        {
            Debug.LogError("[LOOT][GenerateDrops] tableRolls NULL");
            return;
        }

        if (profile.tableRolls.Length == 0)
        {
            Debug.LogError("[LOOT][GenerateDrops] tableRolls EMPTY");
            return;
        }

        System.Random rng = new System.Random(seed);

        Debug.Log($"[LOOT][GenerateDrops] tableRollCount={profile.tableRolls.Length}");

        foreach (LootTableRoll tableRoll in profile.tableRolls)
        {
            if (tableRoll == null)
            {
                Debug.LogWarning("[LOOT][TableRoll] tableRoll NULL");
                continue;
            }

            Debug.Log(
                $"[LOOT][TableRoll] " +
                $"table={(tableRoll.table != null ? tableRoll.table.name : "NULL")} " +
                $"chance={tableRoll.chanceToRoll} " +
                $"minRolls={tableRoll.minRolls} " +
                $"maxRolls={tableRoll.maxRolls} " +
                $"quantityMultiplier={tableRoll.quantityMultiplier}"
            );

            if (tableRoll.table == null)
            {
                Debug.LogWarning("[LOOT][TableRoll] table NULL");
                continue;
            }

            bool passedChance = RollChance(rng, tableRoll.chanceToRoll);

            Debug.Log($"[LOOT][Chance] passed={passedChance}");

            if (!passedChance)
                continue;

            int baseRolls = rng.Next(tableRoll.minRolls, tableRoll.maxRolls + 1);

            float quantity =
                profile.quantityMultiplier *
                tableRoll.quantityMultiplier *
                extraQuantityMultiplier;

            int finalRolls = Mathf.RoundToInt(baseRolls * quantity);
            finalRolls += profile.additionalRolls;
            finalRolls = Mathf.Max(0, finalRolls);

            Debug.Log(
                $"[LOOT][Rolls] " +
                $"baseRolls={baseRolls} " +
                $"quantity={quantity} " +
                $"finalRolls={finalRolls}"
            );

            for (int i = 0; i < finalRolls; i++)
            {
                LootTableEntry entry = tableRoll.table.RollOne(rng);

                Debug.Log(
                    $"[LOOT][RollOne] " +
                    $"entryNull={entry == null}"
                );

                if (entry == null)
                    continue;

                Debug.Log(
                    $"[LOOT][Entry] " +
                    $"dropType={entry.dropType} " +
                    $"itemBase={(entry.itemBase != null ? entry.itemBase.name : "NULL")} " +
                    $"currency={(entry.sigil != null ? entry.sigil.name : "NULL")} " +
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

        Debug.Log("[LOOT][GenerateDrops] END");
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
        Debug.Log(
            $"[LOOT][SpawnItem] " +
            $"itemBase={(entry.itemBase != null ? entry.itemBase.name : "NULL")} " +
            $"itemLevel={itemLevel} " +
            $"position={position}"
        );

        if (entry.itemBase == null)
        {
            Debug.LogError("[LOOT][SpawnItem] itemBase NULL");
            return;
        }

        if (itemObject == null)
        {
            Debug.LogError("[LOOT][SpawnItem] itemObject prefab missing");
            return;
        }

        ItemInstance itemInstance = LootGenerator.Generate(entry.itemBase, itemLevel, rng);

        Debug.Log(
            $"[LOOT][SpawnItem] GENERATED " +
            $"name={itemInstance.itemName} " +
            $"rarity={itemInstance.rarity}"
        );

        GameObject obj = Instantiate(itemObject, position, Quaternion.identity);

        Debug.Log($"[LOOT][SpawnItem] Instantiate OK obj={obj.name}");

        LootPickup pickup = obj.GetComponent<LootPickup>();

        if (pickup == null)
        {
            Debug.LogError("[LOOT][SpawnItem] LootPickup missing on prefab");
            Destroy(obj);
            return;
        }

        pickup.Init(itemInstance);

        Debug.Log("[LOOT][SpawnItem] pickup.Init OK");

        NetworkServer.Spawn(obj);

        Debug.Log("[LOOT][SpawnItem] NetworkServer.Spawn OK");
    }

    private void SpawnCurrency(LootTableEntry entry, float multiplier, System.Random rng, Vector3 position)
    {
        if (entry.sigil == null)
            return;

        if (currencyObject == null)
        {
            Debug.LogError("[LootManager] currencyObject prefab missing.");
            return;
        }

        int amount = RollAmount(entry, multiplier, rng);

        GameObject obj = Instantiate(currencyObject, position, Quaternion.identity);

        SigilPickUp pickup = obj.GetComponent<SigilPickUp>();
        if (pickup == null)
        {
            Debug.LogError("[LootManager] currencyObject has no CurrencyPickup.");
            Destroy(obj);
            return;
        }

        pickup.Init(entry.sigil.sigilId, amount); NetworkServer.Spawn(obj);
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
