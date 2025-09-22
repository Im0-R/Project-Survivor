using UnityEngine;

public class RewardSpellsCanvas : MonoBehaviour
{
    [SerializeField] private GameObject SpellChoicePrefab;
    [SerializeField] private GameObject LayoutGroup;

    public void Start()
    {
        // Example: Add 3 spell choices at start
        AddSpellChoices(3);
    }
    public void AddSpellChoices(int nbOfChoices)
    {
        for (int i = 0; i < nbOfChoices; i++)
        {
            GameObject tempChoice = Instantiate(SpellChoicePrefab, LayoutGroup.transform);
        }
    }
}
