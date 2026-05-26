using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellChoice : MonoBehaviour
{
    private string rewardCode;

    [Header("UI")]
    [SerializeField] private Button buttonHitbox;
    [SerializeField] private TextMeshProUGUI spellNameText;
    [SerializeField] private TextMeshProUGUI spellLevelText;
    [SerializeField] private TextMeshProUGUI spellDescriptionText;
    [SerializeField] private Image spellIconImage;

    public void Init(string rewardCodeFromServer)
    {
        rewardCode = rewardCodeFromServer;

        if (spellNameText != null)
            spellNameText.text = RunRewardUtility.GetRewardTitle(rewardCode);

        if (spellDescriptionText != null)
            spellDescriptionText.text = RunRewardUtility.GetRewardDescription(rewardCode);

        if (spellLevelText != null)
            spellLevelText.text = "Run Upgrade";

        SetupRewardIcon(rewardCode);

        if (buttonHitbox != null)
        {
            buttonHitbox.onClick.RemoveAllListeners();
            buttonHitbox.onClick.AddListener(OnChoosed);
        }
    }

    private void SetupRewardIcon(string rewardCode)
    {
        if (spellIconImage == null)
            return;

        spellIconImage.enabled = false;
        spellIconImage.sprite = null;

        string[] parts = rewardCode.Split('|');

        if (parts.Length == 0)
            return;

        // =========================================
        // SPELL UPGRADE
        // =========================================
        if (parts[0] == "SPELL_UPGRADE" && parts.Length >= 4)
        {
            string spellName = parts[1];

            Spell spell = SpellsManager.Instance.GetSpell(spellName);

            if (spell == null || spell.GetData() == null)
            {
                Debug.LogWarning($"[SpellChoice] Spell not found for icon: {spellName}");
                return;
            }

            Sprite icon = spell.GetData().UISprite;

            if (icon != null)
            {
                spellIconImage.sprite = icon;
                spellIconImage.enabled = true;
            }

            return;
        }

        // =========================================
        // STAT REWARD
        // =========================================
        if (parts[0] == "STAT")
        {
            // Tu peux mettre une icône générique ici plus tard
            spellIconImage.enabled = false;
        }
    }

    public void OnChoosed()
    {
        var playerEnt = PlayerUI.Instance?.playerEnt;

        if (playerEnt == null)
        {
            Debug.LogWarning("[SpellChoice] playerEnt introuvable au moment du choix.");
            return;
        }

        playerEnt.CmdChooseSpellReward(rewardCode);
    }
}