using UnityEngine;

[CreateAssetMenu(menuName = "Game/Currencies/Effects/Sigil of Annulment")]
public class SigilOfAnnulmentEffectSO : ItemCurrencyEffectSO
{
    public override bool CanUseOnItem(ItemInstance item)
    {
        if (item == null || item.corrupted)
            return false;

        item.EnsureLists();

        return item.prefixes.Count > 0 || item.suffixes.Count > 0;
    }

    public override void UseOnItem(ItemInstance item, System.Random rng)
    {
        item.EnsureLists();

        int totalAffixes = item.prefixes.Count + item.suffixes.Count;

        if (totalAffixes <= 0)
            return;

        int roll = rng.Next(0, totalAffixes);

        if (roll < item.prefixes.Count)
            item.prefixes.RemoveAt(roll);
        else
            item.suffixes.RemoveAt(roll - item.prefixes.Count);

        if (item.TotalAffixCount == 0)
            item.rarity = ItemRarity.Normal;
    }
}