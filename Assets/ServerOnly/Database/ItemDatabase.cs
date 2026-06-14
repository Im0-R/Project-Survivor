    using System.Collections.Generic;
    using UnityEngine;

    public static class ItemDatabase
    {
        private static Dictionary<int, ItemBaseSO> itemsById;
        private static Dictionary<string, ItemBaseSO> itemsByName;
        private static bool initialized = false;

        public static void Initialize()
        {
            if (initialized) return;

            itemsById = new Dictionary<int, ItemBaseSO>();
            itemsByName = new Dictionary<string, ItemBaseSO>();

            ItemBaseSO[] allItems = Resources.LoadAll<ItemBaseSO>("");

            if (allItems == null || allItems.Length == 0)
            {
                Debug.LogError("[ItemDatabase] No ItemBaseSO found! Place them in Resources/.");
                initialized = true;
                return;
            }

            foreach (var item in allItems)
            {
                if (item == null) continue;

                if (itemsById.ContainsKey(item.BaseId))
                {
                    Debug.LogError($"[ItemDatabase] Duplicate baseId {item.BaseId} ({item.name})");
                    continue;
                }

                itemsById[item.BaseId] = item;

                if (!string.IsNullOrWhiteSpace(item.BaseName))
                    itemsByName[item.BaseName] = item;
            }

            initialized = true;
            Debug.Log($"[ItemDatabase] Loaded {itemsById.Count} item bases.");
        }

        public static ItemBaseSO GetBase(int baseId)
        {
            if (!initialized) Initialize();
            itemsById.TryGetValue(baseId, out var item);
            return item;
        }

        public static ItemBaseSO GetBase(string baseName)
        {
            if (!initialized) Initialize();
            itemsByName.TryGetValue(baseName, out var item);
            return item;
        }

        public static IReadOnlyCollection<ItemBaseSO> GetAllBases()
        {
            if (!initialized) Initialize();
            return itemsById.Values;
        }
    }