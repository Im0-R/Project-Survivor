using UnityEngine;

public class TestLobbyUI : MonoBehaviour
{
    private string lobbyCode = "";

    private async void OnGUI()
    {
        float areaWidth = 300;
        float areaHeight = Screen.height; // toute la hauteur dispo
        float x = (Screen.width - areaWidth) / 2;

        GUILayout.BeginArea(new Rect(x, 0, areaWidth, areaHeight));

        GUILayout.FlexibleSpace(); // pousse vers le centre verticalement

        GUIStyle bigButton = new GUIStyle(GUI.skin.button);
        bigButton.fontSize = 24;

        if (GUILayout.Button("Créer un Lobby", bigButton, GUILayout.Height(60)))
        {
            lobbyCode = await GameLobbyManager.Instance.CreateGameAsync();

            //Hide the GUI after creating a lobby
            this.enabled = false;
        }

        GUILayout.Space(20);

        GUILayout.Label("Lobby Code: " + lobbyCode);

        lobbyCode = GUILayout.TextField(lobbyCode, 10, GUILayout.Height(40));

        GUILayout.Space(20);

        if (GUILayout.Button("Rejoindre un Lobby", bigButton, GUILayout.Height(60)))
        {
            await GameLobbyManager.Instance.JoinGameAsync(lobbyCode);

            //Hide the GUI after joining a lobby
            this.enabled = false;
        }

        GUILayout.FlexibleSpace(); // équilibre en bas

        GUILayout.EndArea();

    }
}
