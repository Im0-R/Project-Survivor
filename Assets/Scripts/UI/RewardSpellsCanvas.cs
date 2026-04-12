using TMPro;
using UnityEngine;

public class RewardSpellsCanvas : MonoBehaviour
{
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private TextMeshProUGUI titleText;

    public void Init(string[] spellNames, int level, GameObject spellChoicePrefab)
    {
        if (titleText != null)
            titleText.text = $"Level {level} reached, choose a reward";

        if (choicesContainer == null)
        {
            Debug.LogError("[RewardSpellsCanvas] choicesContainer is not assigned.");
            return;
        }

        if (spellChoicePrefab == null)
        {
            Debug.LogError("[RewardSpellsCanvas] spellChoicePrefab is not assigned.");
            return;
        }

        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        if (spellNames == null || spellNames.Length == 0)
        {
            Debug.LogWarning("[RewardSpellsCanvas] No spell names received.");
            return;
        }

        foreach (string spellName in spellNames)
        {
            GameObject obj = Instantiate(spellChoicePrefab, choicesContainer);
            SpellChoice choice = obj.GetComponent<SpellChoice>();

            if (choice == null)
            {
                Debug.LogError("[RewardSpellsCanvas] SpellChoice component missing on prefab.");
                continue;
            }

            choice.Init(spellName);
        }
    }
}