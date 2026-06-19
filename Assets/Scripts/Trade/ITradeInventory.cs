using System.Collections.Generic;

public interface ITradeInventory
{
    int TradeSlotCount { get; }

    bool TryGetTradePayloadServer(int slotIndex, out LootPayload payload);

    bool IsTradeSlotLockedServer(int slotIndex);

    bool TryLockTradeSlotServer(int slotIndex, int amount, string lockId);

    void UnlockTradeSlotServer(int slotIndex, string lockId);

    bool CanReceiveTradePayloadsServer(List<LootPayload> payloads);

    string CreateTradeSnapshotServer();

    void RestoreTradeSnapshotServer(string snapshotJson);

    bool TryRemoveTradePayloadServer(
        int slotIndex,
        int amount,
        string lockId,
        out LootPayload removedPayload
    );

    bool TryAddTradePayloadServer(LootPayload payload);
}