using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTrade : MonoBehaviour
{
    public static CanvasTrade Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text revisionText;

    [Header("Offers")]
    [SerializeField] private Transform selfOfferParent;
    [SerializeField] private Transform otherOfferParent;
    [SerializeField] private TradeOfferLineUI offerLinePrefab;

    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button finalAcceptButton;
    [SerializeField] private Button cancelButton;

    private TradeStateDto currentState;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        if (finalAcceptButton != null)
            finalAcceptButton.onClick.AddListener(OnFinalAcceptClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnEnable()
    {
        PlayerTrade.ClientTradeUpdated += OnTradeUpdated;
        PlayerTrade.ClientTradeClosed += OnTradeClosed;
        PlayerTrade.ClientTradeError += OnTradeError;
    }

    private void OnDisable()
    {
        PlayerTrade.ClientTradeUpdated -= OnTradeUpdated;
        PlayerTrade.ClientTradeClosed -= OnTradeClosed;
        PlayerTrade.ClientTradeError -= OnTradeError;
    }

    public bool IsOpen()
    {
        return root != null && root.activeSelf && currentState != null;
    }

    public void RequestAddInventorySlot(int slotIndex, int amount = 1)
    {
        if (currentState == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdAddInventoryItemToTrade(
            slotIndex,
            amount,
            currentState.revision
        );
    }

    private void OnTradeUpdated(TradeStateDto state)
    {
        currentState = state;

        if (root != null)
            root.SetActive(true);

        Refresh();
    }

    private void OnTradeClosed(string reason)
    {
        currentState = null;

        ClearOffers();

        if (statusText != null)
            statusText.text = reason;

        if (root != null)
            root.SetActive(false);
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

        if (titleText != null)
            titleText.text = $"Trade with {currentState.otherName}";

        if (statusText != null)
            statusText.text = BuildStatusText();

        if (revisionText != null)
            revisionText.text = $"Revision {currentState.revision}";

        RefreshOfferList(selfOfferParent, currentState.selfOffers, true);
        RefreshOfferList(otherOfferParent, currentState.otherOffers, false);

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

    private void RefreshOfferList(
        Transform parent,
        System.Collections.Generic.List<TradeOfferView> offers,
        bool isSelf
    )
    {
        if (parent == null || offerLinePrefab == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        if (offers == null)
            return;

        for (int i = 0; i < offers.Count; i++)
        {
            TradeOfferView view = offers[i];

            TradeOfferLineUI line = Instantiate(offerLinePrefab, parent);

            line.Init(view, isSelf, () =>
            {
                PlayerTrade localTrade = GetLocalTrade();

                if (localTrade == null || currentState == null)
                    return;

                localTrade.CmdRemoveOfferSlot(view.offerIndex, currentState.revision);
            });
        }
    }

    private void ClearOffers()
    {
        ClearParent(selfOfferParent);
        ClearParent(otherOfferParent);
    }

    private void ClearParent(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
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