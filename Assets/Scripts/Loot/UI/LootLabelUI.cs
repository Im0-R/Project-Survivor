using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootLabelUI : MonoBehaviour
{
    public TMP_Text label;
    public Button button;

    [SerializeField] private Image backgroundImage;
    private LootPickup target;

    public void Bind(LootPickup loot)
    {
        target = loot;
        ItemInstance item = loot.GetItem();

        label.text = BuildLabel(loot);

        if (backgroundImage != null && item != null)
        {
            backgroundImage.color = GetRarityColor(item.rarity);
        }
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        LootUIManager.Instance.RequestPickup(target);
    }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
        transform.position = screenPos + Vector3.up * 20f;
    }

    string BuildLabel(LootPickup loot)
    {
        return $"{loot.GetItem().itemName}";
    }
    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Normal:
                return new Color(0.5f, 0.5f, 0.5f);

            case ItemRarity.Magic:
                return new Color(0.3f, 0.5f, 1f);

            case ItemRarity.Rare:
                return new Color(1f, 0.85f, 0.2f);

            case ItemRarity.Unique:
                return new Color(1f, 0.5f, 0.1f);
        }

        return Color.white;
    }
}
