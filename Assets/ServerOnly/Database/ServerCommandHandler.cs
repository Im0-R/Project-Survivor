#if !UNITY_CLIENT
using Mirror;
using UnityEngine;
using AuthMessages;
using System.Net.Sockets;

public class ServerCommandHandler : MonoBehaviour
{
    public static ServerCommandHandler Instance { get; private set; }

    // Hub principal (ton instance Town)
    private const string HUB_IP = "72.60.212.58";
    private const int HUB_PORT = 8000;

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

    // =========================================================================
    // ============================ REGISTER ===================================
    // =========================================================================

    private void OnRegisterMessageReceived(NetworkConnectionToClient conn, RegisterMessage msg)
    {
        Debug.Log($"[MASTER] Register request: {msg.username}");

        try
        {
            if (DatabaseManager.GetUser(msg.username) != null)
            {
                SendAuthResponse(conn, false, "Username already taken.");
                return;
            }

            DatabaseManager.InsertUser(msg.username, msg.password);
            conn.authenticationData = msg.username;

            SendAuthResponse(conn, true, "Account created successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Register Error] {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // =========================================================================
    // ============================== LOGIN ====================================
    // =========================================================================

    private void OnLoginMessageReceived(NetworkConnectionToClient conn, LoginMessage msg)
    {
        Debug.Log($"[MASTER] Login request: {msg.username}");

        try
        {
            bool valid = DatabaseManager.ValidateUser(msg.username, msg.password);

            if (!valid)
            {
                SendAuthResponse(conn, false, "Invalid username or password.");
                return;
            }

            conn.authenticationData = msg.username;

            // Répondre SUCCESS
            SendAuthResponse(conn, true, "Login successful!");

            // Puis rediriger vers l’instance Hub
            RedirectToHub(conn);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Login Error] {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // =========================================================================
    // ========================= REDIRECTION VERS HUB ===========================
    // =========================================================================

    private void RedirectToHub(NetworkConnectionToClient conn)
    {
        Debug.Log($"[MASTER] Preparing redirect to HUB {HUB_IP}:{HUB_PORT}...");

        if (!IsPortOpen(HUB_IP, HUB_PORT))
        {
            Debug.LogError("[MASTER] ⚠ HUB INSTANCE IS DOWN — cannot redirect player.");
            SendAuthResponse(conn, false, "Hub instance unavailable. Try again later.");
            return;
        }

        Debug.Log("[MASTER] HUB is alive → sending redirect instruction.");

        // 1) Envoyer les infos de connexion au client
        conn.Send(new RedirectMessage
        {
            ip = HUB_IP,
            port = HUB_PORT
        });

        // 2) NE PAS SPAWN DE PLAYER ICI !!!
        // Le Master ne possède aucun joueur.

        // 3) Déconnecter le client proprement côté Master
        conn.Disconnect();
    }

    // =========================================================================
    // ============================ UTILITY ====================================
    // =========================================================================

    private bool IsPortOpen(string ip, int port)
    {
        try
        {
            using TcpClient client = new();
            var result = client.BeginConnect(ip, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(150);

            return success;
        }
        catch
        {
            return false;
        }
    }

    private void SendAuthResponse(NetworkConnectionToClient conn, bool success, string message)
    {
        conn.Send(new AuthResponseMessage
        {
            success = success,
            message = message
        });

        Debug.Log($"[MASTER → CLIENT] AuthResponse: {message}");
    }
}
#endif
