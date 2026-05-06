using TMPro;
using UnityEngine;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;

    public void Set(string label, float value)
    {
        labelText.text = label;
        valueText.text = FormatValue(label, value);
    }

    private string FormatValue(string label, float value)
    {
        // Format spécial selon le type de stat
        if (label.Contains("Resistance"))
            return $"{value:0}%";

        if (label.Contains("Mult") || label.Contains("Damage"))
            return $"{value:0.##}";

        return value % 1 == 0
            ? value.ToString("0")
            : value.ToString("0.##");
    }
}