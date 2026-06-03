using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPreview : MonoBehaviour
{
    [SerializeField] private Image backGroundImage;

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI baseType;
    [SerializeField] private TextMeshProUGUI itemLevelRequired;

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

        itemName.text = item.itemName;
        itemName.color = color;

        baseType.text = itemBase.BaseName;
        itemLevelRequired.text = $"Level {itemBase.ItemLevelRequirement}";

        foreach (Transform child in modsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        if (item.affixes != null)
        {
            for (int i = 0; i < item.affixes.Length; i++)
            {
                GameObject modLine = Instantiate(modLineUI, modsContainer.transform);
                TextMeshProUGUI modText = modLine.GetComponentInChildren<TextMeshProUGUI>();

                if (modText == null)
                {
                    Debug.LogError("[ItemPreview] modLineUI prefab has no TextMeshProUGUI.");
                    continue;
                }

                AffixSO affix = AffixDatabase.Get(item.affixes[i].affixId);

                if (affix == null)
                {
                    modText.text = "Unknown Mod";
                    continue;
                }

                modText.text = $"{item.affixes[i].value} to {affix.stat}";
            }
        }

        Debug.Log($"[ItemPreview] itemName={item.itemName}, rarity={item.rarity}, affixesCount={(item.affixes != null ? item.affixes.Length : -1)}");
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