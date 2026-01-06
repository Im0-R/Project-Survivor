#if !UNITY_CLIENT
using Mirror;
using UnityEngine;
using AuthMessages;
using System;

public class ServerCommandHandler : MonoBehaviour
{
    public static ServerCommandHandler Instance { get; private set; }

    // Hub principal (Town)
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
        catch (Exception ex)
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
            RedirectToHub(conn, msg.username);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Login Error] {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // =========================================================================
    // ========================= HUB MANAGEMENT ===========================
    // =========================================================================

    private void RedirectToHub(NetworkConnectionToClient conn, string username)
    {
        Debug.Log($"[MASTER] Sending redirect to HUB {HUB_IP}:{HUB_PORT}...");

        conn.Send(new RedirectMessage
        {
            ip = HUB_IP,
            port = HUB_PORT
        });

        Debug.Log($"[MASTER → CLIENT] RedirectMessage: {HUB_IP}:{HUB_PORT}");

        StartCoroutine(DisconnectAfterDelay(conn, 0.25f));
    }

    private System.Collections.IEnumerator DisconnectAfterDelay(NetworkConnectionToClient conn, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (conn != null)
            conn.Disconnect();
    }


    // =========================================================================
    // ============================ UTILITY ====================================
    // =========================================================================

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
