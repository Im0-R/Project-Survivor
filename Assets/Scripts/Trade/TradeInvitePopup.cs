using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeInvitePopup : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private uint currentRequesterNetId;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(Accept);

        if (declineButton != null)
            declineButton.onClick.AddListener(Decline);
    }

    private void OnEnable()
    {
        PlayerTrade.ClientTradeInviteReceived += OnTradeInviteReceived;
        PlayerTrade.ClientTradeClosed += OnTradeClosed;
    }

    private void OnDisable()
    {
        PlayerTrade.ClientTradeInviteReceived -= OnTradeInviteReceived;
        PlayerTrade.ClientTradeClosed -= OnTradeClosed;
    }

    private void OnTradeInviteReceived(TradeInviteDto invite)
    {
        if (invite == null)
            return;

        currentRequesterNetId = invite.requesterNetId;

        if (messageText != null)
            messageText.text = $"{invite.requesterName} wants to trade with you.";

        if (root != null)
            root.SetActive(true);
    }

    private void OnTradeClosed(string reason)
    {
        Hide();
    }

    private void Accept()
    {
        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdAcceptTradeInvite(currentRequesterNetId);
        Hide();
    }

    private void Decline()
    {
        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdDeclineTradeInvite(currentRequesterNetId);
        Hide();
    }

    private void Hide()
    {
        currentRequesterNetId = 0;

        if (root != null)
            root.SetActive(false);
    }

    private PlayerTrade GetLocalTrade()
    {
        if (NetworkClient.localPlayer == null)
            return null;

        return NetworkClient.localPlayer.GetComponent<PlayerTrade>();
    }
}