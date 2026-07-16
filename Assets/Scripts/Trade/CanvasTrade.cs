using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTrade : UIWindow
{
    public static CanvasTrade Instance { get; private set; }

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text revisionText;

    [Header("Trade Slots")]
    [SerializeField] private BackGroundSlot[] selfTradeSlots;
    [SerializeField] private BackGroundSlot[] otherTradeSlots;

    [Header("Cards")]
    [SerializeField] private GameObject itemCardPrefab;

    [Header("Drag")]
    [SerializeField] private Transform dragRoot;

    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button finalAcceptButton;
    [SerializeField] private Button cancelButton;

    [Header("Options")]
    [SerializeField] private bool openInventoryOnTrade = true;

    private TradeStateDto currentState;

    public Transform DragRoot => dragRoot != null ? dragRoot : transform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetupSlots();

        PlayerTrade.ClientTradeUpdated += OnTradeUpdated;
        PlayerTrade.ClientTradeClosed += OnTradeClosed;
        PlayerTrade.ClientTradeError += OnTradeError;

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        if (finalAcceptButton != null)
            finalAcceptButton.onClick.AddListener(OnFinalAcceptClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnDestroy()
    {
        PlayerTrade.ClientTradeUpdated -= OnTradeUpdated;
        PlayerTrade.ClientTradeClosed -= OnTradeClosed;
        PlayerTrade.ClientTradeError -= OnTradeError;

        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyClicked);

        if (finalAcceptButton != null)
            finalAcceptButton.onClick.RemoveListener(OnFinalAcceptClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClicked);

        if (Instance == this)
            Instance = null;
    }

    protected override void OnBeforeClosed()
    {
        // Escape, loading, reward UI and CloseAllWindows all pass here.
        // The server remains authoritative and will confirm the closure
        // through OnTradeClosed.
        if (currentState == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade != null)
            localTrade.CmdCancelTrade();
    }

    private void SetupSlots()
    {
        if (selfTradeSlots != null)
        {
            for (int i = 0; i < selfTradeSlots.Length; i++)
            {
                if (selfTradeSlots[i] == null)
                    continue;

                selfTradeSlots[i].SetId(i);
                selfTradeSlots[i].SetContext(BackGroundSlotContext.TradeSelf);
            }
        }

        if (otherTradeSlots != null)
        {
            for (int i = 0; i < otherTradeSlots.Length; i++)
            {
                if (otherTradeSlots[i] == null)
                    continue;

                otherTradeSlots[i].SetId(i);
                otherTradeSlots[i].SetContext(BackGroundSlotContext.TradeOther);
            }
        }
    }

    public bool HasActiveTrade => IsOpen && currentState != null;

    public void RequestDrop(ItemCard card, BackGroundSlot targetSlot)
    {
        if (card == null || targetSlot == null)
            return;

        if (currentState == null)
            return;

        if (!targetSlot.IsSelfTradeSlot)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        int targetOfferSlotIndex = targetSlot.Id;

        if (card.Source == ItemCardSource.Inventory)
        {
            localTrade.CmdAddInventoryItemToTradeSlot(
                card.SlotIndex,
                1,
                targetOfferSlotIndex,
                currentState.revision
            );

            return;
        }

        if (card.Source == ItemCardSource.TradeSelf)
        {
            localTrade.CmdMoveOfferSlot(
                card.SlotIndex,
                targetOfferSlotIndex,
                currentState.revision
            );

            return;
        }
    }

    public void RequestAddInventorySlot(int inventorySlotIndex, int amount = 1)
    {
        if (currentState == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdAddInventoryItemToTrade(
            inventorySlotIndex,
            amount,
            currentState.revision
        );
    }

    public void RequestRemoveSelfOfferSlot(int offerSlotIndex)
    {
        if (currentState == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdRemoveOfferSlot(
            offerSlotIndex,
            currentState.revision
        );
    }

    private void OnTradeUpdated(TradeStateDto state)
    {
        currentState = state;

        if (UIManager.Instance != null)
        {
            if (openInventoryOnTrade)
            {
                UIManager.Instance.OpenWindow(
                    UIWindowId.Inventory
                );
            }

            UIManager.Instance.OpenWindow(
                UIWindowId.Trade
            );
        }

        Refresh();
    }

    private void OnTradeClosed(string reason)
    {
        currentState = null;

        ClearAllTradeCards();

        if (statusText != null)
            statusText.text = reason;

        UIManager.Instance?.CloseWindow(
            UIWindowId.Trade
        );
    }

    private void OnTradeError(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void Refresh()
    {
        if (currentState == null)
            return;

        SetupSlots();

        if (titleText != null)
            titleText.text = $"Trade with {currentState.otherName}";

        if (statusText != null)
            statusText.text = BuildStatusText();

        if (revisionText != null)
            revisionText.text = $"Revision {currentState.revision}";

        ClearAllTradeCards();

        PopulateOffer(
            currentState.selfOffers,
            selfTradeSlots,
            ItemCardSource.TradeSelf
        );

        PopulateOffer(
            currentState.otherOffers,
            otherTradeSlots,
            ItemCardSource.TradeOther
        );

        RefreshButtons();
    }

    private void PopulateOffer(
        List<TradeOfferView> offers,
        BackGroundSlot[] slots,
        ItemCardSource source)
    {
        if (offers == null || slots == null || itemCardPrefab == null)
            return;

        for (int i = 0; i < offers.Count; i++)
        {
            TradeOfferView offer = offers[i];

            if (offer.offerSlotIndex < 0 || offer.offerSlotIndex >= slots.Length)
                continue;

            BackGroundSlot slot = slots[offer.offerSlotIndex];

            if (slot == null)
                continue;

            InventoryItemData data = CreateInventoryDataFromTradeView(offer);

            if (data == null)
                continue;

            GameObject cardObject = Instantiate(itemCardPrefab, slot.transform);

            ItemCard card = cardObject.GetComponent<ItemCard>();

            if (card == null)
            {
                Destroy(cardObject);
                continue;
            }

            card.SetInventoryItemData(data);
            card.SetSlotIndex(offer.offerSlotIndex);
            card.SetSource(source);

            ResetCardTransform(cardObject);
        }
    }

    private InventoryItemData CreateInventoryDataFromTradeView(TradeOfferView view)
    {
        if (view == null || view.lootableId == 0 || view.amount <= 0)
            return null;

        InventoryItemData data = new InventoryItemData
        {
            lootableId = view.lootableId,
            amount = view.amount,
            itemJson = view.itemJson ?? "",
            displayNameOverride = view.displayName ?? "",
            hasRarityColor = view.hasRarityColor,
            rarity = view.rarity
        };

        if (!string.IsNullOrWhiteSpace(view.itemJson))
        {
            data.lootableType = LootableType.GeneratedItem;
            return data;
        }

        LootableSO lootable = LootableDatabase.Get(view.lootableId);

        if (lootable is CurrencySO currency)
        {
            if (currency.type == CurrencyType.Sigil)
                data.lootableType = LootableType.Sigil;
            else
                data.lootableType = LootableType.Currency;
        }
        else
        {
            data.lootableType = LootableType.Unknown;
        }

        return data;
    }

    private void ResetCardTransform(GameObject cardObject)
    {
        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void ClearAllTradeCards()
    {
        ClearCardsFromSlots(selfTradeSlots);
        ClearCardsFromSlots(otherTradeSlots);
    }

    private void ClearCardsFromSlots(BackGroundSlot[] slots)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            ItemCard[] cards = slots[i].GetComponentsInChildren<ItemCard>(true);

            for (int j = 0; j < cards.Length; j++)
                Destroy(cards[j].gameObject);
        }
    }

    private void RefreshButtons()
    {
        if (currentState == null)
            return;

        if (readyButton != null)
        {
            readyButton.interactable = !currentState.selfReady;

            TMP_Text text = readyButton.GetComponentInChildren<TMP_Text>();

            if (text != null)
                text.text = currentState.selfReady ? "Ready" : "Validate Offer";
        }

        if (finalAcceptButton != null)
        {
            finalAcceptButton.interactable =
                currentState.selfReady &&
                currentState.otherReady &&
                !currentState.selfFinalAccepted;

            TMP_Text text = finalAcceptButton.GetComponentInChildren<TMP_Text>();

            if (text != null)
                text.text = currentState.selfFinalAccepted ? "Accepted" : "Final Accept";
        }
    }

    private string BuildStatusText()
    {
        if (currentState == null)
            return "";

        if (!string.IsNullOrWhiteSpace(currentState.message))
            return currentState.message;

        if (!currentState.selfReady || !currentState.otherReady)
            return "Both players must validate their offers.";

        if (!currentState.selfFinalAccepted || !currentState.otherFinalAccepted)
            return "Both players must final accept.";

        return "Completing trade...";
    }

    private void OnReadyClicked()
    {
        if (currentState == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdSetReady(true, currentState.revision);
    }

    private void OnFinalAcceptClicked()
    {
        if (currentState == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdFinalAccept(
            currentState.revision,
            currentState.offerHash
        );
    }

    private void OnCancelClicked()
    {
        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdCancelTrade();
    }

    private PlayerTrade GetLocalTrade()
    {
        if (NetworkClient.localPlayer == null)
            return null;

        return NetworkClient.localPlayer.GetComponent<PlayerTrade>();
    }
}
