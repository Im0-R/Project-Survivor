using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootLabelUI : MonoBehaviour
{
    public TMP_Text label;
    public Button button;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    private LootPickup target;

    public void Bind(LootPickup loot)
    {
        target = loot;

        RefreshLabel();

        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {
        if (target == null)
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

        RefreshLabel();

        if (Camera.main == null)
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
        transform.position = screenPos + Vector3.up * 20f;
    }

    private void RefreshLabel()
    {
        if (target == null)
            return;

        if (label != null)
            label.text = target.GetDisplayName();

        if (backgroundImage != null)
            backgroundImage.color = target.GetLabelColor();

        if (iconImage != null)
        {
            Sprite icon = target.GetIcon();
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }
}