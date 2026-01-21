using TMPro;
using UnityEngine;

public class ItemTooltipPreview : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI baseType;

    [SerializeField] private GameObject modsContainer;

    [SerializeField] private TextMeshProUGUI itemLevelRequired;

    [SerializeField] private GameObject modLineUI;
    public void Init(ItemInstance item)
    {
        itemName.text = item.itemName;
        baseType.text = ItemDatabase.GetBase(item.baseId).baseName;
        itemLevelRequired.text = $"Level {ItemDatabase.GetBase(item.baseId).itemLevelRequirement}";

        for (int i = 0; i < item.affixes.Length; i++)
        {
            GameObject modLine = Instantiate(modLineUI, modsContainer.transform);
            TextMeshProUGUI modText = modLine.GetComponent<TextMeshProUGUI>();
            AffixSO affix = AffixDatabase.Get(item.affixes[i].affixId);

            modText.text = $"Grant + {item.affixes[i].value} to {affix.stat.ToString()}";
        }
    }
}
