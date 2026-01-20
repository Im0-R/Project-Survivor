using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootLabelUI : MonoBehaviour
{
    public TMP_Text label;
    public Button button;

    private LootPickup target;

    public void Bind(LootPickup loot)
    {
        target = loot;
        label.text = BuildLabel(loot);
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
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
        return $"Item {loot.GetItem().baseId}";
    }
}
