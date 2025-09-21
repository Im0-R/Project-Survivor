using TMPro;
using UnityEngine;

public class ChangeLobbyCode : MonoBehaviour
{
    void Update()
    {
        if (GameLobbyManager.Instance != null)
        {
            if (GetComponent<TextMeshProUGUI>() != null)
            {
                GetComponent<TextMeshProUGUI>().text = GameLobbyManager.Instance.GetCode();
            }
        }
        else
        {
            Debug.LogError("GameLobbyManager Instance is null");
        }
    }
}
