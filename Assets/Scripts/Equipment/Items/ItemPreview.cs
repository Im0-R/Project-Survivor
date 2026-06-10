using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPreview : MonoBehaviour
{
    [SerializeField] private Image backGroundImage;

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI baseType;
    [SerializeField] private TextMeshProUGUI itemLevel;

    [SerializeField] private GameObject modsContainer;
    [SerializeField] private GameObject modLineUI;

    [SerializeField] private Color normalColor;
    [SerializeField] private Color magicColor;
    [SerializeField] private Color rareColor;
    [SerializeField] private Color uniqueColor;

    public void Init(ItemInstance item)
    {
        if (item == null)
        {
            Debug.LogError("[ItemPreview] Init called with null item.");
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

        baseType.text = itemBase.BaseName;
        itemLevel.text = $"Item Level {item.itemLevel}";

        ClearMods();

        AddAffixLines(item.prefixes, "Prefix");
        AddAffixLines(item.suffixes, "Suffix");

        Debug.Log($"[ItemPreview] itemName={item.itemName}, rarity={item.rarity}, affixesCount={item.TotalAffixCount}");
    }

    private void ClearMods()
    {
        foreach (Transform child in modsContainer.transform)
        {
            Destroy(child.gameObject);
        }
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

    public Color GetWantedColor(ItemRarity itemRarity)
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