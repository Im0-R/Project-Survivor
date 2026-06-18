using UnityEngine;

[CreateAssetMenu(menuName = "Game/Loot/Filter", fileName = "LootFilter")]
public class LootFilterSO : ScriptableObject
{
    // This is intentionally simple for now.
    // Later, a PoE-like filter can inspect item level, rarity, base type,
    // affixes, stack size, currency type, and then hide or restyle the loot.
    public virtual LootVisualStyle Evaluate(
        InventoryItemData data,
        LootVisualStyle defaultStyle)
    {
        return defaultStyle;
    }
}
