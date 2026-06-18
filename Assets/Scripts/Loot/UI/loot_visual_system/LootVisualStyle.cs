using System;
using UnityEngine;

[Serializable]
public struct LootVisualStyle
{
    public bool visible;

    [Header("Ground Label")]
    public Color lootLabelBackgroundColor;
    public Color lootLabelTextColor;
    public float lootLabelScale;

    [Header("Item Preview")]
    public Color previewBackgroundColor;
    public Color previewNameTextColor;
    public Color previewBodyTextColor;
    public Color previewModTextColor;

    [Header("Item Card")]
    public Color itemCardBackgroundColor;
    public Color itemCardTextColor;

    public static LootVisualStyle CreateFallback()
    {
        return new LootVisualStyle
        {
            visible = true,

            lootLabelBackgroundColor = Color.white,
            lootLabelTextColor = Color.black,
            lootLabelScale = 1f,

            previewBackgroundColor = new Color(0.06f, 0.06f, 0.06f, 0.96f),
            previewNameTextColor = Color.white,
            previewBodyTextColor = Color.white,
            previewModTextColor = new Color(0.5f, 0.7f, 1f),

            itemCardBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f),
            itemCardTextColor = Color.white
        };
    }
}
