using UnityEngine;

#if UNITY_SERVER
using Mirror;
#endif

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [SerializeField] private ItemBaseSO[] possibleDrops;

    private void Awake()
    {
        Instance = this;
    }

#if UNITY_SERVER
    public ItemInstance GenerateDrop(int itemLevel, int seed)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("[LootManager] GenerateDrop called but server not active!");
            return null;
        }

        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            Debug.LogError("[LootManager] possibleDrops empty!");
            return null;
        }

        System.Random rng = new System.Random(seed);
        ItemBaseSO itemBase = possibleDrops[rng.Next(0, possibleDrops.Length)];

        Debug.Log($"[LootManager] Generating drop: BaseID={itemBase.BaseId}, ItemLevel={itemLevel}, Seed={seed}");

        return LootGenerator.Generate(itemBase, itemLevel, seed);
    }
#endif
}
