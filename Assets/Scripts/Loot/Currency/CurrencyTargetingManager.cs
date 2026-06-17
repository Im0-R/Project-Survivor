using UnityEngine;

public class CurrencyTargetingManager : MonoBehaviour
{
    public static CurrencyTargetingManager Instance { get; private set; }

    private PlayerInventory inventory;
    private int selectedCurrencySlot = -1;
    private CurrencySO selectedCurrency;

    public bool IsTargetingItem => inventory != null && selectedCurrencySlot >= 0 && selectedCurrency != null;

    private void Awake()
    {
        Instance = this;
    }

    public void StartTargeting(PlayerInventory playerInventory, int currencySlot, CurrencySO currency)
    {
        inventory = playerInventory;
        selectedCurrencySlot = currencySlot;
        selectedCurrency = currency;

        Debug.Log($"[CurrencyTargeting] Selected {currency.DisplayName}. Click on an item.");
    }

    public void TryUseOnItem(int targetItemSlot)
    {
        if (!IsTargetingItem)
            return;

        inventory.CmdUseCurrencyOnItem(selectedCurrencySlot, targetItemSlot);

        Clear();
    }

    public void Clear()
    {
        inventory = null;
        selectedCurrencySlot = -1;
        selectedCurrency = null;
    }
}