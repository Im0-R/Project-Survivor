using UnityEngine;
using UnityEngine.UI;

public class SpellChoice : MonoBehaviour
{
    Spell spellLinked; // The spell associated with this choice
    [SerializeField] TMPro.TextMeshProUGUI spellNameText;
    [SerializeField] TMPro.TextMeshProUGUI spellLevelText;
    [SerializeField] TMPro.TextMeshProUGUI spellDescriptionText;
    [SerializeField] Image spellIconImage;
    public bool isNewSpell = false;
    public SpellChoice()
    {
    }
    public void Start()
    {
        //random spell (50 % for new spell, 50 % for upgrade) but if no spell new spell

        if (Random.value < 0.5f || PlayerUI.Instance.playerEnt.GetAllActiveSpells().Count == 0)
        {
            if (SpellsManager.Instance.GetRandomSpellNotOwned() != null)
            {
                isNewSpell = true;
                spellLinked = SpellsManager.Instance.GetRandomSpellNotOwned();
                Debug.Log("Got new spell: " + spellLinked.GetData().spellName);
            }
            else
            {
                if (PlayerUI.Instance.playerEnt.GetAllActiveSpells().Count != 0)
                {
                    spellLinked = PlayerUI.Instance.playerEnt.GetRandomSpellFromActivesSpells();
                }
            }
        }
        else if (Random.value >= 0.5f || SpellsManager.Instance.GetRandomSpellNotOwned() == null)
        {
            spellLinked = PlayerUI.Instance.playerEnt.GetRandomSpellFromActivesSpells();
            Debug.Log("Got upgrade spell: " + spellLinked.GetData().spellName);
        }

        if (spellLinked == null)
        {
            isNewSpell = true;
            spellLinked = SpellsManager.Instance.GetRandomSpellNotOwned();
        }

        spellNameText.text = spellLinked.GetData().spellName;
        spellDescriptionText.text = spellLinked.GetData().description;
        spellIconImage.sprite = spellLinked.GetData().UISprite;



        if (isNewSpell)
        {
            spellLevelText.text = "New Spell";
        }
        else
        {
            if (spellLinked.IsMaxLevel())
            {
                spellLevelText.text = "Max Level";
            }
            else
            {
                spellLevelText.text = "Level " + spellLinked.GetData().currentLevel + " -> " + (spellLinked.GetData().currentLevel + 1);
            }
        }
    }
    public void OnChoosed()
    {
        if (isNewSpell)
        {
            PlayerUI.Instance.playerEnt.AddSpell(spellLinked);
        }
        else
        {
            if (!spellLinked.IsMaxLevel()) //Can only upgrade if not max level
            {
                UpgradeSpell();
            }
        }






        UIManager.Instance.HideSpellsRewardUI();
    }
    public void UpgradeSpell()
    {
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.playerEnt.UpgradeSpell(spellLinked.GetData().spellName);//Find the player and upgrade the spell
        }
    }
}
