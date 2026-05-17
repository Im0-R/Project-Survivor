using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasArcana : MonoBehaviour
{
    public static CanvasArcana Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Top Loadout")]
    [SerializeField] private ArcanaLoadoutSlotUI[] loadoutSlots;

    [Header("Bottom Inventory")]
    [SerializeField] private ArcanaButtonUI[] arcanaInventorySlots;
    [SerializeField] private RuneButtonUI[] runeInventorySlots;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Info")]
    [SerializeField] private TMP_Text selectedText;

    private PlayerArcanaLoadout currentLoadout;

    private int selectedArcanaSlotIndex = 0;
    private int selectedRuneSlotIndex = -1;

    private void Awake()
    {
        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (root != null)
            root.SetActive(false);
    }

    public void Open()
    {
        if (NetworkClient.localPlayer == null)
        {
            Debug.LogError("[CanvasArcana] No local player.");
            return;
        }

        PlayerArcanaLoadout loadout = NetworkClient.localPlayer.GetComponent<PlayerArcanaLoadout>();

        if (loadout == null)
        {
            Debug.LogError("[CanvasArcana] Local player has no PlayerArcanaLoadout.");
            return;
        }

        Bind(loadout);

        if (root != null)
            root.SetActive(true);

        RefreshAll();
    }

    public void Close()
    {
        if (currentLoadout != null)
            currentLoadout.OnLoadoutChanged -= RefreshAll;

        currentLoadout = null;

        if (root != null)
            root.SetActive(false);
    }

    private void Bind(PlayerArcanaLoadout loadout)
    {
        if (currentLoadout != null)
            currentLoadout.OnLoadoutChanged -= RefreshAll;

        currentLoadout = loadout;

        if (currentLoadout != null)
            currentLoadout.OnLoadoutChanged += RefreshAll;
    }

    private void RefreshAll()
    {
        RefreshLoadout();
        RefreshInventory();
        RefreshSelectedText();
    }

    private void RefreshLoadout()
    {
        if (currentLoadout == null) return;

        for (int i = 0; i < loadoutSlots.Length; i++)
        {
            int arcanaSlotIndex = i;
            ArcanaLoadoutSlotUI slotUI = loadoutSlots[i];

            if (slotUI == null)
                continue;

            ArcanaLoadoutSlotData data = currentLoadout.GetSlot(i);

            Sprite arcanaIcon = GetArcanaIcon(data.arcanaName);

            slotUI.ArcanaSlot.Set(
                data.arcanaName,
                arcanaIcon,
                () =>
                {
                    selectedArcanaSlotIndex = arcanaSlotIndex;
                    selectedRuneSlotIndex = -1;
                    RefreshAll();
                },
                () =>
                {
                    currentLoadout.CmdUnequipArcana(arcanaSlotIndex);
                },
                selectedArcanaSlotIndex == arcanaSlotIndex && selectedRuneSlotIndex < 0
            );

            RuneButtonUI[] runeSlots = slotUI.RuneSlots;

            for (int r = 0; r < runeSlots.Length; r++)
            {
                int runeSlotIndex = r;
                string runeId = "";

                if (data.runeIds != null && r < data.runeIds.Count)
                    runeId = data.runeIds[r];

                RuneSO rune = GetRune(runeId);

                runeSlots[r].Set(
                    rune != null ? rune.runeName : "",
                    rune != null ? rune.icon : null,
                    () =>
                    {
                        selectedArcanaSlotIndex = arcanaSlotIndex;
                        selectedRuneSlotIndex = runeSlotIndex;
                        RefreshAll();
                    },
                    () =>
                    {
                        currentLoadout.CmdUnequipRune(arcanaSlotIndex, runeSlotIndex);
                    },
                    selectedArcanaSlotIndex == arcanaSlotIndex && selectedRuneSlotIndex == runeSlotIndex
                );
            }
        }
    }

    private void RefreshInventory()
    {
        if (currentLoadout == null) return;

        string[] ownedArcana = currentLoadout.OwnedArcanaNames;
        string[] ownedRunes = currentLoadout.OwnedRuneIds;

        for (int i = 0; i < arcanaInventorySlots.Length; i++)
        {
            if (i >= ownedArcana.Length)
            {
                arcanaInventorySlots[i].Clear();
                continue;
            }

            string arcanaName = ownedArcana[i];

            arcanaInventorySlots[i].Set(
                arcanaName,
                GetArcanaIcon(arcanaName),
                () =>
                {
                    currentLoadout.CmdEquipArcana(selectedArcanaSlotIndex, arcanaName);
                }
            );
        }

        for (int i = 0; i < runeInventorySlots.Length; i++)
        {
            if (i >= ownedRunes.Length)
            {
                runeInventorySlots[i].Clear();
                continue;
            }

            string runeId = ownedRunes[i];
            RuneSO rune = GetRune(runeId);

            runeInventorySlots[i].Set(
                rune != null ? rune.runeName : runeId,
                rune != null ? rune.icon : null,
                () =>
                {
                    int runeSlot = selectedRuneSlotIndex;

                    if (runeSlot < 0)
                        runeSlot = currentLoadout.GetFirstEmptyRuneSlot(selectedArcanaSlotIndex);

                    if (runeSlot < 0)
                    {
                        Debug.LogWarning("[CanvasArcana] No empty Rune slot.");
                        return;
                    }

                    currentLoadout.CmdEquipRune(selectedArcanaSlotIndex, runeSlot, runeId);
                }
            );
        }
    }

    private void RefreshSelectedText()
    {
        if (selectedText == null) return;

        if (selectedRuneSlotIndex >= 0)
            selectedText.text = $"Selected Arcana Slot {selectedArcanaSlotIndex + 1}, Rune Slot {selectedRuneSlotIndex + 1}";
        else
            selectedText.text = $"Selected Arcana Slot {selectedArcanaSlotIndex + 1}";
    }

    private Sprite GetArcanaIcon(string arcanaName)
    {
        if (string.IsNullOrWhiteSpace(arcanaName))
            return null;

        if (SpellsManager.Instance == null)
            return null;

        return SpellsManager.Instance.GetSpellIcon(arcanaName);
    }

    private RuneSO GetRune(string runeId)
    {
        if (string.IsNullOrWhiteSpace(runeId))
            return null;

        if (SpellsManager.Instance == null)
            return null;

        return SpellsManager.Instance.GetRune(runeId);
    }
}