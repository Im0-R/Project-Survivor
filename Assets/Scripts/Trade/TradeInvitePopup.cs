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

    [Header("Debug")]
    [SerializeField] private bool enableLogs = true;

    private uint currentRequesterNetId;

    private void Awake()
    {
        Log("Awake.");

        if (root != null)
        {
            root.SetActive(false);
            Log($"Root assigned and hidden. root={root.name}");
        }
        else
        {
            LogWarning("Root is null. Popup cannot be displayed.");
        }

        if (messageText == null)
            LogWarning("MessageText is null.");

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveListener(Accept);
            acceptButton.onClick.AddListener(Accept);
            Log("AcceptButton listener registered.");
        }
        else
        {
            LogWarning("AcceptButton is null.");
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveListener(Decline);
            declineButton.onClick.AddListener(Decline);
            Log("DeclineButton listener registered.");
        }
        else
        {
            LogWarning("DeclineButton is null.");
        }
    }

    private void OnEnable()
    {
        PlayerTrade.ClientTradeInviteReceived += OnTradeInviteReceived;
        PlayerTrade.ClientTradeClosed += OnTradeClosed;

        Log("OnEnable subscribed to PlayerTrade events.");
    }

    private void OnDisable()
    {
        PlayerTrade.ClientTradeInviteReceived -= OnTradeInviteReceived;
        PlayerTrade.ClientTradeClosed -= OnTradeClosed;

        Log("OnDisable unsubscribed from PlayerTrade events.");
    }

    private void OnTradeInviteReceived(TradeInviteDto invite)
    {
        Log(
            $"OnTradeInviteReceived called | " +
            $"inviteNull={invite == null} | " +
            $"rootNull={root == null}"
        );

        if (invite == null)
            return;

        currentRequesterNetId = invite.requesterNetId;

        Log(
            $"Invite received | requesterName={invite.requesterName} | " +
            $"requesterNetId={invite.requesterNetId}"
        );

        if (messageText != null)
            messageText.text = $"{invite.requesterName} wants to trade with you.";
        else
            LogWarning("Cannot write message: messageText is null.");

        if (root != null)
        {
            root.SetActive(true);
            Log($"Popup root shown. activeSelf={root.activeSelf} | activeInHierarchy={root.activeInHierarchy}");
        }
        else
        {
            LogWarning("Cannot show popup: root is null.");
        }
    }

    private void OnTradeClosed(string reason)
    {
        Log($"OnTradeClosed received | reason={reason}");
        Hide();
    }

    private void Accept()
    {
        Log($"Accept clicked | currentRequesterNetId={currentRequesterNetId}");

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
        {
            LogWarning("Accept failed: local PlayerTrade is null.");
            return;
        }

        if (currentRequesterNetId == 0)
        {
            LogWarning("Accept failed: currentRequesterNetId is 0.");
            return;
        }

        localTrade.CmdAcceptTradeInvite(currentRequesterNetId);

        Hide();
    }

    private void Decline()
    {
        Log($"Decline clicked | currentRequesterNetId={currentRequesterNetId}");

        PlayerTrade localTrade = GetLocalTrade();

        if (localTrade == null)
        {
            LogWarning("Decline failed: local PlayerTrade is null.");
            return;
        }

        if (currentRequesterNetId == 0)
        {
            LogWarning("Decline failed: currentRequesterNetId is 0.");
            return;
        }

        localTrade.CmdDeclineTradeInvite(currentRequesterNetId);

        Hide();
    }

    private void Hide()
    {
        Log("Hide popup.");

        currentRequesterNetId = 0;

        if (root != null)
            root.SetActive(false);
    }

    private PlayerTrade GetLocalTrade()
    {
        if (NetworkClient.localPlayer == null)
        {
            LogWarning("GetLocalTrade failed: NetworkClient.localPlayer is null.");
            return null;
        }

        PlayerTrade trade = NetworkClient.localPlayer.GetComponent<PlayerTrade>();

        if (trade == null)
            LogWarning("GetLocalTrade failed: local player has no PlayerTrade.");

        return trade;
    }

    private void Log(string message)
    {
        if (!enableLogs)
            return;

        Debug.Log($"[Client][TradeInvitePopup] {message}");
    }

    private void LogWarning(string message)
    {
        if (!enableLogs)
            return;

        Debug.LogWarning($"[Client][TradeInvitePopup] {message}");
    }
}