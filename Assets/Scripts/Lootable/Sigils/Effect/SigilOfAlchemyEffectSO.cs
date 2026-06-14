using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Effects/Sigil of Alchemy")]
public class SigilOfAlchemyEffectSO : ItemCurrencyEffectSO
{
    public override bool CanUseOnItem(ItemInstance item)
    {
        return item != null
            && item.rarity == ItemRarity.Normal
            && !item.corrupted;
    }

    public override void UseOnItem(ItemInstance item, System.Random rng)
    {
        item.EnsureLists();

        item.rarity = ItemRarity.Rare;
        item.prefixes.Clear();
        item.suffixes.Clear();

        LootGenerator.RollAffixesForExistingItem(item, rng);
    }
}