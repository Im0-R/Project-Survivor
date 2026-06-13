using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class CurrencyPickUp : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SyncVar] private int currencyId;
    [SyncVar] private int amount;

    [SerializeField] private float pickupRadius = 2f;

    [Server]
    public void Init(int newCurrencyId, int newAmount)
    {
        currencyId = newCurrencyId;
        amount = Mathf.Max(1, newAmount);
    }

    public void RequestPickup()
    {
        if (!isClient)
            return;

        CmdRequestPickup();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestPickup(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null)
            return;

        float distance = Vector3.Distance(
            sender.identity.transform.position,
            transform.position
        );

        if (distance > pickupRadius)
            return;

        PlayerCurrencyInventory inventory =
            sender.identity.GetComponent<PlayerCurrencyInventory>();

        if (inventory == null)
            return;

        bool added = inventory.AddCurrency(currencyId, amount);

        if (added)
            NetworkServer.Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CurrencySO currency = CurrencyDatabase.Get(currencyId);
        if (currency == null)
            return;

        Debug.Log($"[CurrencyPickup] {currency.DisplayName} x{amount}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }
}