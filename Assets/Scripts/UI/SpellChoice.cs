using UnityEngine;

public class SpellChoice : MonoBehaviour
{
    Spell spell; // The spell associated with this choice
    [SerializeField] TMPro.TextMeshProUGUI spellNameText;
    [SerializeField] TMPro.TextMeshProUGUI spellLevelText;

    public bool isNewSpell = false;
    public SpellChoice()
    {
        //random spell (50 % for new spell, 50 % for upgrade)
    }
    public void Start()
    {
        if (Random.value > 0.5f || PlayerUI.Instance.playerEnt.GetAllActiveSpells().Count == 0) //if no active spells, force new spell
        {
            spell = SpellsManager.Instance.GetRandomSpell();
        }
        else if (Random.value >= 0.5f)
        {
            spell = PlayerUI.Instance.playerEnt.GetRandomSpellFromActivesSpells();
        }
        spellNameText.text = spell.GetData().spellName;

        if (isNewSpell)
        {
            spellLevelText.text = "New Spell";
        }
        else
        {
            if (spell.GetData().currentLevel >= spell.GetData().maxLevel)
            {
                spellLevelText.text = "Max Level";
            }
            else
            {
                spellLevelText.text = "Level " + spell.GetData().currentLevel + " -> " + (spell.GetData().currentLevel + 1);
            }
        }
    }
    public void OnChoosed()
    {
        if (isNewSpell)
        {
            PlayerUI.Instance.playerEnt.AddSpell(spell);
        }
        else
        {
            UpgradeSpell();
        }
        UIManager.Instance.HideSpellsRewardUI();
    }
    public void UpgradeSpell()
    {
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.playerEnt.UpgradeSpell(spell.GetData().spellName);//Find the player and upgrade the spell
        }
    }
}
