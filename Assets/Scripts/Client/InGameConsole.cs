using UnityEngine;

public class InGameConsole : MonoBehaviour
{
    private string logs = "";
    private Vector2 scroll;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string log, string stackTrace, LogType type)
    {
        logs += log + "\n";
    }

    void OnGUI()
    {
        GUI.backgroundColor = Color.black;
        GUI.contentColor = Color.white;
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(600), GUILayout.Height(400));
        GUILayout.Label(logs);
        GUILayout.EndScrollView();
    }
}
