using UnityEngine;

[CreateAssetMenu(menuName = "Game/Sigils/Effects/Sigil of Chaos")]
public class SigilOfChaosEffectSO : SigilEffectSO
{
    public override bool CanApply(ItemInstance item)
    {
        return item != null
            && item.rarity == ItemRarity.Rare
            && !item.corrupted;
    }

    public override void Apply(ItemInstance item, System.Random rng)
    {
        item.EnsureLists();

        item.prefixes.Clear();
        item.suffixes.Clear();

        LootGenerator.RollAffixesForExistingItem(item, rng);
    }
}