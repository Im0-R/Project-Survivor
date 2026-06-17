using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Effects/Sigil of Transmutation")]
public class SigilOfTransmutationEffectSO : ItemCurrencyEffectSO
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

        item.rarity = ItemRarity.Magic;
        item.prefixes.Clear();
        item.suffixes.Clear();

        LootGenerator.RollSingleAffixForExistingItem(item, rng);
    }
}