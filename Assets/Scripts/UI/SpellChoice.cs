#if !UNITY_SERVER
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellChoice : MonoBehaviour
{
    private Spell spellLinked;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI spellNameText;
    [SerializeField] private TextMeshProUGUI spellLevelText;
    [SerializeField] private TextMeshProUGUI spellDescriptionText;
    [SerializeField] private Image spellIconImage;

    [Header("FX")]
    public bool isNewSpell = false;
    public float fadeSpeed = 2.0f;
    private bool hasFaded = false;

    private void Start()
    {
        transform.localScale = Vector3.zero;
        ChooseSpell();
        PopulateUI();
    }

    private void Update()
    {
        AnimationSpawnCard();
    }

    private void ChooseSpell()
    {
        var playerEnt = PlayerUI.Instance?.playerEnt;

        if (playerEnt == null)
        {
            Debug.LogWarning("[SpellChoice] playerEnt introuvable, fallback GetRandomSpell()");
            spellLinked = SpellsManager.Instance.GetRandomSpell();
            isNewSpell  = true;
            return;
        }

        int activeCount = playerEnt.GetAllActiveSpells().Count;
        bool hasAny = activeCount > 0;
        bool wantNew = (Random.value < 0.5f) || !hasAny;

        if (wantNew)
        {
            spellLinked = SpellsManager.Instance.GetRandomSpellClient();
            isNewSpell  = (spellLinked != null);

            if (spellLinked == null && hasAny)
            {
                spellLinked = playerEnt.GetRandomSpellFromActivesSpells();
                isNewSpell  = false;
            }
        }
        else
        {
            spellLinked = playerEnt.GetRandomSpellFromActivesSpells();
            isNewSpell  = false;

            if (spellLinked == null)
            {
                spellLinked = SpellsManager.Instance.GetRandomSpellClient();
                isNewSpell  = (spellLinked != null);
            }
        }

        if (spellLinked == null)
        {
            Debug.LogWarning("[SpellChoice] Aucun sort dispo, fallback GetRandomSpell()");
            spellLinked = SpellsManager.Instance.GetRandomSpell();
            isNewSpell  = true;
        }
    }

    private void PopulateUI()
    {
        if (spellLinked == null)
        {
            spellNameText.text = "No Spell";
            spellDescriptionText.text = "Aucun sort disponible.";
            if (spellIconImage) spellIconImage.sprite = null;
            spellLevelText.text = "";
            return;
        }

        var data = spellLinked.GetData();
        spellNameText.text = data.spellName;
        spellDescriptionText.text = data.description;
        if (spellIconImage) spellIconImage.sprite = data.UISprite;

        if (isNewSpell)
        {
            spellLevelText.text = "New Spell";
        }
        else
        {
            spellLevelText.text = spellLinked.IsMaxLevel()
                ? "Max Level"
                : $"Level {data.currentLevel} -> {data.currentLevel + 1}";
        }
    }

    public void OnChoosed()
    {
        if (spellLinked == null)
        {
            UIManager.Instance.HideSpellsRewardUI();
            return;
        }

        var playerEnt = PlayerUI.Instance?.playerEnt;
        if (playerEnt == null)
        {
            Debug.LogWarning("[SpellChoice] playerEnt introuvable au moment du choix.");
            UIManager.Instance.HideSpellsRewardUI();
            return;
        }

        if (isNewSpell)
        {
            playerEnt.CmdAddSpell(spellLinked.GetData().spellName);
        }
        else if (!spellLinked.IsMaxLevel())
        {
            playerEnt.UpgradeSpell(spellLinked.GetData().spellName);
        }

        UIManager.Instance.HideSpellsRewardUI();
    }

    private void AnimationSpawnCard()
    {
        if (!hasFaded)
        {
            float step = fadeSpeed * Time.deltaTime;
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.one, step);
            if (transform.localScale == Vector3.one) hasFaded = true;
        }
    }
}
#endif
