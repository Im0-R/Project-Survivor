#if UNITY_SERVER || UNITY_EDITOR
using Mirror;
using System;
using UnityEngine;
using static AuthMessages;

// ======================
// SERVER-SIDE AUTH HANDLER
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

    // ============================
    // REGISTER
    // ============================
    private void OnRegisterMessageReceived(NetworkConnectionToClient conn, RegisterMessage msg)
    {
        Debug.Log($"[Server] Register request from {conn.connectionId}: {msg.username}");

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

    // ============================
    // LOGIN
    // ============================
    private void OnLoginMessageReceived(NetworkConnectionToClient conn, LoginMessage msg)
    {
        Debug.Log($"[Server] Login request from {conn.connectionId}: {msg.username}");

        try
        {
            bool valid = DatabaseManager.ValidateUser(msg.username, msg.password);
            if (!valid)
            {
                SendAuthResponse(conn, false, "Invalid username or password.");
                return;
            }

            conn.authenticationData = msg.username;

            // Already has player
            if (conn.identity != null)
            {
                SendAuthResponse(conn, true, "Already logged in.");
                return;
            }

            // Spawn the player
            GameObject prefab = NetworkManager.singleton.playerPrefab;
            GameObject instance = Instantiate(prefab);

            NetworkServer.AddPlayerForConnection(conn, instance);

            Debug.Log($"[Server] Spawned player for '{msg.username}'.");
            SendAuthResponse(conn, true, "Login successful!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Login error: {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    // ============================
    // SEND RESPONSE
    // ============================
    private void SendAuthResponse(NetworkConnectionToClient conn, bool success, string message)
    {
        conn.Send(new AuthResponseMessage
        {
            success = success,
            message = message
        });

        Debug.Log($"[Server → Client] Auth response sent: {message}");
    }
}
#endif
