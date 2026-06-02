using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapDifficultyCanvas : MonoBehaviour
{
    public static MapDifficultyCanvas Instance { get; private set; }

    [SerializeField] private GameObject panel;

    [Header("Difficulty")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text multipliersText;

    [Header("Difficulty Settings")]
    [SerializeField] private DifficultyScalingSO difficultyScaling;

    private int selectedDifficulty = 1;
    private PortalInstances currentPortal;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        difficultySlider.minValue = 1;
        difficultySlider.maxValue = 10;
        difficultySlider.wholeNumbers = true;
        difficultySlider.value = 1;

        difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);

        OnDifficultyChanged(difficultySlider.value);
    }

    public void Open(PortalInstances portal)
    {
        currentPortal = portal;

        if (panel != null)
            panel.SetActive(true);

        OnDifficultyChanged(difficultySlider.value);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        currentPortal = null;
    }

    private void OnDifficultyChanged(float value)
    {
        selectedDifficulty = Mathf.RoundToInt(value);

        if (difficultyText != null)
            difficultyText.text = $"Difficulty {selectedDifficulty}";

        UpdateMultiplierText();
    }

    private void UpdateMultiplierText()
    {
        if (multipliersText == null)
            return;

        if (difficultyScaling == null)
        {
            multipliersText.text = "Missing DifficultyScalingSO";
            return;
        }

        int points = selectedDifficulty - 1;

        float hpMultiplier = 1f + points * difficultyScaling.healthPercentPerPoint / 100f;
        float damageMultiplier = 1f + points * difficultyScaling.damagePercentPerPoint / 100f;
        float moveSpeedMultiplier = 1f + points * difficultyScaling.moveSpeedPercentPerPoint / 100f;
        float xpMultiplier = 1f + points * difficultyScaling.experiencePercentPerPoint / 100f;

        float lootQuantityMultiplier = 1f + points * difficultyScaling.lootQuantityPercentPerPoint / 100f;
        float currencyQuantityMultiplier = 1f + points * difficultyScaling.currencyQuantityPercentPerPoint / 100f;
        float goldQuantityMultiplier = 1f + points * difficultyScaling.goldQuantityPercentPerPoint / 100f;

        multipliersText.text =
            $"Enemy HP: x{hpMultiplier:0.00}\n" +
            $"Enemy Damage: x{damageMultiplier:0.00}\n" +
            $"Move Speed: x{moveSpeedMultiplier:0.00}\n" +
            $"XP: x{xpMultiplier:0.00}\n\n" +
            $"Loot Quantity: x{lootQuantityMultiplier:0.00}\n" +
            $"Currency Quantity: x{currencyQuantityMultiplier:0.00}\n" +
            $"Gold Quantity: x{goldQuantityMultiplier:0.00}";
    }

    public void EnterMap()
    {
        if (currentPortal == null)
        {
            Debug.LogWarning("[MapDifficultyCanvas] No portal selected");
            return;
        }

        currentPortal.RequestMapWithDifficulty(selectedDifficulty);
        Close();
    }
}