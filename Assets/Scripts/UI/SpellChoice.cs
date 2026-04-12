#if !UNITY_SERVER
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellChoice : MonoBehaviour
{
    private string spellName;

    [Header("UI")]
    [SerializeField] private Button buttonHitbox;
    [SerializeField] private TextMeshProUGUI spellNameText;
    [SerializeField] private TextMeshProUGUI spellLevelText;
    [SerializeField] private TextMeshProUGUI spellDescriptionText;
    [SerializeField] private Image spellIconImage;

    public void Init(string spellNameFromServer)
    {
        spellName = spellNameFromServer;

        Spell spell = SpellsManager.Instance.GetSpell(spellName);
        if (spell == null)
        {
            Debug.LogError($"[SpellChoice] Spell '{spellName}' introuvable dans SpellsManager.");
            return;
        }

        var data = spell.GetData();

        if (spellNameText != null)
            spellNameText.text = data.spellName;

        if (spellDescriptionText != null)
            spellDescriptionText.text = data.description;

        if (spellIconImage != null)
            spellIconImage.sprite = data.UISprite;

        var playerEnt = PlayerUI.Instance?.playerEnt;
        Spell ownedSpell = playerEnt != null ? playerEnt.GetSpellByName(data.spellName) : null;

        if (spellLevelText != null)
        {
            if (ownedSpell == null)
            {
                spellLevelText.text = "New Spell";
            }
            else if (ownedSpell.IsMaxLevel())
            {
                spellLevelText.text = "Max Level";
            }
            else
            {
                int currentLevel = ownedSpell.GetData().currentLevel;
                spellLevelText.text = $"Level {currentLevel} -> {currentLevel + 1}";
            }
        }

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

        playerEnt.CmdChooseSpellReward(spellName);
    }
}
#endif