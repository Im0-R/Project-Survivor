public abstract class ItemCurrencyEffectSO : CurrencyEffectSO
{
    public abstract bool CanUseOnItem(ItemInstance item);
    public abstract void UseOnItem(ItemInstance item, System.Random rng);
}