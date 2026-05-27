using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArcanaLoadout : NetworkBehaviour
{
    public class SyncListString : SyncList<string> { }

    [Header("Loadout Settings")]
    [SerializeField] private int arcanaSlotCount = 4;
    [SerializeField] private int runeSlotsPerArcana = 3;

    [Header("Temporary Owned Arcana")]
    [SerializeField]
    private string[] ownedArcanaNames =
    {
        "Fireball",
        "Frostball"
    };

    [Header("Temporary Owned Runes")]
    [SerializeField]
    private string[] ownedRuneIds =
    {
        "splitting_rune",
        "piercing_rune",
        "rapid_rune"
    };

    public SyncListString EquippedSlotsJson = new SyncListString();

    private readonly List<ArcanaLoadoutSlotData> slots = new();

    public event Action OnLoadoutChanged;

    public int ArcanaSlotCount => arcanaSlotCount;
    public int RuneSlotsPerArcana => runeSlotsPerArcana;

    public string[] OwnedArcanaNames => ownedArcanaNames;
    public string[] OwnedRuneIds => ownedRuneIds;

    public override void OnStartServer()
    {
        base.OnStartServer();

        EnsureSlotsServer();
        RefreshSyncedSlots();

        // Build initial runtime spells.
        RebuildRuntimeArcanaServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        EquippedSlotsJson.Callback += OnEquippedSlotsChanged;
        OnLoadoutChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        EquippedSlotsJson.Callback -= OnEquippedSlotsChanged;
        base.OnStopClient();
    }

    private void OnEquippedSlotsChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        OnLoadoutChanged?.Invoke();
    }

    [Command]
    public void CmdEquipArcana(int slotIndex, string arcanaName)
    {
        EquipArcanaServer(slotIndex, arcanaName);
    }

    [Command]
    public void CmdUnequipArcana(int slotIndex)
    {
        UnequipArcanaServer(slotIndex);
    }

    [Command]
    public void CmdEquipRune(int arcanaSlotIndex, int runeSlotIndex, string runeId)
    {
        EquipRuneServer(arcanaSlotIndex, runeSlotIndex, runeId);
    }

    [Command]
    public void CmdUnequipRune(int arcanaSlotIndex, int runeSlotIndex)
    {
        UnequipRuneServer(arcanaSlotIndex, runeSlotIndex);
    }

    [Server]
    private void EquipArcanaServer(int slotIndex, string arcanaName)
    {
        EnsureSlotsServer();

        if (!IsValidArcanaSlot(slotIndex)) return;
        if (string.IsNullOrWhiteSpace(arcanaName)) return;

        if (!PlayerOwnsArcana(arcanaName))
        {
            Debug.LogWarning($"[ArcanaLoadout] Player does not own Arcana: {arcanaName}");
            return;
        }

        slots[slotIndex].arcanaName = arcanaName;

        RefreshSyncedSlots();
        RebuildRuntimeArcanaServer();

        Debug.Log($"[ArcanaLoadout] Equipped Arcana {arcanaName} in slot {slotIndex}");
    }

    [Server]
    private void UnequipArcanaServer(int slotIndex)
    {
        EnsureSlotsServer();

        if (!IsValidArcanaSlot(slotIndex)) return;

        slots[slotIndex] = new ArcanaLoadoutSlotData(runeSlotsPerArcana);

        RefreshSyncedSlots();
        RebuildRuntimeArcanaServer();

        Debug.Log($"[ArcanaLoadout] Unequipped Arcana slot {slotIndex}");
    }

    [Server]
    private void EquipRuneServer(int arcanaSlotIndex, int runeSlotIndex, string runeId)
    {
        EnsureSlotsServer();

        if (!IsValidArcanaSlot(arcanaSlotIndex)) return;
        if (!IsValidRuneSlot(runeSlotIndex)) return;
        if (string.IsNullOrWhiteSpace(runeId)) return;

        ArcanaLoadoutSlotData slot = slots[arcanaSlotIndex];

        if (string.IsNullOrWhiteSpace(slot.arcanaName))
        {
            Debug.LogWarning("[ArcanaLoadout] Cannot equip Rune without Arcana.");
            return;
        }

        if (!PlayerOwnsRune(runeId))
        {
            Debug.LogWarning($"[ArcanaLoadout] Player does not own Rune: {runeId}");
            return;
        }

        RuneSO rune = SpellsManager.Instance.GetRune(runeId);

        if (rune == null)
            return;

        Spell arcana = SpellsManager.Instance.GetSpell(slot.arcanaName);

        if (arcana == null)
            return;

        if (!rune.CanApplyTo(arcana.GetData()))
        {
            Debug.LogWarning($"[ArcanaLoadout] Rune {runeId} incompatible with Arcana {slot.arcanaName}");
            return;
        }

        slot.runeIds[runeSlotIndex] = runeId;

        RefreshSyncedSlots();
        RebuildRuntimeArcanaServer();

        Debug.Log($"[ArcanaLoadout] Equipped Rune {runeId} in Arcana slot {arcanaSlotIndex}, rune slot {runeSlotIndex}");
    }

    [Server]
    private void UnequipRuneServer(int arcanaSlotIndex, int runeSlotIndex)
    {
        EnsureSlotsServer();

        if (!IsValidArcanaSlot(arcanaSlotIndex)) return;
        if (!IsValidRuneSlot(runeSlotIndex)) return;

        slots[arcanaSlotIndex].runeIds[runeSlotIndex] = "";

        RefreshSyncedSlots();
        RebuildRuntimeArcanaServer();

        Debug.Log($"[ArcanaLoadout] Unequipped Rune from Arcana slot {arcanaSlotIndex}, rune slot {runeSlotIndex}");
    }

    [Server]
    public void RebuildRuntimeArcanaServer()
    {
        NetworkEntity entity = GetComponent<NetworkEntity>();

        if (entity == null)
        {
            Debug.LogError("[ArcanaLoadout] NetworkEntity missing.");
            return;
        }

        entity.ClearRuntimeArcana();

        EnsureSlotsServer();

        for (int i = 0; i < slots.Count; i++)
        {
            ArcanaLoadoutSlotData slot = slots[i];

            if (slot == null || string.IsNullOrWhiteSpace(slot.arcanaName))
                continue;

            List<string> validRunes = new();

            foreach (string runeId in slot.runeIds)
            {
                if (!string.IsNullOrWhiteSpace(runeId))
                    validRunes.Add(runeId);
            }

            entity.AddArcanaWithRunes(slot.arcanaName, validRunes.ToArray());
        }
    }
    [Server]
    public void EquipArcanaWithRunesServer(int slotIndex, string arcanaName, params string[] runeIds)
    {
        EnsureSlotsServer();

        if (!IsValidArcanaSlot(slotIndex))
            return;

        if (string.IsNullOrWhiteSpace(arcanaName))
            return;

        if (!PlayerOwnsArcana(arcanaName))
        {
            Debug.LogWarning($"[ArcanaLoadout] Player does not own Arcana: {arcanaName}");
            return;
        }

        slots[slotIndex].arcanaName = arcanaName;

        for (int i = 0; i < runeSlotsPerArcana; i++)
        {
            string runeId = i < runeIds.Length ? runeIds[i] : "";
            slots[slotIndex].runeIds[i] = runeId;
        }

        RefreshSyncedSlots();
        RebuildRuntimeArcanaServer();

        Debug.Log($"[ArcanaLoadout] Equipped {arcanaName} with {runeIds.Length} rune(s) in slot {slotIndex}");
    }
    [Server]
    public void EquipStarterBuildServer()
    {
        EquipArcanaWithRunesServer(
            0,
            "Fireball",
            "splitting_rune",
            "piercing_rune"
        );
    }
    public ArcanaLoadoutSlotData GetSlot(int index)
    {
        if (index < 0 || index >= EquippedSlotsJson.Count)
            return new ArcanaLoadoutSlotData(runeSlotsPerArcana);

        string json = EquippedSlotsJson[index];

        if (string.IsNullOrWhiteSpace(json))
            return new ArcanaLoadoutSlotData(runeSlotsPerArcana);

        try
        {
            ArcanaLoadoutSlotData data = JsonUtility.FromJson<ArcanaLoadoutSlotData>(json);

            if (data.runeIds == null)
                data.runeIds = new List<string>();

            while (data.runeIds.Count < runeSlotsPerArcana)
                data.runeIds.Add("");

            return data;
        }
        catch
        {
            return new ArcanaLoadoutSlotData(runeSlotsPerArcana);
        }
    }

    public int GetFirstEmptyRuneSlot(int arcanaSlotIndex)
    {
        ArcanaLoadoutSlotData slot = GetSlot(arcanaSlotIndex);

        for (int i = 0; i < slot.runeIds.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(slot.runeIds[i]))
                return i;
        }

        return -1;
    }

    [Server]
    private void EnsureSlotsServer()
    {
        while (slots.Count < arcanaSlotCount)
            slots.Add(new ArcanaLoadoutSlotData(runeSlotsPerArcana));

        while (slots.Count > arcanaSlotCount)
            slots.RemoveAt(slots.Count - 1);

        foreach (ArcanaLoadoutSlotData slot in slots)
        {
            if (slot.runeIds == null)
                slot.runeIds = new List<string>();

            while (slot.runeIds.Count < runeSlotsPerArcana)
                slot.runeIds.Add("");

            while (slot.runeIds.Count > runeSlotsPerArcana)
                slot.runeIds.RemoveAt(slot.runeIds.Count - 1);
        }
    }

    [Server]
    private void RefreshSyncedSlots()
    {
        EquippedSlotsJson.Clear();

        for (int i = 0; i < slots.Count; i++)
            EquippedSlotsJson.Add(JsonUtility.ToJson(slots[i]));

        OnLoadoutChanged?.Invoke();
    }

    private bool IsValidArcanaSlot(int index)
    {
        return index >= 0 && index < arcanaSlotCount;
    }

    private bool IsValidRuneSlot(int index)
    {
        return index >= 0 && index < runeSlotsPerArcana;
    }

    private bool PlayerOwnsArcana(string arcanaName)
    {
        foreach (string owned in ownedArcanaNames)
        {
            if (owned == arcanaName)
                return true;
        }

        return false;
    }
    [Server]
    public void EquipStarterBuildIfEmptyServer()
    {
        EnsureSlotsServer();

        bool hasEquippedArcana = false;

        foreach (ArcanaLoadoutSlotData slot in slots)
        {
            if (slot != null && !string.IsNullOrWhiteSpace(slot.arcanaName))
            {
                hasEquippedArcana = true;
                break;
            }
        }

        if (hasEquippedArcana)
        {
            Debug.Log("[ArcanaLoadout] Existing loadout found, rebuilding runtime arcana.");
            RefreshSyncedSlots();
            RebuildRuntimeArcanaServer();
            return;
        }

        Debug.Log("[ArcanaLoadout] Empty loadout, giving starter build.");

        EquipArcanaWithRunesServer(
            0,
            "Fireball",
            "splitting_rune",
            "piercing_rune"
        );
    }
    private bool PlayerOwnsRune(string runeId)
    {
        foreach (string owned in ownedRuneIds)
        {
            if (owned == runeId)
                return true;
        }

        return false;
    }
    [Serializable]
    public class ArcanaLoadoutSaveData
    {
        public List<ArcanaLoadoutSlotData> slots = new();
    }
    [Server]
    public string ToSaveJsonServer()
    {
        EnsureSlotsServer();

        ArcanaLoadoutSaveData saveData = new ArcanaLoadoutSaveData();

        foreach (ArcanaLoadoutSlotData slot in slots)
            saveData.slots.Add(slot);

        return JsonUtility.ToJson(saveData);
    }
    [Server]
    public void LoadFromSaveJsonServer(string json)
    {
        EnsureSlotsServer();

        if (string.IsNullOrWhiteSpace(json))
        {
            EquipStarterBuildIfEmptyServer();
            return;
        }

        ArcanaLoadoutSaveData saveData = JsonUtility.FromJson<ArcanaLoadoutSaveData>(json);

        slots.Clear();

        if (saveData != null && saveData.slots != null)
        {
            foreach (ArcanaLoadoutSlotData slot in saveData.slots)
                slots.Add(slot);
        }

        EnsureSlotsServer();
        RefreshSyncedSlots();
        RebuildRuntimeArcanaServer();

        EquipStarterBuildIfEmptyServer();
    }
}