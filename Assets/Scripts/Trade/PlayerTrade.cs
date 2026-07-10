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

    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log(
            $"[Client][PlayerTrade] OnStartClient | " +
            $"name={name} | netId={netId} | isLocalPlayer={isLocalPlayer}"
        );
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log(
            $"[Server][PlayerTrade] OnStartServer | " +
            $"name={name} | netId={netId}"
        );
    }

    public override void OnStopServer()
    {
        Debug.Log($"[Server][PlayerTrade] OnStopServer | name={name} | netId={netId}");

        if (TradeManager.Instance != null)
            TradeManager.Instance.CancelTradeFor(this, "Player disconnected.");

        base.OnStopServer();
    }

    [Command]
    public void CmdRequestTrade(uint targetNetId)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdRequestTrade received | " +
            $"requester={name} | requesterNetId={netId} | targetNetId={targetNetId}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] CmdRequestTrade failed: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.RequestTrade(this, targetNetId);
    }

    [Command]
    public void CmdAcceptTradeInvite(uint requesterNetId)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdAcceptTradeInvite received | " +
            $"target={name} | targetNetId={netId} | requesterNetId={requesterNetId}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot accept trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.AcceptTradeInvite(this, requesterNetId);
    }

    [Command]
    public void CmdDeclineTradeInvite(uint requesterNetId)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdDeclineTradeInvite received | " +
            $"target={name} | targetNetId={netId} | requesterNetId={requesterNetId}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot decline trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.DeclineTradeInvite(this, requesterNetId);
    }

    [Command]
    public void CmdAddInventoryItemToTrade(int inventorySlotIndex, int amount, int knownRevision)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdAddInventoryItemToTrade | " +
            $"player={name} | inventorySlotIndex={inventorySlotIndex} | amount={amount} | knownRevision={knownRevision}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot add item: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.AddInventoryItemToTrade(
            this,
            inventorySlotIndex,
            amount,
            knownRevision
        );
    }

    [Command]
    public void CmdAddInventoryItemToTradeSlot(
        int inventorySlotIndex,
        int amount,
        int offerSlotIndex,
        int knownRevision)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdAddInventoryItemToTradeSlot | " +
            $"player={name} | inventorySlotIndex={inventorySlotIndex} | " +
            $"amount={amount} | offerSlotIndex={offerSlotIndex} | knownRevision={knownRevision}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot add item to trade slot: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.AddInventoryItemToTradeSlot(
            this,
            inventorySlotIndex,
            amount,
            offerSlotIndex,
            knownRevision
        );
    }

    [Command]
    public void CmdMoveOfferSlot(
        int fromOfferSlotIndex,
        int toOfferSlotIndex,
        int knownRevision)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdMoveOfferSlot | " +
            $"player={name} | from={fromOfferSlotIndex} | to={toOfferSlotIndex} | knownRevision={knownRevision}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot move offer slot: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.MoveOfferSlot(
            this,
            fromOfferSlotIndex,
            toOfferSlotIndex,
            knownRevision
        );
    }

    [Command]
    public void CmdRemoveOfferSlot(int offerSlotIndex, int knownRevision)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdRemoveOfferSlot | " +
            $"player={name} | offerSlotIndex={offerSlotIndex} | knownRevision={knownRevision}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot remove offer slot: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.RemoveOfferSlot(
            this,
            offerSlotIndex,
            knownRevision
        );
    }

    [Command]
    public void CmdSetReady(bool ready, int knownRevision)
    {
        Debug.Log(
            $"[Server][PlayerTrade] CmdSetReady | " +
            $"player={name} | ready={ready} | knownRevision={knownRevision}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot set ready: TradeManager.Instance is null.");
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
        Debug.Log(
            $"[Server][PlayerTrade] CmdFinalAccept | " +
            $"player={name} | knownRevision={knownRevision} | knownOfferHash={knownOfferHash}"
        );

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot final accept: TradeManager.Instance is null.");
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
        Debug.Log($"[Server][PlayerTrade] CmdCancelTrade | player={name}");

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[Server][PlayerTrade] Cannot cancel trade: TradeManager.Instance is null.");
            return;
        }

        TradeManager.Instance.CancelTradeFor(this, "Trade cancelled.");
    }

    [TargetRpc]
    public void TargetReceiveTradeInvite(NetworkConnectionToClient target, string inviteJson)
    {
        Debug.Log(
            $"[Client][PlayerTrade] TargetReceiveTradeInvite received | " +
            $"localObject={name} | json={inviteJson}"
        );

        TradeInviteDto dto = JsonUtility.FromJson<TradeInviteDto>(inviteJson);

        if (dto == null)
        {
            Debug.LogWarning("[Client][PlayerTrade] Received invalid trade invite dto.");
            return;
        }

        if (ClientTradeInviteReceived == null)
            Debug.LogWarning("[Client][PlayerTrade] No listener subscribed to ClientTradeInviteReceived.");

        ClientTradeInviteReceived?.Invoke(dto);
    }

    [TargetRpc]
    public void TargetReceiveTradeState(NetworkConnectionToClient target, string stateJson)
    {
        Debug.Log(
            $"[Client][PlayerTrade] TargetReceiveTradeState received | " +
            $"localObject={name} | jsonLength={stateJson?.Length ?? 0}"
        );

        TradeStateDto dto = JsonUtility.FromJson<TradeStateDto>(stateJson);

        if (dto == null)
        {
            Debug.LogWarning("[Client][PlayerTrade] Received invalid trade state dto.");
            return;
        }

        if (ClientTradeUpdated == null)
            Debug.LogWarning("[Client][PlayerTrade] No listener subscribed to ClientTradeUpdated.");

        ClientTradeUpdated?.Invoke(dto);
    }

    [TargetRpc]
    public void TargetTradeClosed(NetworkConnectionToClient target, string reason)
    {
        Debug.Log($"[Client][PlayerTrade] TargetTradeClosed received | reason={reason}");

        ClientTradeClosed?.Invoke(reason);
    }

    [TargetRpc]
    public void TargetTradeError(NetworkConnectionToClient target, string message)
    {
        Debug.LogWarning($"[Client][PlayerTrade] TargetTradeError / Message received | message={message}");

        ClientTradeError?.Invoke(message);
    }
}