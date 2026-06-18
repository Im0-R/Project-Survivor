using System;
using UnityEngine;

public class LootVisualManager : MonoBehaviour
{
    public static LootVisualManager Instance { get; private set; }

    [SerializeField] private LootVisualThemeSO visualTheme;
    [SerializeField] private LootFilterSO activeLootFilter;

    public event Action OnVisualSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public LootVisualStyle Resolve(InventoryItemData data)
    {
        LootVisualStyle style = visualTheme != null
            ? visualTheme.GetStyle(data)
            : LootVisualStyle.CreateFallback();

        if (activeLootFilter != null)
            style = activeLootFilter.Evaluate(data, style);

        style.lootLabelScale = Mathf.Max(0.1f, style.lootLabelScale);

        return style;
    }

    public LootVisualStyle Resolve(LootPickup loot)
    {
        InventoryItemData data = loot != null
            ? loot.GetInventoryItemData()
            : null;

        return Resolve(data);
    }

    public void SetActiveFilter(LootFilterSO filter)
    {
        activeLootFilter = filter;
        OnVisualSettingsChanged?.Invoke();
    }

    public void RefreshVisuals()
    {
        OnVisualSettingsChanged?.Invoke();
    }
}
