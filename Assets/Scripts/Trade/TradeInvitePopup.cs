using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeInvitePopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private TradeInviteDto currentInvite;

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
        PlayerTrade.ClientTradeInviteReceived += OnInviteReceived;
    }

    private void OnDisable()
    {
        PlayerTrade.ClientTradeInviteReceived -= OnInviteReceived;
    }

    private void OnInviteReceived(TradeInviteDto invite)
    {
        currentInvite = invite;

        if (messageText != null)
            messageText.text = $"{invite.requesterName} wants to trade with you.";

        if (root != null)
            root.SetActive(true);
    }

    private void Accept()
    {
        if (currentInvite == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
            return;

        localTrade.CmdAcceptTradeInvite(currentInvite.requesterNetId);

        Close();
    }

    private void Decline()
    {
        if (currentInvite == null)
            return;

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade != null)
            localTrade.CmdDeclineTradeInvite(currentInvite.requesterNetId);

        Close();
    }

    private void Close()
    {
        currentInvite = null;

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