using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientAccountAPI : MonoBehaviour
{
    public static ClientAccountAPI Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // ✅ écoute les réponses du serveur
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponse);
    }

    // ==========================================================
    // =============== LOGIN / REGISTER ==========================
    // ==========================================================

    public void TryRegister(string username, string password)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        Debug.Log($"[ClientAccountAPI] Sending RegisterMessage for {username}");
        NetworkClient.Send(new RegisterMessage { username = username, password = password });
    }

    public void TryLogin(string username, string password)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        Debug.Log($"[ClientAccountAPI] Sending LoginMessage for {username}");
        NetworkClient.Send(new LoginMessage { username = username, password = password });
    }

    // ==========================================================
    // =============== RESPONSE HANDLER ==========================
    // ==========================================================

    private void OnAuthResponse(AuthResponseMessage msg)
    {
        Debug.Log($"[ClientAccountAPI] AuthResponse → success={msg.success}, message={msg.message}");

        if (msg.success)
        {
            Debug.Log("[ClientAccountAPI] Login/Register successful — waiting for Mirror to load online scene...");

            // Optionnel : forcer un retour visuel ou UI

            // Mirror va charger automatiquement "Town" une fois AddPlayerForConnection() exécuté
            // Si tu veux forcer manuellement la scène :
            SceneLoader.LoadTownScene();
        }
        else
        {
            Debug.LogWarning($"[ClientAccountAPI] Login/Register failed: {msg.message}");
        }
    }
}
