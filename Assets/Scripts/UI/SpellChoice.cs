using Unity.VisualScripting;
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
    public float fadeSpeed = 2.0f;
    private bool hasFaded = false;
    public SpellChoice()
    {
    }
    public void Start()
    {
        //do a little animation where the spell icon scales up from 0 to 1
        gameObject.transform.localScale = Vector3.zero;

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
    public void Update()
    {
        AnimationSpawnCard();
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
    public void AnimationSpawnCard()
    {
        //do a little animation where the spell icon scales up from 0 to 1 , lerp over 0.5 second with ease out back
        if (gameObject.transform.localScale != Vector3.one && !hasFaded)
        {
            gameObject.transform.localScale += fadeSpeed * Time.fixedDeltaTime * Vector3.one;
            if (gameObject.transform.localScale.x >= 1.0f)
            {
                gameObject.transform.localScale = Vector3.one;
                hasFaded = true;
            }
        }
    }
}
