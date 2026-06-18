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

    private void Start()
    {
        if (LootVisualManager.Instance != null)
            LootVisualManager.Instance.OnVisualSettingsChanged += RefreshAllLabels;
    }

    private void OnDestroy()
    {
        if (LootVisualManager.Instance != null)
            LootVisualManager.Instance.OnVisualSettingsChanged -= RefreshAllLabels;

        if (Instance == this)
            Instance = null;
    }

    public void RegisterLoot(LootPickup loot)
    {
        if (loot == null || labelPrefab == null || canvas == null)
            return;

        if (labels.ContainsKey(loot))
            return;

        LootLabelUI label = Instantiate(labelPrefab, canvas.transform);
        label.Bind(loot);

        labels.Add(loot, label);
    }

    public void UnregisterLoot(LootPickup loot)
    {
        if (loot == null)
            return;

        if (!labels.TryGetValue(loot, out LootLabelUI label))
            return;

        if (label != null)
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

    public void RefreshAllLabels()
    {
        foreach (LootLabelUI label in labels.Values)
        {
            if (label != null)
                label.RefreshLabel();
        }
    }
}
