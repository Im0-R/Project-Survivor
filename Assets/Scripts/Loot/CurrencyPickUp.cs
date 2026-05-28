using Mirror;
using UnityEngine;

public class CurrencyPickup : NetworkBehaviour
{
    [SyncVar] private int currencyId;
    [SyncVar] private int amount;

    public int CurrencyId => currencyId;
    public int Amount => amount;

    [Server]
    public void Init(int id, int amountValue)
    {
        currencyId = id;
        amount = amountValue;
    }
}