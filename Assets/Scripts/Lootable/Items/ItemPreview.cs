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

    [SerializeField] private Color normalColor;
    [SerializeField] private Color magicColor;
    [SerializeField] private Color rareColor;
    [SerializeField] private Color uniqueColor;
    [SerializeField] private Color sigilColor;
    [SerializeField] private Color currencyColor;

    public void Init(InventoryItemData data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemPreview] Init called with null data.");
            return;
        }

        ClearMods();

        if (data.lootableType == LootableType.GeneratedItem || !string.IsNullOrWhiteSpace(data.itemJson))
        {
            InitGeneratedItem(data);
            return;
        }

        if (data.lootableType == LootableType.Sigil)
        {
            InitSimpleLoot(data, sigilColor);
            return;
        }

        if (data.lootableType == LootableType.Currency)
        {
            InitSimpleLoot(data, currencyColor);
            return;
        }

        InitUnknown(data);
    }

    private void InitGeneratedItem(InventoryItemData data)
    {
        ItemInstance item = JsonUtility.FromJson<ItemInstance>(data.itemJson);

        if (item == null)
        {
            Debug.LogError("[ItemPreview] Failed to parse ItemInstance.");
            InitUnknown(data);
            return;
        }

        item.EnsureLists();

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);

        if (itemBase == null)
        {
            Debug.LogError($"[ItemPreview] No ItemBase found for baseId={item.baseId}");
            InitUnknown(data);
            return;
        }

        Color color = GetWantedColor(item.rarity);

        if (backGroundImage != null)
            backGroundImage.color = color;

        if (itemName != null)
        {
            itemName.text = string.IsNullOrEmpty(item.itemName) ? itemBase.BaseName : item.itemName;
            itemName.color = color;
        }

        if (baseType != null)
        {
            baseType.gameObject.SetActive(true);
            baseType.text = itemBase.BaseName;
        }

        if (itemLevel != null)
        {
            itemLevel.gameObject.SetActive(true);
            itemLevel.text = $"Item Level {item.itemLevel}";
        }

        if (modsContainer != null)
            modsContainer.SetActive(true);

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        AddAffixLines(item.prefixes, "Prefix");
        AddAffixLines(item.suffixes, "Suffix");
    }

    private void InitSimpleLoot(InventoryItemData data, Color color)
    {
        LootableSO lootable = LootableDatabase.Get(data.lootableId);

        if (backGroundImage != null)
            backGroundImage.color = color;

        if (itemName != null)
        {
            string finalName = data.displayNameOverride;

            if (string.IsNullOrWhiteSpace(finalName) && lootable != null)
                finalName = lootable.DisplayName;

            if (string.IsNullOrWhiteSpace(finalName))
                finalName = $"Lootable {data.lootableId}";

            itemName.text = finalName;
            itemName.color = color;
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

            descriptionText.text = string.IsNullOrWhiteSpace(data.description)
                ? lootable != null ? lootable.DisplayName : "No description available."
                : data.description;
        }
    }

    private void InitUnknown(InventoryItemData data)
    {
        if (backGroundImage != null)
            backGroundImage.color = Color.gray;

        if (itemName != null)
        {
            itemName.text = string.IsNullOrEmpty(data.displayNameOverride)
                ? "Unknown Loot"
                : data.displayNameOverride;

            itemName.color = Color.white;
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
            descriptionText.text = string.IsNullOrWhiteSpace(data.description)
                ? "No description available."
                : data.description  ;
        }
    }

    private void ClearMods()
    {
        if (modsContainer == null)
            return;

        foreach (Transform child in modsContainer.transform)
            Destroy(child.gameObject);
    }

    private void AddAffixLines(System.Collections.Generic.List<ItemAffix> affixes, string label)
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

            AffixSO affix = AffixDatabase.Get(affixes[i].affixId);

            if (affix == null)
            {
                modText.text = "Unknown Mod";
                continue;
            }

            modText.text = $"[{label}] +{affixes[i].value} to {affix.stat}";
        }
    }

    private Color GetWantedColor(ItemRarity itemRarity)
    {
        switch (itemRarity)
        {
            case ItemRarity.Normal:
                return normalColor;

            case ItemRarity.Magic:
                return magicColor;

            case ItemRarity.Rare:
                return rareColor;

            case ItemRarity.Unique:
                return uniqueColor;

            default:
                return Color.white;
        }
    }
}