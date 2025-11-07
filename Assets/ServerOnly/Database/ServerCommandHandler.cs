#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using UnityEngine;
using System;

// ======================
// MESSAGES DEFINITIONS
// ======================
public struct RegisterMessage : NetworkMessage
{
    public string username;
    public string password;
}

public struct LoginMessage : NetworkMessage
{
    public string username;
    public string password;
}

public struct AuthResponseMessage : NetworkMessage
{
    public bool success;
    public string message;
}

// ======================
// SERVER HANDLER
// ======================
public class ServerCommandHandler : MonoBehaviour
{
    public static ServerCommandHandler Instance { get; private set; }

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
    // ========== REGISTER / LOGIN LOGIC =========================
    // ==========================================================

    private void OnRegisterMessageReceived(NetworkConnectionToClient conn, RegisterMessage msg)
    {
        Debug.Log($"[Server] Received register message from {conn.connectionId}");
        try
        {
            Debug.Log($"[Server] Register request from {conn.connectionId}: {msg.username}");

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
        catch (Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Register error: {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    private void OnLoginMessageReceived(NetworkConnectionToClient conn, LoginMessage msg)
    {
        Debug.Log($"[Server] Received login message from {conn.connectionId}");
        try
        {
            Debug.Log($"[Server] Login request from {conn.connectionId}: {msg.username}");

            bool valid = DatabaseManager.ValidateUser(msg.username, msg.password);
            if (!valid)
            {
                SendAuthResponse(conn, false, "Invalid username or password.");
                return;
            }

            conn.authenticationData = msg.username;

            // no duplicate players per connection
            if (conn.identity != null)
            {
                Debug.LogWarning($"[Server] Connection {conn.connectionId} already has a player!");
                SendAuthResponse(conn, true, "Already logged in.");
                return;
            }

            //spawn player object 
            GameObject playerPrefab = NetworkManager.singleton.playerPrefab;
            GameObject playerInstance = Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, playerInstance);
            Debug.Log($"[Server] Player '{msg.username}' spawned for connection {conn.connectionId}.");

            SendAuthResponse(conn, true, "Login successful!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Login error: {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // ==========================================================
    // ========== RESPONSE TO CLIENT ============================
    // ==========================================================
    private void SendAuthResponse(NetworkConnectionToClient conn, bool success, string message)
    {
        conn.Send(new AuthResponseMessage
        {
            success = success,
            message = message
        });

        Debug.Log($"[Server → Client] Auth response: {message}");
    }
}
#endif
