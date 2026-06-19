using System;
using Mirror;
using UnityEngine;

public class PlayerTrade : NetworkBehaviour
{
    public static event Action<TradeInviteDto> ClientTradeInviteReceived;
    public static event Action<TradeStateDto> ClientTradeUpdated;
    public static event Action<string> ClientTradeClosed;
    public static event Action<string> ClientTradeError;

    public string TradeDisplayName
    {
        get
        {
            string playerName = gameObject.name;
            playerName = playerName.Replace("(Clone)", "");
            return playerName;
        }
    }

    public override void OnStopServer()
    {
        if (TradeManager.Instance != null)
            TradeManager.Instance.CancelTradeFor(this, "Player disconnected.");

        base.OnStopServer();
    }

    // =========================================================
    // Client -> Server
    // =========================================================

    [Command]
    public void CmdRequestTrade(uint targetNetId)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot request trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.RequestTrade(this, targetNetId);
    }

    [Command]
    public void CmdAcceptTradeInvite(uint requesterNetId)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot accept trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.AcceptTradeInvite(this, requesterNetId);
    }

    [Command]
    public void CmdDeclineTradeInvite(uint requesterNetId)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot decline trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.DeclineTradeInvite(this, requesterNetId);
    }

    [Command]
    public void CmdAddInventoryItemToTrade(int slotIndex, int amount, int knownRevision)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot add item: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.AddInventoryItemToTrade(
            this,
            slotIndex,
            amount,
            knownRevision
        );
    }

    [Command]
    public void CmdRemoveOfferSlot(int offerIndex, int knownRevision)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot remove offer slot: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.RemoveOfferSlot(
            this,
            offerIndex,
            knownRevision
        );
    }

    [Command]
    public void CmdSetReady(bool ready, int knownRevision)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot set ready: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.SetReady(
            this,
            ready,
            knownRevision
        );
    }

    [Command]
    public void CmdFinalAccept(int knownRevision, string knownOfferHash)
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot final accept: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.FinalAccept(
            this,
            knownRevision,
            knownOfferHash
        );
    }

    [Command]
    public void CmdCancelTrade()
    {
        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[PlayerTrade] Cannot cancel trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.CancelTradeFor(this, "Trade cancelled.");
    }

    // =========================================================
    // Server -> Client
    // =========================================================

    [TargetRpc]
    public void TargetReceiveTradeInvite(NetworkConnectionToClient target, string inviteJson)
    {
        TradeInviteDto dto = JsonUtility.FromJson<TradeInviteDto>(inviteJson);

        if (dto == null)
        {
            Debug.LogWarning("[PlayerTrade] Received invalid trade invite dto.");
            return;
        }

        ClientTradeInviteReceived?.Invoke(dto);
    }

    [TargetRpc]
    public void TargetReceiveTradeState(NetworkConnectionToClient target, string stateJson)
    {
        TradeStateDto dto = JsonUtility.FromJson<TradeStateDto>(stateJson);

        if (dto == null)
        {
            Debug.LogWarning("[PlayerTrade] Received invalid trade state dto.");
            return;
        }

        ClientTradeUpdated?.Invoke(dto);
    }

    [TargetRpc]
    public void TargetTradeClosed(NetworkConnectionToClient target, string reason)
    {
        ClientTradeClosed?.Invoke(reason);
    }

    [TargetRpc]
    public void TargetTradeError(NetworkConnectionToClient target, string message)
    {
        ClientTradeError?.Invoke(message);
    }
}