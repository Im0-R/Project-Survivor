using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LootLabelUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text label;
    public Button button;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    private LootPickup target;

    public void Bind(LootPickup loot)
    {
        if (target != null)
            target.OnVisualChanged -= RefreshLabel;

        target = loot;

        if (target != null)
            target.OnVisualChanged += RefreshLabel;

        RefreshLabel();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnVisualChanged -= RefreshLabel;

        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {
        if (target == null || LootUIManager.Instance == null)
            return;

        LootUIManager.Instance.RequestPickup(target);
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Camera.main == null)
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
        transform.position = screenPos + Vector3.up * 20f;
    }

    public void RefreshLabel()
    {
        if (target == null)
            return;

        InventoryItemData data = target.GetInventoryItemData();

        LootVisualStyle style = LootVisualManager.Instance != null
            ? LootVisualManager.Instance.Resolve(data)
            : LootVisualStyle.CreateFallback();

        gameObject.SetActive(style.visible);

        if (!style.visible)
            return;

        transform.localScale = Vector3.one * style.lootLabelScale;

        if (label != null)
        {
            label.text = target.GetDisplayName();
            label.color = style.lootLabelTextColor;
        }

        if (backgroundImage != null)
            backgroundImage.color = style.lootLabelBackgroundColor;

        if (iconImage != null)
        {
            Sprite icon = target.GetIcon();
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target == null || ItemPreviewManager.Instance == null)
            return;

        InventoryItemData data = target.GetInventoryItemData();

        if (data == null)
            return;

        ItemPreviewManager.Instance.InitPreview(data, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemPreviewManager.Instance != null)
            ItemPreviewManager.Instance.ClosePreview();
    }
}
