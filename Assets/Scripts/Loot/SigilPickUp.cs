using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class SigilPickUp : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SyncVar] private int sigilId;
    [SyncVar] private int amount;

    [SerializeField] private float pickupRadius = 2f;

    [Server]
    public void Init(int newSigilId, int newAmount)
    {
        sigilId = newSigilId;
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

        PlayerSigilInventory inventory =
            sender.identity.GetComponent<PlayerSigilInventory>();

        if (inventory == null)
            return;

        bool added = inventory.AddSigil(sigilId, amount);

        if (added)
            NetworkServer.Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SigilSO sigil = SigilDatabase.Get(sigilId);
        if (sigil == null)
            return;

        Debug.Log($"[CurrencyPickup] {sigil.sigilName} x{amount}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }
}