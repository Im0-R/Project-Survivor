using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CanvasPartyListUI : MonoBehaviour
{
    public static CanvasPartyListUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject memberPrefab;

    [Header("Debug")]
    [SerializeField] private bool enableClientLogs = true;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    public void SetMembers(string[] members)
    {
        LogClient($"SetMembers called. Count={(members == null ? 0 : members.Length)}");

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        bool hasParty = members != null && members.Length > 0;

        if (panel != null)
            panel.SetActive(hasParty);

        if (!hasParty)
            return;

        foreach (string member in members)
        {
            GameObject row = Instantiate(memberPrefab, contentParent);

            TMP_Text text = row.GetComponentInChildren<TMP_Text>(true);

            Button teleportButton = null;

            Transform buttonTransform = row.transform.Find("ButtonTeleportation");

            if (buttonTransform != null)
                teleportButton = buttonTransform.GetComponent<Button>();

            if (text != null)
                text.text = member;

            string targetName = member;

            if (teleportButton != null)
            {
                teleportButton.onClick.RemoveAllListeners();

                teleportButton.onClick.AddListener(() =>
                {
                    LogClient($"Teleport button clicked for {targetName}");

                    if (NetworkClient.localPlayer == null)
                    {
                        LogClient("NetworkClient.localPlayer is null.");
                        return;
                    }

                    PlayerEntity localPlayer =
                        NetworkClient.localPlayer.GetComponent<PlayerEntity>();

                    if (localPlayer == null)
                    {
                        LogClient("Local player has no PlayerEntity.");
                        return;
                    }

                    LogClient($"Sending teleport request to {targetName}");

                    localPlayer.CmdTeleportToPartyMemberByName(targetName);
                });
            }
            else
            {
                LogClient($"ButtonTeleportation not found for {targetName}");
            }
        }
    }

    private void LogClient(string message)
    {
        if (!enableClientLogs)
            return;

        Debug.Log($"[Client][CanvasPartyListUI] {message}");
    }
}