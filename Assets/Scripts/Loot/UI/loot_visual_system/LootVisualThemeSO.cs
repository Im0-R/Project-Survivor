using System;
using UnityEngine;

public enum LootVisualCategory
{
    Normal,
    Magic,
    Rare,
    Unique,
    Sigil,
    Currency,
    Unknown
}

[Serializable]
public class LootCategoryVisual
{
    public Color lootLabelBackgroundColor = Color.white;
    public Color lootLabelTextColor = Color.black;
    public Color previewNameTextColor = Color.white;
    public Color itemCardTextColor = Color.white;
}

[CreateAssetMenu(menuName = "Game/Loot/Visual Theme", fileName = "LootVisualTheme")]
public class LootVisualThemeSO : ScriptableObject
{
    [Header("Categories")]
    [SerializeField] private LootCategoryVisual normal = new LootCategoryVisual
    {
        lootLabelBackgroundColor = new Color(0.28f, 0.28f, 0.28f, 0.95f),
        lootLabelTextColor = Color.white,
        previewNameTextColor = new Color(0.8f, 0.8f, 0.8f),
        itemCardTextColor = Color.white
    };

    [SerializeField] private LootCategoryVisual magic = new LootCategoryVisual
    {
        lootLabelBackgroundColor = new Color(0.18f, 0.32f, 0.68f, 0.95f),
        lootLabelTextColor = Color.white,
        previewNameTextColor = new Color(0.35f, 0.55f, 1f),
        itemCardTextColor = new Color(0.35f, 0.55f, 1f)
    };

    [SerializeField] private LootCategoryVisual rare = new LootCategoryVisual
    {
        lootLabelBackgroundColor = new Color(1f, 0.82f, 0.15f, 0.95f),
        lootLabelTextColor = Color.black,
        previewNameTextColor = new Color(1f, 0.85f, 0.2f),
        itemCardTextColor = new Color(1f, 0.85f, 0.2f)
    };

    [SerializeField] private LootCategoryVisual unique = new LootCategoryVisual
    {
        lootLabelBackgroundColor = new Color(1f, 0.48f, 0.08f, 0.95f),
        lootLabelTextColor = Color.black,
        previewNameTextColor = new Color(1f, 0.5f, 0.1f),
        itemCardTextColor = new Color(1f, 0.5f, 0.1f)
    };

    [SerializeField] private LootCategoryVisual sigil = new LootCategoryVisual
    {
        lootLabelBackgroundColor = Color.white,
        lootLabelTextColor = Color.black,
        previewNameTextColor = new Color(0.75f, 0.45f, 1f),
        itemCardTextColor = new Color(0.75f, 0.45f, 1f)
    };

    [SerializeField] private LootCategoryVisual currency = new LootCategoryVisual
    {
        lootLabelBackgroundColor = new Color(0.86f, 0.92f, 1f, 0.95f),
        lootLabelTextColor = Color.black,
        previewNameTextColor = new Color(0.35f, 0.85f, 1f),
        itemCardTextColor = new Color(0.35f, 0.85f, 1f)
    };

    [SerializeField] private LootCategoryVisual unknown = new LootCategoryVisual
    {
        lootLabelBackgroundColor = Color.gray,
        lootLabelTextColor = Color.white,
        previewNameTextColor = Color.white,
        itemCardTextColor = Color.white
    };

    [Header("Shared Preview Colors")]
    [SerializeField] private Color previewBackgroundColor = new Color(0.06f, 0.06f, 0.06f, 0.96f);
    [SerializeField] private Color previewBodyTextColor = Color.white;
    [SerializeField] private Color previewModTextColor = new Color(0.5f, 0.7f, 1f);

    [Header("Shared Item Card Colors")]
    [Tooltip("This background is used for every ItemCard, regardless of rarity or loot type.")]
    [SerializeField] private Color itemCardBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Header("Ground Label")]
    [SerializeField, Min(0.1f)] private float defaultLootLabelScale = 1f;

    public LootVisualStyle GetStyle(InventoryItemData data)
    {
        LootCategoryVisual categoryVisual = GetCategoryVisual(GetCategory(data));

        if (categoryVisual == null)
            categoryVisual = unknown;

        LootVisualStyle style = LootVisualStyle.CreateFallback();

        style.lootLabelBackgroundColor = categoryVisual.lootLabelBackgroundColor;
        style.lootLabelTextColor = categoryVisual.lootLabelTextColor;
        style.lootLabelScale = Mathf.Max(0.1f, defaultLootLabelScale);

        style.previewBackgroundColor = previewBackgroundColor;
        style.previewNameTextColor = categoryVisual.previewNameTextColor;
        style.previewBodyTextColor = previewBodyTextColor;
        style.previewModTextColor = previewModTextColor;

        style.itemCardBackgroundColor = itemCardBackgroundColor;
        style.itemCardTextColor = categoryVisual.itemCardTextColor;

        return style;
    }

    public LootVisualCategory GetCategory(InventoryItemData data)
    {
        if (data == null)
            return LootVisualCategory.Unknown;

        if (data.lootableType == LootableType.GeneratedItem ||
            !string.IsNullOrWhiteSpace(data.itemJson) ||
            data.hasRarityColor)
        {
            return data.rarity switch
            {
                ItemRarity.Normal => LootVisualCategory.Normal,
                ItemRarity.Magic => LootVisualCategory.Magic,
                ItemRarity.Rare => LootVisualCategory.Rare,
                ItemRarity.Unique => LootVisualCategory.Unique,
                _ => LootVisualCategory.Unknown
            };
        }

        if (data.lootableType == LootableType.Sigil)
            return LootVisualCategory.Sigil;

        if (data.lootableType == LootableType.Currency)
            return LootVisualCategory.Currency;

        return LootVisualCategory.Unknown;
    }

    private LootCategoryVisual GetCategoryVisual(LootVisualCategory category)
    {
        return category switch
        {
            LootVisualCategory.Normal => normal,
            LootVisualCategory.Magic => magic,
            LootVisualCategory.Rare => rare,
            LootVisualCategory.Unique => unique,
            LootVisualCategory.Sigil => sigil,
            LootVisualCategory.Currency => currency,
            _ => unknown
        };
    }
}
