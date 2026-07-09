using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class TradeUtility
{
    public static string BuildPayloadHash(LootPayload payload)
    {
        if (payload == null)
            return "";

        string raw =
            payload.lootableId + "|" +
            Mathf.Max(1, payload.amount) + "|" +
            (payload.itemJson ?? "") + "|" +
            (payload.displayNameOverride ?? "") + "|" +
            payload.hasRarityColor + "|" +
            payload.rarity;

        return Sha256(raw);
    }

    public static string BuildOfferHash(
        List<TradeOfferEntry> playerAOffers,
        List<TradeOfferEntry> playerBOffers)
    {
        StringBuilder sb = new StringBuilder();

        AppendOffers(sb, "A", playerAOffers);
        AppendOffers(sb, "B", playerBOffers);

        return Sha256(sb.ToString());
    }

    private static void AppendOffers(
        StringBuilder sb,
        string label,
        List<TradeOfferEntry> offers)
    {
        sb.Append(label).Append(":");

        if (offers == null)
            return;

        foreach (TradeOfferEntry offer in offers.OrderBy(o => o.offerSlotIndex))
        {
            sb.Append("[")
                .Append("offerSlot=")
                .Append(offer.offerSlotIndex)
                .Append("|owner=")
                .Append(offer.ownerNetId)
                .Append("|sourceSlot=")
                .Append(offer.sourceSlotIndex)
                .Append("|amount=")
                .Append(offer.amount)
                .Append("|hash=")
                .Append(offer.payloadHash)
                .Append("]");
        }
    }

    public static bool PayloadStillMatchesOffer(
        LootPayload currentPayload,
        TradeOfferEntry offer)
    {
        if (currentPayload == null || offer == null || offer.payload == null)
            return false;

        if (currentPayload.lootableId != offer.payload.lootableId)
            return false;

        if (currentPayload.amount < offer.amount)
            return false;

        if ((currentPayload.itemJson ?? "") != (offer.payload.itemJson ?? ""))
            return false;

        LootPayload comparable = currentPayload.CloneWithAmount(offer.amount);
        string currentHash = BuildPayloadHash(comparable);

        return currentHash == offer.payloadHash;
    }

    public static TradeOfferView ToView(TradeOfferEntry entry)
    {
        if (entry == null || entry.payload == null)
            return null;

        LootPayload payload = entry.payload;

        return new TradeOfferView
        {
            offerSlotIndex = entry.offerSlotIndex,

            ownerNetId = entry.ownerNetId,
            sourceSlotIndex = entry.sourceSlotIndex,

            lootableId = payload.lootableId,
            amount = payload.amount,
            itemJson = payload.itemJson ?? "",
            displayName = GetDisplayName(payload),
            hasRarityColor = payload.hasRarityColor,
            rarity = payload.rarity
        };
    }

    public static string GetDisplayName(LootPayload payload)
    {
        if (payload == null)
            return "Unknown";

        if (!string.IsNullOrWhiteSpace(payload.displayNameOverride))
            return payload.displayNameOverride;

        if (!string.IsNullOrWhiteSpace(payload.itemJson))
        {
            try
            {
                ItemInstance item = JsonUtility.FromJson<ItemInstance>(payload.itemJson);

                if (item != null && !string.IsNullOrWhiteSpace(item.itemName))
                    return item.itemName;
            }
            catch
            {
                // ignored
            }
        }

        LootableSO lootable = LootableDatabase.Get(payload.lootableId);

        if (lootable != null)
            return lootable.DisplayName;

        return $"Lootable {payload.lootableId}";
    }

    public static bool IsPayloadTradeable(LootPayload payload, out string reason)
    {
        reason = "";

        if (payload == null || !payload.IsValid())
        {
            reason = "Invalid item.";
            return false;
        }

        LootableSO lootable = LootableDatabase.Get(payload.lootableId);

        if (lootable == null)
        {
            reason = $"Lootable not found. id={payload.lootableId}";
            return false;
        }

        if (!lootable.Tradeable)
        {
            reason = $"{lootable.DisplayName} is not tradeable.";
            return false;
        }

        if (payload.IsGeneratedItem())
        {
            try
            {
                ItemInstance item = JsonUtility.FromJson<ItemInstance>(payload.itemJson);

                if (item == null || item.instanceId == 0)
                {
                    reason = "Generated item has no valid instanceId.";
                    return false;
                }
            }
            catch
            {
                reason = "Generated item json is invalid.";
                return false;
            }
        }

        return true;
    }

    private static string Sha256(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
            byte[] hash = sha.ComputeHash(bytes);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));

            return sb.ToString();
        }
    }
}