using System.Collections.Generic;
using UnityEngine;

public static class CurrencyDatabase
{
    private static Dictionary<int, CurrencySO> currenciesById;
    private static CurrencySO[] allCurrencies;

    public static void Initialize()
    {
        allCurrencies = Resources.LoadAll<CurrencySO>("ScriptableObjects/Currencies");
        currenciesById = new Dictionary<int, CurrencySO>();

        foreach (CurrencySO currency in allCurrencies)
        {
            if (currency == null)
                continue;

            if (currenciesById.ContainsKey(currency.currencyId))
            {
                Debug.LogError($"[CurrencyDatabase] Duplicate currencyId={currency.currencyId}");
                continue;
            }

            currenciesById.Add(currency.currencyId, currency);
        }

        Debug.Log($"[CurrencyDatabase] Loaded {currenciesById.Count} currencies.");
    }

    public static CurrencySO Get(int currencyId)
    {
        if (currenciesById == null)
            Initialize();

        currenciesById.TryGetValue(currencyId, out CurrencySO currency);
        return currency;
    }

    public static CurrencySO[] GetAll()
    {
        if (allCurrencies == null)
            Initialize();

        return allCurrencies;
    }
}