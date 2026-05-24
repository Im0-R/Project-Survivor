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

        if (spellIconImage != null)
            spellIconImage.enabled = false;

        if (buttonHitbox != null)
        {
            buttonHitbox.onClick.RemoveAllListeners();
            buttonHitbox.onClick.AddListener(OnChoosed);
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