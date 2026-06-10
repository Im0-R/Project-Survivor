using UnityEngine;

[CreateAssetMenu(menuName = "Game/Sigils/Effects/Sigil of Alchemy")]
public class SigilOfAlchemyEffectSO : SigilEffectSO
{
    public override bool CanApply(ItemInstance item)
    {
        return item != null
            && item.rarity == ItemRarity.Normal
            && !item.corrupted;
    }

    public override void Apply(ItemInstance item, System.Random rng)
    {
        item.EnsureLists();

        item.rarity = ItemRarity.Rare;
        item.prefixes.Clear();
        item.suffixes.Clear();

        LootGenerator.RollAffixesForExistingItem(item, rng);
    }
}