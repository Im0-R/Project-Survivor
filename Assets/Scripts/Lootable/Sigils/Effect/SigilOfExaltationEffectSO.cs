using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Effects/Sigil of Exaltation")]
public class SigilOfExaltationEffectSO : ItemCurrencyEffectSO
{
    [SerializeField] private int maxPrefixes = 3;
    [SerializeField] private int maxSuffixes = 3;

    public override bool CanUseOnItem(ItemInstance item)
    {
        if (item == null || item.corrupted)
            return false;

        item.EnsureLists();

        if (item.rarity != ItemRarity.Rare)
            return false;

        return item.prefixes.Count < maxPrefixes || item.suffixes.Count < maxSuffixes;
    }

    public override void UseOnItem(ItemInstance item, System.Random rng)
    {
        item.EnsureLists();
        LootGenerator.RollSingleAffixForExistingItem(item, rng);
    }
}