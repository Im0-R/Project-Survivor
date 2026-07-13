using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPreview : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Image backGroundImage;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI baseType;
    [SerializeField] private TextMeshProUGUI itemLevel;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Base Stats")]
    [Tooltip("Container placé au-dessus de l'Item Level.")]
    [SerializeField] private GameObject baseStatsContainer;

    [Tooltip("Prefab d'une ligne de statistique de base.")]
    [SerializeField] private GameObject baseStatLineUI;

    [Header("Modifiers")]
    [Tooltip("Container placé sous l'Item Level.")]
    [SerializeField] private GameObject modsContainer;

    [SerializeField] private GameObject modLineUI;

    [Header("Modifier Appearance")]
    [SerializeField] private bool showModifierDetails = true;

    [SerializeField, Range(40, 100)]
    private int modifierHeaderSizePercent = 65;

    [SerializeField]
    private Color modifierHeaderColor =
        new Color(0.55f, 0.55f, 0.55f, 1f);

    public void Init(InventoryItemData data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemPreview] Init called with null data.");
            return;
        }

        ClearGeneratedLines();

        LootVisualStyle style = LootVisualManager.Instance != null
            ? LootVisualManager.Instance.Resolve(data)
            : LootVisualStyle.CreateFallback();

        ApplySharedStyle(style);

        if (data.lootableType == LootableType.GeneratedItem ||
            !string.IsNullOrWhiteSpace(data.itemJson))
        {
            InitGeneratedItem(data, style);
            return;
        }

        if (data.lootableType == LootableType.Sigil ||
            data.lootableType == LootableType.Currency)
        {
            InitSimpleLoot(data, style);
            return;
        }

        InitUnknown(data, style);
    }

    private void ApplySharedStyle(LootVisualStyle style)
    {
        if (backGroundImage != null)
            backGroundImage.color = style.previewBackgroundColor;

        if (itemName != null)
            itemName.color = style.previewNameTextColor;

        if (baseType != null)
            baseType.color = style.previewBodyTextColor;

        if (itemLevel != null)
            itemLevel.color = style.previewBodyTextColor;

        if (descriptionText != null)
            descriptionText.color = style.previewBodyTextColor;
    }

    private void InitGeneratedItem(
        InventoryItemData data,
        LootVisualStyle style)
    {
        ItemInstance item = JsonUtility.FromJson<ItemInstance>(data.itemJson);

        if (item == null)
        {
            Debug.LogError("[ItemPreview] Failed to parse ItemInstance.");
            InitUnknown(data, style);
            return;
        }

        item.EnsureLists();

        ItemBaseSO itemBase = ItemDatabase.GetBase(item.baseId);

        if (itemBase == null)
        {
            Debug.LogError(
                $"[ItemPreview] No ItemBase found for baseId={item.baseId}"
            );

            InitUnknown(data, style);
            return;
        }

        // =========================
        // Name
        // =========================

        if (itemName != null)
        {
            itemName.gameObject.SetActive(true);

            itemName.text = string.IsNullOrWhiteSpace(item.itemName)
                ? itemBase.BaseName
                : item.itemName;

            itemName.color = style.previewNameTextColor;
        }

        // =========================
        // Base type
        // =========================

        if (baseType != null)
        {
            baseType.gameObject.SetActive(true);
            baseType.text = itemBase.BaseName;
            baseType.color = style.previewBodyTextColor;
        }

        // =========================
        // Base stats
        // =========================

        int baseStatCount = AddBaseStatLines(
            itemBase,
            style.previewBodyTextColor
        );

        // =========================
        // Item level
        // =========================

        if (itemLevel != null)
        {
            itemLevel.gameObject.SetActive(true);
            itemLevel.text = $"ITEM LEVEL: {item.itemLevel}";
            itemLevel.color = style.previewBodyTextColor;
        }

        // =========================
        // Affixes
        // =========================

        int modifierCount = 0;

        modifierCount += AddAffixLines(
            item.prefixes,
            "PREFIX",
            style.previewModTextColor
        );

        modifierCount += AddAffixLines(
            item.suffixes,
            "SUFFIX",
            style.previewModTextColor
        );

        if (baseStatsContainer != null)
            baseStatsContainer.SetActive(baseStatCount > 0);

        if (modsContainer != null)
        {
            bool baseStatsUseModsContainer =
                baseStatsContainer == null && baseStatCount > 0;

            modsContainer.SetActive(
                modifierCount > 0 || baseStatsUseModsContainer
            );
        }

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);
    }

    private void InitSimpleLoot(
        InventoryItemData data,
        LootVisualStyle style)
    {
        LootableSO lootable = LootableDatabase.Get(data.lootableId);

        HideGeneratedItemElements();

        if (itemName != null)
        {
            string finalName = data.displayNameOverride;

            if (string.IsNullOrWhiteSpace(finalName) && lootable != null)
                finalName = lootable.DisplayName;

            if (string.IsNullOrWhiteSpace(finalName))
                finalName = $"Lootable {data.lootableId}";

            itemName.text = finalName;
            itemName.color = style.previewNameTextColor;
            itemName.gameObject.SetActive(true);
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.color = style.previewBodyTextColor;

            if (!string.IsNullOrWhiteSpace(data.description))
            {
                descriptionText.text = data.description;
            }
            else if (lootable != null)
            {
                descriptionText.text = lootable.DisplayName;
            }
            else
            {
                descriptionText.text = "No description available.";
            }
        }
    }

    private void InitUnknown(
        InventoryItemData data,
        LootVisualStyle style)
    {
        HideGeneratedItemElements();

        if (itemName != null)
        {
            itemName.gameObject.SetActive(true);

            itemName.text = string.IsNullOrWhiteSpace(
                data.displayNameOverride
            )
                ? "Unknown Loot"
                : data.displayNameOverride;

            itemName.color = style.previewNameTextColor;
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.color = style.previewBodyTextColor;

            descriptionText.text =
                string.IsNullOrWhiteSpace(data.description)
                    ? "No description available."
                    : data.description;
        }
    }

    private void HideGeneratedItemElements()
    {
        if (baseType != null)
            baseType.gameObject.SetActive(false);

        if (itemLevel != null)
            itemLevel.gameObject.SetActive(false);

        if (baseStatsContainer != null)
            baseStatsContainer.SetActive(false);

        if (modsContainer != null)
            modsContainer.SetActive(false);
    }

    // =====================================================
    // Base stats
    // =====================================================

    private int AddBaseStatLines(
        ItemBaseSO itemBase,
        Color textColor)
    {
        if (itemBase == null)
            return 0;

        int lineCount = 0;

        if (itemBase.BaseAttack != 0)
        {
            bool created = AddBaseStatLine(
                $"DAMAGE: {itemBase.BaseAttack}",
                textColor
            );

            if (created)
                lineCount++;
        }

        if (itemBase.BaseDefense != 0)
        {
            bool created = AddBaseStatLine(
                $"ARMOUR: {itemBase.BaseDefense}",
                textColor
            );

            if (created)
                lineCount++;
        }

        if (itemBase.BaseVitality != 0)
        {
            bool created = AddBaseStatLine(
                $"MAXIMUM LIFE: {itemBase.BaseVitality}",
                textColor
            );

            if (created)
                lineCount++;
        }

        return lineCount;
    }

    private bool AddBaseStatLine(
        string text,
        Color textColor)
    {
        Transform wantedParent = GetBaseStatsParent();
        GameObject wantedPrefab = GetBaseStatLinePrefab();

        return CreateTextLine(
            wantedParent,
            wantedPrefab,
            $"<b>{text}</b>",
            textColor
        );
    }

    private Transform GetBaseStatsParent()
    {
        if (baseStatsContainer != null)
            return baseStatsContainer.transform;

        // Fallback pour éviter une erreur si le nouveau container
        // n'est pas encore assigné.
        if (modsContainer != null)
            return modsContainer.transform;

        return null;
    }

    private GameObject GetBaseStatLinePrefab()
    {
        if (baseStatLineUI != null)
            return baseStatLineUI;

        // Le prefab des affixes peut temporairement servir de fallback.
        return modLineUI;
    }

    // =====================================================
    // Affixes
    // =====================================================

    private int AddAffixLines(
        List<ItemAffix> affixes,
        string modifierType,
        Color textColor)
    {
        if (affixes == null ||
            modsContainer == null ||
            modLineUI == null)
        {
            return 0;
        }

        int createdLines = 0;

        for (int i = 0; i < affixes.Count; i++)
        {
            ItemAffix affixInstance = affixes[i];

            AffixSO affix = AffixDatabase.Get(
                affixInstance.affixId
            );

            if (affix == null)
            {
                Debug.LogWarning(
                    $"[ItemPreview] Unknown affix id=" +
                    $"{affixInstance.affixId}"
                );

                continue;
            }

            string effectText = FormatAffix(
                affix.stat,
                affixInstance.value
            );

            string finalText = effectText;

            if (showModifierDetails)
            {
                string modifierHeader = BuildModifierHeader(
                    modifierType,
                    affix,
                    affixInstance
                );

                string headerColor =
                    ColorUtility.ToHtmlStringRGBA(
                        modifierHeaderColor
                    );

                finalText =
                    $"<size={modifierHeaderSizePercent}%>" +
                    $"<color=#{headerColor}>" +
                    $"{modifierHeader}" +
                    $"</color>" +
                    $"</size>\n" +
                    effectText;
            }

            bool created = CreateTextLine(
                modsContainer.transform,
                modLineUI,
                finalText,
                textColor
            );

            if (created)
                createdLines++;
        }

        return createdLines;
    }

    private string BuildModifierHeader(
        string modifierType,
        AffixSO affix,
        ItemAffix affixInstance)
    {
        string result = $"{modifierType} MODIFIER";

        if (!string.IsNullOrWhiteSpace(affix.affixName))
            result += $" \"{affix.affixName}\"";

        if (affixInstance.tier > 0)
            result += $" (TIER {affixInstance.tier})";

        return result;
    }

    private string FormatAffix(
        StatId stat,
        float value)
    {
        string signedValue = GetSignedValue(value);

        return stat switch
        {
            StatId.MaxHealth =>
                $"{signedValue} to Maximum Life",

            StatId.HealthRegen =>
                $"{signedValue} Life Regenerated per Second",

            StatId.MaxMana =>
                $"{signedValue} to Maximum Mana",

            StatId.ManaRegen =>
                $"{signedValue} Mana Regenerated per Second",

            StatId.Armor =>
                $"{signedValue} to Armour",

            StatId.Evasion =>
                $"{signedValue} to Evasion Rating",

            StatId.FireResistance =>
                $"{signedValue}% to Fire Resistance",

            StatId.ColdResistance =>
                $"{signedValue}% to Cold Resistance",

            StatId.LightningResistance =>
                $"{signedValue}% to Lightning Resistance",

            StatId.ChaosResistance =>
                $"{signedValue}% to Chaos Resistance",

            StatId.SpellDamage =>
                $"{signedValue}% to Spell Damage",

            StatId.FireDamage =>
                $"{signedValue} to Fire Damage",

            StatId.ColdDamage =>
                $"{signedValue} to Cold Damage",

            StatId.LightningDamage =>
                $"{signedValue} to Lightning Damage",

            StatId.ChaosDamage =>
                $"{signedValue} to Chaos Damage",

            StatId.CooldownReduction =>
                $"{signedValue}% Cooldown Reduction",

            StatId.CritChance =>
                $"{signedValue}% to Critical Strike Chance",

            StatId.CritDamage =>
                $"{signedValue}% to Critical Strike Damage",

            StatId.ProjectileSpeed =>
                $"{signedValue}% to Projectile Speed",

            StatId.DurationMult =>
                $"{signedValue}% to Duration",

            StatId.DamageMult =>
                $"{signedValue}% to Damage",

            StatId.MoveSpeedMult =>
                $"{signedValue}% to Movement Speed",

            _ =>
                $"{signedValue} to {stat}"
        };
    }

    private string GetSignedValue(float value)
    {
        string formattedValue;

        if (Mathf.Approximately(
                value,
                Mathf.Round(value)))
        {
            formattedValue =
                Mathf.Abs(Mathf.RoundToInt(value)).ToString();
        }
        else
        {
            formattedValue =
                Mathf.Abs(value).ToString("0.##");
        }

        return value >= 0f
            ? $"+{formattedValue}"
            : $"-{formattedValue}";
    }

    // =====================================================
    // UI creation and cleanup
    // =====================================================

    private bool CreateTextLine(
        Transform parent,
        GameObject prefab,
        string text,
        Color textColor)
    {
        if (parent == null || prefab == null)
            return false;

        GameObject line = Instantiate(prefab, parent);

        TextMeshProUGUI lineText =
            line.GetComponentInChildren<TextMeshProUGUI>(true);

        if (lineText == null)
        {
            Debug.LogError(
                "[ItemPreview] Line prefab has no " +
                "TextMeshProUGUI component."
            );

            Destroy(line);
            return false;
        }

        lineText.richText = true;
        lineText.color = textColor;
        lineText.text = text;

        return true;
    }

    private void ClearGeneratedLines()
    {
        ClearContainer(baseStatsContainer);

        if (modsContainer != baseStatsContainer)
            ClearContainer(modsContainer);
    }

    private void ClearContainer(GameObject container)
    {
        if (container == null)
            return;

        foreach (Transform child in container.transform)
            Destroy(child.gameObject);
    }
}