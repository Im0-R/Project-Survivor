#if UNITY_SERVER
using Mirror;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [SerializeField] private ItemBaseSO[] possibleDrops;
    [SerializeField] private GameObject itemObject;

    private void Awake()
    {
        Instance = this;
    }

    public void GenerateDrop(int itemLevel, int seed, Vector3 position)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("[LootManager] GenerateDrop called but server not active!");
            return;
        }

        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            Debug.LogError("[LootManager] possibleDrops empty!");
            return;
        }

        System.Random rng = new System.Random(seed);

        ItemBaseSO itemBase = possibleDrops[rng.Next(0, possibleDrops.Length)];

        if (itemBase == null)
        {
            Debug.LogError("[LootManager] Selected itemBase is null!");
            return;
        }

        Debug.Log($"[LootManager] Generating drop: Item={itemBase.BaseName}, BaseID={itemBase.BaseId}, ItemLevel={itemLevel}, Seed={seed}");

        ItemInstance itemInstance = LootGenerator.Generate(itemBase, itemLevel, rng);

        GameObject objectToSpawn = Instantiate(itemObject, position, Quaternion.identity);

        LootPickup lootPickup = objectToSpawn.GetComponent<LootPickup>();

        if (lootPickup == null)
        {
            Debug.LogError("[LootManager] Spawned itemObject has no LootPickup component!");
            Destroy(objectToSpawn);
            return;
        }

        lootPickup.Init(itemInstance);

        NetworkServer.Spawn(objectToSpawn);
    }
}
#endif