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

        switch (data.lootableType)
        {
            case LootableType.GeneratedItem:
                InitGeneratedItem(data);
                break;

            case LootableType.Sigil:
                InitSimpleLoot(data, sigilColor);
                break;

            case LootableType.Currency:
                InitSimpleLoot(data, currencyColor);
                break;

            default:
                InitUnknown(data);
                break;
        }
    }

    private void InitGeneratedItem(InventoryItemData data)
    {
        ItemInstance item = JsonUtility.FromJson<ItemInstance>(data.itemJson);

        if (item == null)
        {
            Debug.LogError("[ItemPreview] Failed to parse ItemInstance.");
            return;
        }

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);

        if (itemBase == null)
        {
            Debug.LogError($"[ItemPreview] No ItemBase found for baseId={item.baseId}");
            return;
        }

        Color color = GetWantedColor(item.rarity);

        backGroundImage.color = color;

        itemName.text = string.IsNullOrEmpty(item.itemName) ? itemBase.BaseName : item.itemName;
        itemName.color = color;

        baseType.gameObject.SetActive(true);
        itemLevel.gameObject.SetActive(true);
        modsContainer.SetActive(true);
        descriptionText.gameObject.SetActive(false);

        baseType.text = itemBase.BaseName;
        itemLevel.text = $"Item Level {item.itemLevel}";

        AddAffixLines(item.prefixes, "Prefix");
        AddAffixLines(item.suffixes, "Suffix");
    }

    private void InitSimpleLoot(InventoryItemData data, Color color)
    {
        backGroundImage.color = color;

        itemName.text = data.displayNameOverride;
        itemName.color = color;

        baseType.gameObject.SetActive(false);
        itemLevel.gameObject.SetActive(false);
        modsContainer.SetActive(false);

        descriptionText.gameObject.SetActive(true);
        descriptionText.text = data.description;
    }

    private void InitUnknown(InventoryItemData data)
    {
        backGroundImage.color = Color.gray;

        itemName.text = string.IsNullOrEmpty(data.displayNameOverride)
            ? "Unknown Loot"
            : data.displayNameOverride;

        itemName.color = Color.white;

        baseType.gameObject.SetActive(false);
        itemLevel.gameObject.SetActive(false);
        modsContainer.SetActive(false);

        descriptionText.gameObject.SetActive(true);
        descriptionText.text = "No description available.";
    }

    private void ClearMods()
    {
        foreach (Transform child in modsContainer.transform)
            Destroy(child.gameObject);
    }

    private void AddAffixLines(System.Collections.Generic.List<ItemAffix> affixes, string label)
    {
        if (affixes == null)
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