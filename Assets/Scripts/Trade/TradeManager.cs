using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class TradeManager : NetworkBehaviour
{
    public static TradeManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private float maxTradeDistance = 5f;
    [SerializeField] private float inviteDuration = 15f;
    [SerializeField] private int maxOfferSlots = 12;

    private readonly Dictionary<string, TradeSession> sessionsById = new Dictionary<string, TradeSession>();
    private readonly Dictionary<uint, string> sessionByPlayerNetId = new Dictionary<uint, string>();
    private readonly List<TradeInvite> pendingInvites = new List<TradeInvite>();

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void RequestTrade(PlayerTrade requester, uint targetNetId)
    {
        CleanupExpiredInvites();

        if (requester == null)
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            SendError(requester, "Target player not found.");
            return;
        }

        PlayerTrade target = targetIdentity.GetComponent<PlayerTrade>();

        if (!CanStartTrade(requester, target, out string reason))
        {
            SendError(requester, reason);
            return;
        }

        pendingInvites.RemoveAll(i =>
            i.requester == requester ||
            i.target == requester ||
            i.requester == target ||
            i.target == target
        );

        TradeInvite invite = new TradeInvite
        {
            requester = requester,
            target = target,
            expiresAt = Time.time + inviteDuration
        };

        pendingInvites.Add(invite);

        TradeInviteDto dto = new TradeInviteDto
        {
            requesterNetId = requester.netId,
            requesterName = requester.TradeDisplayName
        };

        target.TargetReceiveTradeInvite(
            target.connectionToClient,
            JsonUtility.ToJson(dto)
        );

        SendError(requester, $"Trade invite sent to {target.TradeDisplayName}.");
    }

    [Server]
    public void AcceptTradeInvite(PlayerTrade target, uint requesterNetId)
    {
        CleanupExpiredInvites();

        TradeInvite invite = pendingInvites.FirstOrDefault(i =>
            i.target == target &&
            i.requester != null &&
            i.requester.netId == requesterNetId
        );

        if (invite == null)
        {
            SendError(target, "Trade invite expired.");
            return;
        }

        pendingInvites.Remove(invite);

        if (!CanStartTrade(invite.requester, invite.target, out string reason))
        {
            SendError(invite.requester, reason);
            SendError(invite.target, reason);
            return;
        }

        CreateSession(invite.requester, invite.target);
    }

    [Server]
    public void DeclineTradeInvite(PlayerTrade target, uint requesterNetId)
    {
        CleanupExpiredInvites();

        TradeInvite invite = pendingInvites.FirstOrDefault(i =>
            i.target == target &&
            i.requester != null &&
            i.requester.netId == requesterNetId
        );

        if (invite == null)
            return;

        pendingInvites.Remove(invite);

        SendError(invite.requester, $"{target.TradeDisplayName} declined your trade invite.");
        SendError(target, "Trade invite declined.");
    }

    [Server]
    public void AddInventoryItemToTrade(
        PlayerTrade player,
        int slotIndex,
        int amount,
        int knownRevision
    )
    {
        if (!TryGetSession(player, out TradeSession session))
        {
            SendError(player, "You are not in a trade.");
            return;
        }

        if (knownRevision != session.revision)
        {
            SendError(player, "Trade changed. Refresh required.");
            SendUpdate(session, "Trade changed.");
            return;
        }

        ITradeInventory inventory = GetTradeInventory(player);

        if (inventory == null)
        {
            SendError(player, "No trade inventory found.");
            return;
        }

        if (slotIndex < 0 || slotIndex >= inventory.TradeSlotCount)
        {
            SendError(player, "Invalid inventory slot.");
            return;
        }

        List<TradeOfferEntry> ownOffers = GetOffers(session, player);

        if (ownOffers.Count >= maxOfferSlots)
        {
            SendError(player, "Trade offer is full.");
            return;
        }

        if (ownOffers.Any(o => o.sourceSlotIndex == slotIndex))
        {
            SendError(player, "This slot is already in your trade offer.");
            return;
        }

        if (!inventory.TryGetTradePayloadServer(slotIndex, out LootPayload payload))
        {
            SendError(player, "No item in this slot.");
            return;
        }

        if (!TradeUtility.IsPayloadTradeable(payload, out string reason))
        {
            SendError(player, reason);
            return;
        }

        amount = Mathf.Clamp(amount, 1, payload.amount);

        string lockId = BuildLockId(session, player, slotIndex);

        if (!inventory.TryLockTradeSlotServer(slotIndex, amount, lockId))
        {
            SendError(player, "This item is already locked.");
            return;
        }

        LootPayload offeredPayload = payload.CloneWithAmount(amount);

        TradeOfferEntry entry = new TradeOfferEntry
        {
            ownerNetId = player.netId,
            sourceSlotIndex = slotIndex,
            amount = amount,
            lockId = lockId,
            payload = offeredPayload,
            payloadHash = TradeUtility.BuildPayloadHash(offeredPayload)
        };

        ownOffers.Add(entry);

        MarkOfferChanged(session, "Offer changed.");
    }

    [Server]
    public void RemoveOfferSlot(PlayerTrade player, int offerIndex, int knownRevision)
    {
        if (!TryGetSession(player, out TradeSession session))
        {
            SendError(player, "You are not in a trade.");
            return;
        }

        if (knownRevision != session.revision)
        {
            SendError(player, "Trade changed. Refresh required.");
            SendUpdate(session, "Trade changed.");
            return;
        }

        List<TradeOfferEntry> offers = GetOffers(session, player);

        if (offerIndex < 0 || offerIndex >= offers.Count)
        {
            SendError(player, "Invalid offer slot.");
            return;
        }

        TradeOfferEntry entry = offers[offerIndex];

        ITradeInventory inventory = GetTradeInventory(player);

        if (inventory != null)
            inventory.UnlockTradeSlotServer(entry.sourceSlotIndex, entry.lockId);

        offers.RemoveAt(offerIndex);

        MarkOfferChanged(session, "Offer changed.");
    }

    [Server]
    public void SetReady(PlayerTrade player, bool ready, int knownRevision)
    {
        if (!TryGetSession(player, out TradeSession session))
        {
            SendError(player, "You are not in a trade.");
            return;
        }

        if (knownRevision != session.revision)
        {
            SendError(player, "Trade changed. Validate again.");
            SendUpdate(session, "Trade changed.");
            return;
        }

        if (!ValidateSessionOffers(session, out string reason))
        {
            CancelSession(session, reason);
            return;
        }

        if (player == session.playerA)
            session.playerAReady = ready;
        else
            session.playerBReady = ready;

        if (!ready)
        {
            if (player == session.playerA)
                session.playerAFinalAccepted = false;
            else
                session.playerBFinalAccepted = false;
        }

        session.state = session.playerAReady && session.playerBReady
            ? TradeSessionState.WaitingFinalAccept
            : TradeSessionState.Open;

        SendUpdate(session, ready ? "Player is ready." : "Player is not ready.");
    }

    [Server]
    public void FinalAccept(PlayerTrade player, int knownRevision, string knownOfferHash)
    {
        if (!TryGetSession(player, out TradeSession session))
        {
            SendError(player, "You are not in a trade.");
            return;
        }

        if (!session.playerAReady || !session.playerBReady)
        {
            SendError(player, "Both players must validate first.");
            return;
        }

        if (knownRevision != session.revision)
        {
            SendError(player, "Trade changed. Validate again.");
            SendUpdate(session, "Trade changed.");
            return;
        }

        string currentHash = TradeUtility.BuildOfferHash(
            session.playerAOffers,
            session.playerBOffers
        );

        if (knownOfferHash != currentHash)
        {
            SendError(player, "Trade content changed. Validate again.");
            SendUpdate(session, "Trade content changed.");
            return;
        }

        if (!ValidateSessionOffers(session, out string reason))
        {
            CancelSession(session, reason);
            return;
        }

        if (player == session.playerA)
            session.playerAFinalAccepted = true;
        else
            session.playerBFinalAccepted = true;

        SendUpdate(session, "Final accept received.");

        if (session.playerAFinalAccepted && session.playerBFinalAccepted)
            CommitTrade(session);
    }

    [Server]
    public void CancelTradeFor(PlayerTrade player, string reason)
    {
        if (!TryGetSession(player, out TradeSession session))
            return;

        CancelSession(session, reason);
    }

    [Server]
    private void CreateSession(PlayerTrade playerA, PlayerTrade playerB)
    {
        TradeSession session = new TradeSession
        {
            sessionId = Guid.NewGuid().ToString("N"),
            playerA = playerA,
            playerB = playerB,
            state = TradeSessionState.Open,
            revision = 0
        };

        sessionsById.Add(session.sessionId, session);
        sessionByPlayerNetId.Add(playerA.netId, session.sessionId);
        sessionByPlayerNetId.Add(playerB.netId, session.sessionId);

        SendUpdate(session, "Trade started.");
    }

    [Server]
    private void CommitTrade(TradeSession session)
    {
        if (!ValidateSessionOffers(session, out string reason))
        {
            CancelSession(session, reason);
            return;
        }

        ITradeInventory invA = GetTradeInventory(session.playerA);
        ITradeInventory invB = GetTradeInventory(session.playerB);

        if (invA == null || invB == null)
        {
            CancelSession(session, "Inventory missing.");
            return;
        }

        List<LootPayload> aToB = session.playerAOffers
            .Select(o => o.payload.CloneWithAmount(o.amount))
            .ToList();

        List<LootPayload> bToA = session.playerBOffers
            .Select(o => o.payload.CloneWithAmount(o.amount))
            .ToList();

        if (!invA.CanReceiveTradePayloadsServer(bToA))
        {
            SendError(session.playerA, "Not enough inventory space.");
            SendError(session.playerB, $"{session.playerA.TradeDisplayName} has no inventory space.");
            return;
        }

        if (!invB.CanReceiveTradePayloadsServer(aToB))
        {
            SendError(session.playerB, "Not enough inventory space.");
            SendError(session.playerA, $"{session.playerB.TradeDisplayName} has no inventory space.");
            return;
        }

        string snapshotA = invA.CreateTradeSnapshotServer();
        string snapshotB = invB.CreateTradeSnapshotServer();

        bool success = true;

        try
        {
            foreach (TradeOfferEntry offer in session.playerAOffers)
            {
                if (!invA.TryRemoveTradePayloadServer(
                        offer.sourceSlotIndex,
                        offer.amount,
                        offer.lockId,
                        out LootPayload removedPayload
                    ))
                {
                    success = false;
                    break;
                }
            }

            if (success)
            {
                foreach (TradeOfferEntry offer in session.playerBOffers)
                {
                    if (!invB.TryRemoveTradePayloadServer(
                            offer.sourceSlotIndex,
                            offer.amount,
                            offer.lockId,
                            out LootPayload removedPayload
                        ))
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (success)
            {
                foreach (LootPayload payload in bToA)
                {
                    if (!invA.TryAddTradePayloadServer(payload))
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (success)
            {
                foreach (LootPayload payload in aToB)
                {
                    if (!invB.TryAddTradePayloadServer(payload))
                    {
                        success = false;
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TradeManager] Commit exception: {e}");
            success = false;
        }

        if (!success)
        {
            invA.RestoreTradeSnapshotServer(snapshotA);
            invB.RestoreTradeSnapshotServer(snapshotB);

            UnlockAll(session);

            CancelSession(session, "Trade failed. Rollback applied.");
            return;
        }

        UnlockAll(session);

        session.state = TradeSessionState.Completed;

        CloseSession(session, "Trade completed.");
    }

    [Server]
    private bool ValidateSessionOffers(TradeSession session, out string reason)
    {
        reason = "";

        if (session == null || session.playerA == null || session.playerB == null)
        {
            reason = "Trade session is invalid.";
            return false;
        }

        if (!ValidatePlayerOffers(session.playerA, session.playerAOffers, out reason))
            return false;

        if (!ValidatePlayerOffers(session.playerB, session.playerBOffers, out reason))
            return false;

        return true;
    }

    [Server]
    private bool ValidatePlayerOffers(
        PlayerTrade owner,
        List<TradeOfferEntry> offers,
        out string reason
    )
    {
        reason = "";

        ITradeInventory inventory = GetTradeInventory(owner);

        if (inventory == null)
        {
            reason = "Trade inventory missing.";
            return false;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            TradeOfferEntry offer = offers[i];

            if (offer.ownerNetId != owner.netId)
            {
                reason = "Offer owner mismatch.";
                return false;
            }

            if (!inventory.IsTradeSlotLockedServer(offer.sourceSlotIndex))
            {
                reason = "Offered item is no longer locked.";
                return false;
            }

            if (!inventory.TryGetTradePayloadServer(offer.sourceSlotIndex, out LootPayload currentPayload))
            {
                reason = "Offered item no longer exists.";
                return false;
            }

            if (!TradeUtility.PayloadStillMatchesOffer(currentPayload, offer))
            {
                reason = "Offered item changed.";
                return false;
            }

            if (!TradeUtility.IsPayloadTradeable(currentPayload, out string tradeableReason))
            {
                reason = tradeableReason;
                return false;
            }
        }

        return true;
    }

    [Server]
    private bool CanStartTrade(PlayerTrade requester, PlayerTrade target, out string reason)
    {
        reason = "";

        if (requester == null || target == null)
        {
            reason = "Invalid trade target.";
            return false;
        }

        if (requester == target)
        {
            reason = "You cannot trade with yourself.";
            return false;
        }

        if (requester.connectionToClient == null || target.connectionToClient == null)
        {
            reason = "Both players must be connected.";
            return false;
        }

        if (requester.gameObject.scene != target.gameObject.scene)
        {
            reason = "Players are not in the same scene.";
            return false;
        }

        if (Vector3.Distance(requester.transform.position, target.transform.position) > maxTradeDistance)
        {
            reason = "Target player is too far away.";
            return false;
        }

        if (sessionByPlayerNetId.ContainsKey(requester.netId))
        {
            reason = "You are already trading.";
            return false;
        }

        if (sessionByPlayerNetId.ContainsKey(target.netId))
        {
            reason = "Target player is already trading.";
            return false;
        }

        if (GetTradeInventory(requester) == null)
        {
            reason = "Requester has no trade inventory.";
            return false;
        }

        if (GetTradeInventory(target) == null)
        {
            reason = "Target has no trade inventory.";
            return false;
        }

        return true;
    }

    [Server]
    private void MarkOfferChanged(TradeSession session, string message)
    {
        session.revision++;

        session.playerAReady = false;
        session.playerBReady = false;

        session.playerAFinalAccepted = false;
        session.playerBFinalAccepted = false;

        session.state = TradeSessionState.Open;

        SendUpdate(session, message);
    }

    [Server]
    private void SendUpdate(TradeSession session, string message)
    {
        if (session == null)
            return;

        string offerHash = TradeUtility.BuildOfferHash(
            session.playerAOffers,
            session.playerBOffers
        );

        TradeStateDto dtoA = BuildStateDto(session, session.playerA, offerHash, message);
        TradeStateDto dtoB = BuildStateDto(session, session.playerB, offerHash, message);

        session.playerA.TargetReceiveTradeState(
            session.playerA.connectionToClient,
            JsonUtility.ToJson(dtoA)
        );

        session.playerB.TargetReceiveTradeState(
            session.playerB.connectionToClient,
            JsonUtility.ToJson(dtoB)
        );
    }

    [Server]
    private TradeStateDto BuildStateDto(
        TradeSession session,
        PlayerTrade viewer,
        string offerHash,
        string message
    )
    {
        bool viewerIsA = viewer == session.playerA;

        List<TradeOfferEntry> selfOffers = viewerIsA
            ? session.playerAOffers
            : session.playerBOffers;

        List<TradeOfferEntry> otherOffers = viewerIsA
            ? session.playerBOffers
            : session.playerAOffers;

        TradeStateDto dto = new TradeStateDto
        {
            sessionId = session.sessionId,
            state = session.state,

            selfNetId = viewer.netId,
            otherNetId = viewerIsA ? session.playerB.netId : session.playerA.netId,

            otherName = viewerIsA
                ? session.playerB.TradeDisplayName
                : session.playerA.TradeDisplayName,

            revision = session.revision,
            offerHash = offerHash,

            selfReady = viewerIsA ? session.playerAReady : session.playerBReady,
            otherReady = viewerIsA ? session.playerBReady : session.playerAReady,

            selfFinalAccepted = viewerIsA
                ? session.playerAFinalAccepted
                : session.playerBFinalAccepted,

            otherFinalAccepted = viewerIsA
                ? session.playerBFinalAccepted
                : session.playerAFinalAccepted,

            message = message ?? ""
        };

        for (int i = 0; i < selfOffers.Count; i++)
            dto.selfOffers.Add(TradeUtility.ToView(selfOffers[i], i));

        for (int i = 0; i < otherOffers.Count; i++)
            dto.otherOffers.Add(TradeUtility.ToView(otherOffers[i], i));

        return dto;
    }

    [Server]
    private void CancelSession(TradeSession session, string reason)
    {
        if (session == null)
            return;

        session.state = TradeSessionState.Cancelled;

        UnlockAll(session);

        CloseSession(session, reason);
    }

    [Server]
    private void CloseSession(TradeSession session, string reason)
    {
        if (session == null)
            return;

        sessionsById.Remove(session.sessionId);

        if (session.playerA != null)
            sessionByPlayerNetId.Remove(session.playerA.netId);

        if (session.playerB != null)
            sessionByPlayerNetId.Remove(session.playerB.netId);

        if (session.playerA != null && session.playerA.connectionToClient != null)
            session.playerA.TargetTradeClosed(session.playerA.connectionToClient, reason);

        if (session.playerB != null && session.playerB.connectionToClient != null)
            session.playerB.TargetTradeClosed(session.playerB.connectionToClient, reason);
    }

    [Server]
    private void UnlockAll(TradeSession session)
    {
        if (session == null)
            return;

        UnlockOffers(session.playerA, session.playerAOffers);
        UnlockOffers(session.playerB, session.playerBOffers);
    }

    [Server]
    private void UnlockOffers(PlayerTrade player, List<TradeOfferEntry> offers)
    {
        if (player == null || offers == null)
            return;

        ITradeInventory inventory = GetTradeInventory(player);

        if (inventory == null)
            return;

        foreach (TradeOfferEntry offer in offers)
            inventory.UnlockTradeSlotServer(offer.sourceSlotIndex, offer.lockId);
    }

    [Server]
    private bool TryGetSession(PlayerTrade player, out TradeSession session)
    {
        session = null;

        if (player == null)
            return false;

        if (!sessionByPlayerNetId.TryGetValue(player.netId, out string sessionId))
            return false;

        return sessionsById.TryGetValue(sessionId, out session);
    }

    [Server]
    private List<TradeOfferEntry> GetOffers(TradeSession session, PlayerTrade player)
    {
        return player == session.playerA
            ? session.playerAOffers
            : session.playerBOffers;
    }

    [Server]
    private ITradeInventory GetTradeInventory(PlayerTrade player)
    {
        if (player == null)
            return null;

        MonoBehaviour[] behaviours = player.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ITradeInventory tradeInventory)
                return tradeInventory;
        }

        return null;
    }

    [Server]
    private string BuildLockId(TradeSession session, PlayerTrade player, int slotIndex)
    {
        return $"TRADE:{session.sessionId}:{player.netId}:{slotIndex}:{Guid.NewGuid():N}";
    }

    [Server]
    private void SendError(PlayerTrade player, string message)
    {
        if (player == null || player.connectionToClient == null)
            return;

        player.TargetTradeError(player.connectionToClient, message);
    }

    [Server]
    private void CleanupExpiredInvites()
    {
        pendingInvites.RemoveAll(i =>
            i == null ||
            i.requester == null ||
            i.target == null ||
            Time.time >= i.expiresAt
        );
    }

    private class TradeInvite
    {
        public PlayerTrade requester;
        public PlayerTrade target;
        public float expiresAt;
    }

    private class TradeSession
    {
        public string sessionId;

        public PlayerTrade playerA;
        public PlayerTrade playerB;

        public TradeSessionState state = TradeSessionState.None;

        public int revision;

        public bool playerAReady;
        public bool playerBReady;

        public bool playerAFinalAccepted;
        public bool playerBFinalAccepted;

        public List<TradeOfferEntry> playerAOffers = new List<TradeOfferEntry>();
        public List<TradeOfferEntry> playerBOffers = new List<TradeOfferEntry>();
    }
}