using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PickupType
{
    Item,
    Currency
}

public class LootPickup : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SyncVar] private PickupType pickupType;

    [SyncVar] private string itemJson = "";
    [SyncVar] private int currencyId;
    [SyncVar] private int amount;

    [SyncVar] private uint ownerNetId;
    [SyncVar] public bool IsClaimed = false;

    [SerializeField] private float pickupRadius = 2f;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (LootUIManager.Instance != null)
            LootUIManager.Instance.RegisterLoot(this);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (LootUIManager.Instance != null)
            LootUIManager.Instance.UnregisterLoot(this);
    }

    [Server]
    public void InitItem(ItemInstance item)
    {
        pickupType = PickupType.Item;
        itemJson = item != null ? JsonUtility.ToJson(item) : "";
        currencyId = 0;
        amount = 0;
    }

    [Server]
    public void InitCurrency(int newCurrencyId, int newAmount)
    {
        pickupType = PickupType.Currency;
        itemJson = "";
        currencyId = newCurrencyId;
        amount = Mathf.Max(1, newAmount);
    }

    public ItemInstance GetItem()
    {
        if (pickupType != PickupType.Item)
            return null;

        if (string.IsNullOrWhiteSpace(itemJson))
            return null;

        ItemInstance item = JsonUtility.FromJson<ItemInstance>(itemJson);
        item?.EnsureLists();

        return item;
    }

    public CurrencySO GetCurrency()
    {
        if (pickupType != PickupType.Currency)
            return null;

        return CurrencyDatabase.Get(currencyId);
    }

    public int GetAmount()
    {
        return amount;
    }

    public PickupType GetPickupType()
    {
        return pickupType;
    }

    public string GetDisplayName()
    {
        if (pickupType == PickupType.Item)
        {
            ItemInstance item = GetItem();

            if (item == null)
                return "Unknown Item";

            return item.itemName;
        }

        if (pickupType == PickupType.Currency)
        {
            CurrencySO currency = GetCurrency();

            if (currency == null)
                return $"Currency x{amount}";

            return amount > 1
                ? $"{currency.DisplayName} x{amount}"
                : currency.DisplayName;
        }

        return "Unknown Loot";
    }

    public Sprite GetIcon()
    {
        if (pickupType == PickupType.Currency)
        {
            CurrencySO currency = GetCurrency();
            return currency != null ? currency.Icon : null;
        }

        if (pickupType == PickupType.Item)
        {
            ItemInstance item = GetItem();

            if (item == null)
                return null;

            ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);
            return itemBase != null ? itemBase.Icon : null;
        }

        return null;
    }

    public Color GetLabelColor()
    {
        if (pickupType == PickupType.Item)
        {
            ItemInstance item = GetItem();

            if (item != null)
                return GetRarityColor(item.rarity);

            return Color.white;
        }

        if (pickupType == PickupType.Currency)
        {
            CurrencySO currency = GetCurrency();

            if (currency != null)
                return currency.LabelColor;

            return Color.white;
        }

        return Color.white;
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

        if (IsClaimed)
            return;

        if (ownerNetId != 0 && sender.identity.netId != ownerNetId)
            return;

        float distance = Vector3.Distance(
            sender.identity.transform.position,
            transform.position
        );

        if (distance > pickupRadius)
            return;

        switch (pickupType)
        {
            case PickupType.Item:
                TryPickupItem(sender);
                break;

            case PickupType.Currency:
                TryPickupCurrency(sender);
                break;
        }
    }

    [Server]
    private void TryPickupItem(NetworkConnectionToClient sender)
    {
        if (string.IsNullOrWhiteSpace(itemJson))
            return;

        ItemInstance item = JsonUtility.FromJson<ItemInstance>(itemJson);

        if (item == null || item.instanceId == 0)
            return;

        item.EnsureLists();

        PlayerInventory inventory = sender.identity.GetComponent<PlayerInventory>();

        if (inventory == null)
            return;

        bool added = inventory.Server_AddItem(item);

        if (!added)
            return;

        IsClaimed = true;
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void TryPickupCurrency(NetworkConnectionToClient sender)
    {
        if (currencyId == 0 || amount <= 0)
            return;

        PlayerCurrencyInventory inventory =
            sender.identity.GetComponent<PlayerCurrencyInventory>();

        if (inventory == null)
            return;

        bool added = inventory.AddCurrency(currencyId, amount);

        if (!added)
            return;

        IsClaimed = true;
        NetworkServer.Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pickupType == PickupType.Item)
        {
            if (ItemPreviewManager.Instance != null)
                ItemPreviewManager.Instance.InitPreview(GetItem());

            return;
        }

        if (pickupType == PickupType.Currency)
        {
            CurrencySO currency = GetCurrency();

            if (currency != null)
                Debug.Log($"[LootPickup] {currency.CurrencyName} x{amount}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (pickupType == PickupType.Item)
        {
            if (ItemPreviewManager.Instance != null)
                ItemPreviewManager.Instance.ClosePreview();
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Normal:
                return new Color(0.5f, 0.5f, 0.5f);

            case ItemRarity.Magic:
                return new Color(0.3f, 0.5f, 1f);

            case ItemRarity.Rare:
                return new Color(1f, 0.85f, 0.2f);

            case ItemRarity.Unique:
                return new Color(1f, 0.5f, 0.1f);

            default:
                return Color.white;
        }
    }
}