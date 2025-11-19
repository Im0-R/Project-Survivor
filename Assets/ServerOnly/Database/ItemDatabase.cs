using System.Collections.Generic;
using UnityEngine;

public static class ItemDatabase
{
    private static Dictionary<int, ItemDataSO> itemsById;
    private static Dictionary<string, ItemDataSO> itemsByName;
    private static bool initialized = false;

    // ============================================================
    // INIT
    // ============================================================
    public static void Initialize()
    {
        if (initialized) return;

        itemsById = new Dictionary<int, ItemDataSO>();
        itemsByName = new Dictionary<string, ItemDataSO>();

        // Charge tous les items dans Resources, peu importe le dossier
        ItemDataSO[] allItems = Resources.LoadAll<ItemDataSO>("");

        if (allItems.Length == 0)
        {
            Debug.LogError("[ItemDatabase] Aucun ItemDataSO trouvé ! " +
                           "Place tes items dans un dossier Resources/");
        }

        foreach (var item in allItems)
        {
            // Vérifie doublon ID
            if (itemsById.ContainsKey(item.itemId))
            {
                Debug.LogError($"[ItemDatabase] Duplicate ID {item.itemId} for item {item.name}");
                continue;
            }

            itemsById[item.itemId] = item;

            // Vérifie doublon nom
            if (!string.IsNullOrWhiteSpace(item.itemName))
                itemsByName[item.itemName] = item;
        }

        initialized = true;
        Debug.Log($"[ItemDatabase] Loaded {itemsById.Count} items.");
    }

    // ============================================================
    // GETTERS
    // ============================================================
    public static ItemDataSO GetItem(int id)
    {
        if (!initialized) Initialize();

        itemsById.TryGetValue(id, out ItemDataSO item);
        if (item == null)
            Debug.LogWarning($"[ItemDatabase] No item with ID {id}");

        return item;
    }

    public static ItemDataSO GetItem(string name)
    {
        if (!initialized) Initialize();

        itemsByName.TryGetValue(name, out ItemDataSO item);
        if (item == null)
            Debug.LogWarning($"[ItemDatabase] No item named '{name}'");

        return item;
    }

    public static List<ItemDataSO> GetAllItems()
    {
        if (!initialized) Initialize();
        return new List<ItemDataSO>(itemsById.Values);
    }
}
