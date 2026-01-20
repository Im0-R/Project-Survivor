using UnityEngine;
using Mirror;

public class LootPickup : NetworkBehaviour
{
    [SyncVar] private string itemJson;
    [SyncVar] private uint ownerNetId;

    [SyncVar] public bool IsClaimed = false;

    [SerializeField] private float pickupRadius = 2f;
    public override void OnStartClient()
    {
        base.OnStartClient();
        LootUIManager.Instance.RegisterLoot(this);
    }
    // Server only: init
    [Server]
    public void Init(ItemInstance item)
    {
        itemJson = JsonUtility.ToJson(item);
    }

    public ItemInstance GetItem()
    {
        if (string.IsNullOrEmpty(itemJson)) return null;
        return JsonUtility.FromJson<ItemInstance>(itemJson);
    }

    public void RequestPickup()
    {
        if (!isClient) return;
        CmdRequestPickup();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestPickup(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;

        if (ownerNetId != 0 && sender.identity.netId != ownerNetId)
            return;

        float dist = Vector3.Distance(sender.identity.transform.position, transform.position);
        if (dist > pickupRadius) return;

        ItemInstance item = JsonUtility.FromJson<ItemInstance>(itemJson);

        if (item == null) return;

        PlayerInventory inv = sender.identity.GetComponent<PlayerInventory>();

        if (inv == null) return;

        inv.Server_AddItem(item);

        NetworkServer.Destroy(gameObject);
    }
}
