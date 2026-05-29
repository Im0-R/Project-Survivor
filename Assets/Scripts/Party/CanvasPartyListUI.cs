using TMPro;
using UnityEngine;

public class CanvasPartyListUI : MonoBehaviour
{
    public static CanvasPartyListUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private TextMeshProUGUI memberTextPrefab;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    public void SetMembers(string[] members)
    {
        if (contentParent == null || memberTextPrefab == null)
        {
            Debug.LogError("[PartyUI] Missing contentParent or memberTextPrefab");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        bool hasParty = members != null && members.Length > 0;

        if (panel != null)
            panel.SetActive(hasParty);

        if (!hasParty)
            return;

        foreach (string member in members)
        {
            TextMeshProUGUI row = Instantiate(memberTextPrefab, contentParent);
            row.gameObject.SetActive(true);
            row.text = member;
        }
    }
}