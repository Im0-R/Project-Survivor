using Mirror;
using UnityEngine;

public class CustomNetworkGUI : MonoBehaviour
{
    private NetworkManager manager;
    private string networkAddress = "72.60.212.58"; // IP par défaut

    // Positionnement de la fenêtre
    private Rect windowRect = new Rect(10, 10, 250, 220);

    void Awake()
    {
        manager = GetComponent<NetworkManager>();
        if (manager == null)
        {
            Debug.LogError("[CustomNetworkGUI] Aucun NetworkManager trouvé sur cet objet !");
        }
    }

    void OnGUI()
    {
        // Style optionnel
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        windowRect = GUI.Window(0, windowRect, DrawWindow, "Network Control");
    }

    void DrawWindow(int windowID)
    {
        GUILayout.Space(10);
        GUILayout.Label("Status : " + GetStatus(), GUI.skin.label);
        GUILayout.Space(10);

        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            GUILayout.Label("Adresse :", GUI.skin.label);
            networkAddress = GUILayout.TextField(networkAddress);

            if (GUILayout.Button("▶ Lancer Host"))
            {
                manager.StartHost();
                UIManager.Instance.ShowGameUI();
            }

            if (GUILayout.Button("🌐 Lancer Serveur"))
            {
                manager.StartServer();
                UIManager.Instance.ShowGameUI();
            }
            if (GUILayout.Button("🔌 Rejoindre Client"))
            {
                manager.networkAddress = networkAddress;
                manager.StartClient();
                UIManager.Instance.ShowGameUI();
            }
        }
        else
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                GUILayout.Label("Mode : Host");
            }
            else if (NetworkServer.active)
            {
                GUILayout.Label("Mode : Serveur");
            }
            else if (NetworkClient.isConnected)
            {
                GUILayout.Label("Mode : Client");
            }

            if (GUILayout.Button("⏹ Stop"))
            {
                manager.StopHost();
                manager.StopServer();
                manager.StopClient();
            }
        }
        // Déplacer la fenêtre
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    string GetStatus()
    {
        if (NetworkServer.active && NetworkClient.isConnected) return "Host actif ✅";
        if (NetworkServer.active) return "Serveur actif ✅";
        if (NetworkClient.isConnected) return "Client connecté ✅";
        return "Hors ligne ⛔";
    }
}