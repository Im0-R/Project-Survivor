using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TMP_Text cooldownText;

    private float cooldownDuration;
    private float cooldownRemaining;

    public void SetSpell(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        SetCooldown(0f, 0f);
    }

    public void SetCooldown(float remaining, float duration)
    {
        cooldownRemaining = Mathf.Max(0f, remaining);
        cooldownDuration = Mathf.Max(0.01f, duration);

        RefreshCooldownUI();
    }

    private void Update()
    {
        if (cooldownRemaining <= 0f)
            return;

        cooldownRemaining -= Time.deltaTime;

        if (cooldownRemaining < 0f)
            cooldownRemaining = 0f;

        RefreshCooldownUI();
    }

    private void RefreshCooldownUI()
    {
        float ratio = cooldownRemaining / cooldownDuration;

        if (cooldownFillImage != null)
        {
            cooldownFillImage.enabled = cooldownRemaining > 0f;
            cooldownFillImage.fillAmount = ratio;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(cooldownRemaining > 0f);
            cooldownText.text = cooldownRemaining > 0f
                ? cooldownRemaining.ToString("0.0")
                : "";
        }

        if (backgroundImage != null)
            backgroundImage.color = cooldownRemaining > 0f
                ? new Color(0.35f, 0.35f, 0.35f, 1f)
                : Color.white;
    }
}