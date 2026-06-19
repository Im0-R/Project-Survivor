using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeOfferLineUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button removeButton;

    public void Init(TradeOfferView view, bool isSelfOffer, Action onRemoveClicked)
    {
        if (nameText != null)
            nameText.text = view.displayName;

        if (amountText != null)
            amountText.text = view.amount > 1 ? $"x{view.amount}" : "";

        if (iconImage != null)
        {
            Sprite icon = ResolveIcon(view);

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(isSelfOffer);
            removeButton.onClick.RemoveAllListeners();

            if (isSelfOffer && onRemoveClicked != null)
                removeButton.onClick.AddListener(() => onRemoveClicked.Invoke());
        }
    }

    private Sprite ResolveIcon(TradeOfferView view)
    {
        LootableSO lootable = LootableDatabase.Get(view.lootableId);

        if (lootable != null && lootable.Icon != null)
            return lootable.Icon;

        if (!string.IsNullOrWhiteSpace(view.itemJson))
        {
            try
            {
                ItemInstance item = JsonUtility.FromJson<ItemInstance>(view.itemJson);

                if (item != null)
                {
                    ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);

                    if (itemBase != null)
                        return itemBase.Icon;
                }
            }
            catch
            {
                // ignored
            }
        }

        return null;
    }
}