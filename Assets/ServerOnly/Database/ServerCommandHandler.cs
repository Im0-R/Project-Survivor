#if !UNITY_CLIENT
using Mirror;
using UnityEngine;
using AuthMessages;

public class ServerCommandHandler : MonoBehaviour
{
    public static ServerCommandHandler Instance { get; private set; }

    // L’adresse IP de ton serveur HUB (ou localhost si même machine)
    private const string HUB_IP = "72.60.212.58";
    private const int HUB_PORT = 8000;     // PORT FIXE DE TON HUB INSTANCE

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        DatabaseManager.Initialize();
        Debug.Log("[ServerCommandHandler] Initialized and DB ready.");
        RegisterNetworkHandlers();
    }

    private void RegisterNetworkHandlers()
    {
        NetworkServer.RegisterHandler<RegisterMessage>(OnRegisterMessageReceived);
        NetworkServer.RegisterHandler<LoginMessage>(OnLoginMessageReceived);
        Debug.Log("[ServerCommandHandler] NetworkMessage handlers registered.");
    }

    // ==========================================================
    // =============== REGISTER =================================
    // ==========================================================

    private void OnRegisterMessageReceived(NetworkConnectionToClient conn, RegisterMessage msg)
    {
        Debug.Log($"[Server] Register request: {msg.username}");

        try
        {
            if (DatabaseManager.GetUser(msg.username) != null)
            {
                SendAuthResponse(conn, false, "Username already taken.");
                return;
            }

            DatabaseManager.InsertUser(msg.username, msg.password);
            conn.authenticationData = msg.username;

            Debug.Log($"[Server] User '{msg.username}' registered successfully.");
            SendAuthResponse(conn, true, "Account created successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Register error: {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // ==========================================================
    // =============== LOGIN ====================================
    // ==========================================================

    private void OnLoginMessageReceived(NetworkConnectionToClient conn, LoginMessage msg)
    {
        Debug.Log($"[Server] Login request: {msg.username}");

        try
        {
            bool valid = DatabaseManager.ValidateUser(msg.username, msg.password);

            if (!valid)
            {
                SendAuthResponse(conn, false, "Invalid username or password.");
                return;
            }

            conn.authenticationData = msg.username;

            Debug.Log($"[Server] Login OK for {msg.username}");

            // Étape 1 : répondre que l’authentification est OK
            SendAuthResponse(conn, true, "Login successful!");

            // Étape 2 : envoyer une redirection vers le HUB
            SendRedirect(conn);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Login error: {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // ==========================================================
    // =============== REDIRECTION VERS HUB =====================
    // ==========================================================

    private void SendRedirect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[Server] Redirecting player to HUB instance {HUB_IP}:{HUB_PORT}");

        conn.Send(new RedirectMessage
        {
            ip = HUB_IP,
            port = HUB_PORT
        });

        // IMPORTANT : NE PAS SPAWN DE PLAYER SUR LE MASTER !
        // NE PAS appeler AddPlayerForConnection ici.
    }

    // ==========================================================
    private void SendAuthResponse(NetworkConnectionToClient conn, bool success, string message)
    {
        conn.Send(new AuthResponseMessage
        {
            success = success,
            message = message
        });

        Debug.Log($"[Server → Client] AuthResponse : {message}");
    }
}
#endif
