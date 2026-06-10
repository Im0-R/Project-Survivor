using UnityEngine;

[CreateAssetMenu(menuName = "Game/Sigils/Effects/Sigil of Transmutation")]
public class SigilOfTransmutationEffectSO : SigilEffectSO
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

        item.rarity = ItemRarity.Magic;
        item.prefixes.Clear();
        item.suffixes.Clear();

        LootGenerator.RollSingleAffixForExistingItem(item, rng);
    }
}