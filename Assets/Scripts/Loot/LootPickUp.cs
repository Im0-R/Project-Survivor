using Mirror;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class LootPickup : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SyncVar(hook = nameof(OnPayloadJsonChanged))]
    private string payloadJson = "";

    [SyncVar] private uint ownerNetId;
    [SyncVar] public bool IsClaimed = false;

    [SerializeField] private float pickupRadius = 2f;

    private LootPayload cachedPayload;

    public event Action OnVisualChanged;

    public override void OnStartClient()
    {
        base.OnStartClient();

        RebuildCachedPayload();

        if (LootUIManager.Instance != null)
            LootUIManager.Instance.RegisterLoot(this);

        OnVisualChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        if (LootUIManager.Instance != null)
            LootUIManager.Instance.UnregisterLoot(this);

        base.OnStopClient();
    }

    [Server]
    public void Init(LootPayload payload)
    {
        if (payload == null)
        {
            Debug.LogError("[LootPickup] Init failed: payload is null.");
            return;
        }

        payload.amount = Mathf.Max(1, payload.amount);

        cachedPayload = payload;
        payloadJson = JsonUtility.ToJson(payload);
    }

    private void OnPayloadJsonChanged(string oldValue, string newValue)
    {
        RebuildCachedPayload();
        OnVisualChanged?.Invoke();
    }

    private void RebuildCachedPayload()
    {
        cachedPayload = DeserializePayload(payloadJson);
    }

    public LootPayload GetPayload()
    {
        if (cachedPayload == null && !string.IsNullOrWhiteSpace(payloadJson))
            RebuildCachedPayload();

        return cachedPayload;
    }

    public LootableSO GetLootable()
    {
        LootPayload payload = GetPayload();

        if (payload == null || payload.lootableId == 0)
            return null;

        return LootableDatabase.Get(payload.lootableId);
    }

    public ItemInstance GetItem()
    {
        LootPayload payload = GetPayload();

        if (payload == null || !payload.IsGeneratedItem())
            return null;

        ItemInstance item = JsonUtility.FromJson<ItemInstance>(payload.itemJson);
        item?.EnsureLists();

        return item;
    }

    public int GetAmount()
    {
        LootPayload payload = GetPayload();
        return payload != null ? payload.amount : 0;
    }

    public string GetDisplayName()
    {
        LootPayload payload = GetPayload();

        if (payload == null)
            return "Unknown Loot";

        string baseName = "";

        if (!string.IsNullOrWhiteSpace(payload.displayNameOverride))
        {
            baseName = payload.displayNameOverride;
        }
        else
        {
            LootableSO lootable = GetLootable();

            if (lootable == null)
                return $"Lootable {payload.lootableId}";

            baseName = lootable.DisplayName;
        }

        if (payload.amount > 1 && !payload.IsGeneratedItem())
            return $"{baseName} x{payload.amount}";

        return baseName;
    }

    public Sprite GetIcon()
    {
        LootableSO lootable = GetLootable();
        return lootable != null ? lootable.Icon : null;
    }

    public Color GetLabelColor()
    {
        LootPayload payload = GetPayload();

        if (payload != null && payload.hasRarityColor)
            return GetRarityColor(payload.rarity);

        LootableSO lootable = GetLootable();

        if (lootable != null)
            return lootable.LabelColor;

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

        LootPayload payload = GetPayload();

        if (payload == null || payload.lootableId == 0 || payload.amount <= 0)
            return;

        float distance = Vector3.Distance(
            sender.identity.transform.position,
            transform.position
        );

        if (distance > pickupRadius)
            return;

        PlayerInventory inventory = sender.identity.GetComponent<PlayerInventory>();

        if (inventory == null)
            inventory = sender.identity.GetComponentInChildren<PlayerInventory>(true);

        if (inventory == null)
        {
            Debug.LogError("[LootPickup] PlayerInventory missing.");
            return;
        }

        bool added = inventory.Server_AddLoot(payload);

        if (!added)
            return;

        IsClaimed = true;
        NetworkServer.Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemInstance item = GetItem();

        if (item != null)
        {
            if (ItemPreviewManager.Instance != null)
                ItemPreviewManager.Instance.InitPreview(item);

            return;
        }

        Debug.Log($"[LootPickup] {GetDisplayName()}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GetItem() != null)
        {
            if (ItemPreviewManager.Instance != null)
                ItemPreviewManager.Instance.ClosePreview();
        }
    }

    private LootPayload DeserializePayload(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            LootPayload payload = JsonUtility.FromJson<LootPayload>(json);

            if (payload != null)
                payload.amount = Mathf.Max(1, payload.amount);

            return payload;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LootPickup] Payload deserialize failed: {e}");
            return null;
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