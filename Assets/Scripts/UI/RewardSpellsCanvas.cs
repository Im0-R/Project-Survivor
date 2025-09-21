using UnityEngine;

public class RewardSpellsCanvas : MonoBehaviour
{
    [SerializeField] private GameObject SpellChoicePrefab;
    [SerializeField] private GameObject LayoutGroup;

    // Update is called once per frame
    void Update()
    {

    }

    public void AddSpellChoices(int nbOfChoices)
    {
        for (int i = 0; i < nbOfChoices; i++)
        {

            GameObject tempChoice = Instantiate(SpellChoicePrefab, LayoutGroup.transform);
            SpellChoice spellChoice = tempChoice.GetComponent<SpellChoice>();
        }
    }
}
