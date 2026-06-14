using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Effects/Sigil of Chaos")]
public class SigilOfChaosEffectSO : ItemCurrencyEffectSO
{
    public override bool CanUseOnItem(ItemInstance item)
    {
        return item != null
            && item.rarity == ItemRarity.Rare
            && !item.corrupted;
    }

    public override void UseOnItem(ItemInstance item, System.Random rng)
    {
        item.EnsureLists();
        LootGenerator.RollAffixesForExistingItem(item, rng);
    }
}