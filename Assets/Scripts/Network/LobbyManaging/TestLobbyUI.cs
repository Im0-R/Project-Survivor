using UnityEngine;

public class TestLobbyUI : MonoBehaviour
{
    private string lobbyCode = "";

    private async void OnGUI()
    {
        if (GUILayout.Button("Créer un Lobby"))
        {
            lobbyCode = await GameLobbyManager.Instance.CreateGameAsync();
        }

        GUILayout.Label("Lobby Code: " + lobbyCode);

        lobbyCode = GUILayout.TextField(lobbyCode, 10);

        if (GUILayout.Button("Rejoindre un Lobby"))
        {
            _ = GameLobbyManager.Instance.JoinGameAsync(lobbyCode);
        }
    }
}
