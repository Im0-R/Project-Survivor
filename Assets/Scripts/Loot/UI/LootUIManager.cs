using System.Collections.Generic;
using UnityEngine;

public class LootUIManager : MonoBehaviour
{
    public static LootUIManager Instance;

    [SerializeField] private LootLabelUI labelPrefab;
    [SerializeField] private Canvas canvas;

    private readonly Dictionary<LootPickup, LootLabelUI> labels =
        new Dictionary<LootPickup, LootLabelUI>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterLoot(LootPickup loot)
    {
        LootLabelUI label = Instantiate(labelPrefab, canvas.transform);
        label.Bind(loot);

        labels.Add(loot, label);
    }

    public void UnregisterLoot(LootPickup loot)
    {
        if (!labels.TryGetValue(loot, out LootLabelUI label))
            return;

        Destroy(label.gameObject);
        labels.Remove(loot);
    }

    public void RequestPickup(LootPickup loot)
    {
        if (PlayerPickupController.Local == null)
        {
            Debug.LogError("[LootUIManager] No local PlayerPickupController found!");
            return;
        }

        PlayerPickupController.Local.RequestPickup(loot);
    }
}