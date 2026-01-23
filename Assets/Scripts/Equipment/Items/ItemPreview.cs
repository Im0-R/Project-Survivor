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


    //Color for each rarity?

    [SerializeField] private Color normalColor;
    [SerializeField] private Color magicColor;
    [SerializeField] private Color rareColor;
    [SerializeField] private Color uniqueColor;
    public void Init(ItemInstance item)
    {
        // Set color based on rarity
        Color color = GetWantedColor(item.rarity);

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);

        backGroundImage.color = color;

        itemName.text = item.itemName;
        itemName.color = color;


        baseType.text = itemBase.baseName;

        itemLevelRequired.text = $"Level {itemBase.itemLevelRequirement}";

        for (int i = 0; i < item.affixes.Length; i++)
        {
            GameObject modLine = Instantiate(modLineUI, modsContainer.transform);
            TextMeshProUGUI modText = modLine.GetComponent<TextMeshProUGUI>();
            AffixSO affix = AffixDatabase.Get(item.affixes[i].affixId);

            modText.text = $"Grant + {item.affixes[i].value} to {affix.stat.ToString()}";
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
