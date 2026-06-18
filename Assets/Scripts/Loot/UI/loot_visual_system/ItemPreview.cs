using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPreview : MonoBehaviour
{
    [SerializeField] private Image backGroundImage;

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI baseType;
    [SerializeField] private TextMeshProUGUI itemLevel;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private GameObject modsContainer;
    [SerializeField] private GameObject modLineUI;

    public void Init(InventoryItemData data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemPreview] Init called with null data.");
            return;
        }

        ClearMods();

        LootVisualStyle style = LootVisualManager.Instance != null
            ? LootVisualManager.Instance.Resolve(data)
            : LootVisualStyle.CreateFallback();

        ApplySharedStyle(style);

        if (data.lootableType == LootableType.GeneratedItem ||
            !string.IsNullOrWhiteSpace(data.itemJson))
        {
            InitGeneratedItem(data, style);
            return;
        }

        if (data.lootableType == LootableType.Sigil ||
            data.lootableType == LootableType.Currency)
        {
            InitSimpleLoot(data, style);
            return;
        }

        InitUnknown(data, style);
    }

    private void ApplySharedStyle(LootVisualStyle style)
    {
        if (backGroundImage != null)
            backGroundImage.color = style.previewBackgroundColor;

        if (itemName != null)
            itemName.color = style.previewNameTextColor;

        if (baseType != null)
            baseType.color = style.previewBodyTextColor;

        if (itemLevel != null)
            itemLevel.color = style.previewBodyTextColor;

        if (descriptionText != null)
            descriptionText.color = style.previewBodyTextColor;
    }

    private void InitGeneratedItem(InventoryItemData data, LootVisualStyle style)
    {
        ItemInstance item = JsonUtility.FromJson<ItemInstance>(data.itemJson);

        if (item == null)
        {
            Debug.LogError("[ItemPreview] Failed to parse ItemInstance.");
            InitUnknown(data, style);
            return;
        }

        item.EnsureLists();

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);

        if (itemBase == null)
        {
            Debug.LogError($"[ItemPreview] No ItemBase found for baseId={item.baseId}");
            InitUnknown(data, style);
            return;
        }

        if (itemName != null)
        {
            itemName.gameObject.SetActive(true);
            itemName.text = string.IsNullOrWhiteSpace(item.itemName)
                ? itemBase.BaseName
                : item.itemName;
            itemName.color = style.previewNameTextColor;
        }

        if (baseType != null)
        {
            baseType.gameObject.SetActive(true);
            baseType.text = itemBase.BaseName;
            baseType.color = style.previewBodyTextColor;
        }

        if (itemLevel != null)
        {
            itemLevel.gameObject.SetActive(true);
            itemLevel.text = $"Item Level {item.itemLevel}";
            itemLevel.color = style.previewBodyTextColor;
        }

        if (modsContainer != null)
            modsContainer.SetActive(true);

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        AddAffixLines(item.prefixes, "Prefix", style.previewModTextColor);
        AddAffixLines(item.suffixes, "Suffix", style.previewModTextColor);
    }

    private void InitSimpleLoot(InventoryItemData data, LootVisualStyle style)
    {
        LootableSO lootable = LootableDatabase.Get(data.lootableId);

        if (itemName != null)
        {
            string finalName = data.displayNameOverride;

            if (string.IsNullOrWhiteSpace(finalName) && lootable != null)
                finalName = lootable.DisplayName;

            if (string.IsNullOrWhiteSpace(finalName))
                finalName = $"Lootable {data.lootableId}";

            itemName.text = finalName;
            itemName.color = style.previewNameTextColor;
            itemName.gameObject.SetActive(true);
        }

        if (baseType != null)
            baseType.gameObject.SetActive(false);

        if (itemLevel != null)
            itemLevel.gameObject.SetActive(false);

        if (modsContainer != null)
            modsContainer.SetActive(false);

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.color = style.previewBodyTextColor;

            if (!string.IsNullOrWhiteSpace(data.description))
                descriptionText.text = data.description;
            else if (lootable != null)
                descriptionText.text = lootable.DisplayName;
            else
                descriptionText.text = "No description available.";
        }
    }

    private void InitUnknown(InventoryItemData data, LootVisualStyle style)
    {
        if (itemName != null)
        {
            itemName.gameObject.SetActive(true);
            itemName.text = string.IsNullOrWhiteSpace(data.displayNameOverride)
                ? "Unknown Loot"
                : data.displayNameOverride;
            itemName.color = style.previewNameTextColor;
        }

        if (baseType != null)
            baseType.gameObject.SetActive(false);

        if (itemLevel != null)
            itemLevel.gameObject.SetActive(false);

        if (modsContainer != null)
            modsContainer.SetActive(false);

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.color = style.previewBodyTextColor;
            descriptionText.text = string.IsNullOrWhiteSpace(data.description)
                ? "No description available."
                : data.description;
        }
    }

    private void ClearMods()
    {
        if (modsContainer == null)
            return;

        foreach (Transform child in modsContainer.transform)
            Destroy(child.gameObject);
    }

    private void AddAffixLines(
        System.Collections.Generic.List<ItemAffix> affixes,
        string label,
        Color textColor)
    {
        if (affixes == null || modsContainer == null || modLineUI == null)
            return;

        for (int i = 0; i < affixes.Count; i++)
        {
            GameObject modLine = Instantiate(modLineUI, modsContainer.transform);
            TextMeshProUGUI modText = modLine.GetComponentInChildren<TextMeshProUGUI>();

            if (modText == null)
            {
                Debug.LogError("[ItemPreview] modLineUI prefab has no TextMeshProUGUI.");
                continue;
            }

            modText.color = textColor;

            AffixSO affix = AffixDatabase.Get(affixes[i].affixId);

            if (affix == null)
            {
                modText.text = "Unknown Mod";
                continue;
            }

            modText.text = $"[{label}] +{affixes[i].value} to {affix.stat}";
        }
    }
}
