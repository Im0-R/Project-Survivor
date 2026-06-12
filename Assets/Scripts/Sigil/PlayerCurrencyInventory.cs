using Mirror;
using UnityEngine;

public class PlayerCurrencyInventory : NetworkBehaviour
{
    public readonly SyncDictionary<int, int> Currencies = new();

    [Server]
    public bool AddCurrency(int currencyId, int amount)
    {
        if (amount <= 0)
            return false;

        CurrencySO currency = CurrencyDatabase.Get(currencyId);
        if (currency == null)
            return false;

        if (Currencies.ContainsKey(currencyId))
            Currencies[currencyId] += amount;
        else
            Currencies.Add(currencyId, amount);

        Debug.Log($"[CurrencyInventory] Added {amount}x {currency.currencyName}");
        return true;
    }

    [Server]
    public bool RemoveCurrency(int currencyId, int amount)
    {
        if (amount <= 0)
            return false;

        if (!Currencies.ContainsKey(currencyId))
            return false;

        if (Currencies[currencyId] < amount)
            return false;

        Currencies[currencyId] -= amount;

        if (Currencies[currencyId] <= 0)
            Currencies.Remove(currencyId);

        return true;
    }

    public int GetAmount(int currencyId)
    {
        return Currencies.TryGetValue(currencyId, out int amount) ? amount : 0;
    }

    [Command]
    public void CmdUseCurrencyOnItem(int currencyId, int itemIndex)
    {
        CurrencySO currency = CurrencyDatabase.Get(currencyId);
        if (currency == null || currency.effect == null)
            return;

        if (currency.effect is not ItemCurrencyEffectSO itemEffect)
            return;

        if (GetAmount(currencyId) <= 0)
            return;

        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
            return;

        ItemInstance item = inventory.GetItemByIndex(itemIndex);
        if (item == null || item.instanceId == 0)
            return;

        item.EnsureLists();

        System.Random rng = new System.Random();

        if (!itemEffect.CanUseOnItem(item))
            return;

        itemEffect.UseOnItem(item, rng);

        RemoveCurrency(currencyId, 1);
        inventory.SetSlot(itemIndex, item);

        Debug.Log($"[CurrencyInventory] Used {currency.currencyName} on {item.itemName}");
    }
}