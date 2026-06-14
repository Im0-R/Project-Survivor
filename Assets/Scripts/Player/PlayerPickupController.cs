using Mirror;
using UnityEngine;

public class PlayerPickupController : NetworkBehaviour
{
    public static PlayerPickupController Local;

    [Header("Pickup rules")]
    [SerializeField] private float pickupRange = 4f;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Local = this;
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        if (Local == this)
            Local = null;
    }

    public void RequestPickup(LootPickup loot)
    {
        if (!isLocalPlayer)
            return;

        if (loot == null)
            return;

        float dist = Vector3.Distance(transform.position, loot.transform.position);

        if (dist > pickupRange + 1f)
            return;

        loot.RequestPickup();
    }
}