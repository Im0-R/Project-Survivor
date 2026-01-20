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


    public void GenerateDrop(int itemLevel, int seed , Vector3 position)
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

        Debug.Log($"[LootManager] Generating drop: BaseID={itemBase.BaseId}, ItemLevel={itemLevel}, Seed={seed}");

        ItemInstance itemInstance = LootGenerator.Generate(itemBase, itemLevel, seed);

        GameObject objectToSpawn = itemObject;

        //Spawn the item in the world
        LootPickup lootPickup = objectToSpawn.GetComponent<LootPickup>();
        lootPickup.Init(itemInstance);

        NetworkServer.Spawn(objectToSpawn);

        objectToSpawn.transform.position = position;
    }
}
#endif
