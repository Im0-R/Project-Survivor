using System;
using System.Collections.Generic;

public enum TradeSessionState
{
    None,
    Open,
    WaitingFinalAccept,
    Completed,
    Cancelled
}

[Serializable]
public class TradeInviteDto
{
    public uint requesterNetId;
    public string requesterName;
}

[Serializable]
public class TradeOfferEntry
{
    public uint ownerNetId;
    public int sourceSlotIndex;
    public int amount;
    public string lockId;
    public LootPayload payload;
    public string payloadHash;
}

[Serializable]
public class TradeOfferView
{
    public int offerIndex;
    public uint ownerNetId;
    public int sourceSlotIndex;

    public int lootableId;
    public int amount;
    public string itemJson;
    public string displayName;
    public bool hasRarityColor;
    public ItemRarity rarity;
}

[Serializable]
public class TradeStateDto
{
    public string sessionId;
    public TradeSessionState state;

    public uint selfNetId;
    public uint otherNetId;

    public string otherName;

    public int revision;
    public string offerHash;

    public bool selfReady;
    public bool otherReady;

    public bool selfFinalAccepted;
    public bool otherFinalAccepted;

    public string message;

    public List<TradeOfferView> selfOffers = new List<TradeOfferView>();
    public List<TradeOfferView> otherOffers = new List<TradeOfferView>();
}