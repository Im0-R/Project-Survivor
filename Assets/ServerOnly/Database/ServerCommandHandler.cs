#if UNITY_SERVER
using Mirror;
using UnityEngine;
using System;

public class ServerCommandHandler : NetworkBehaviour
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
    }

    // ==========================================================
    // ========== REGISTER / LOGIN HANDLERS =====================
    // ==========================================================

    [Server]
    public void HandleRegister(NetworkConnectionToClient conn, string username, string password)
    {
        try
        {
            if (DatabaseManager.GetUser(username) != null)
            {
                SendRegisterResponse(conn, false, "Username already taken.");
                return;
            }

            DatabaseManager.InsertUser(username, password);
            conn.authenticationData = username;

            Debug.Log($"[Server] User '{username}' registered successfully.");
            SendRegisterResponse(conn, true, "Account created successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Register error: {ex}");
            SendRegisterResponse(conn, false, "Internal server error.");
        }
    }

    [Server]
    public void HandleLogin(NetworkConnectionToClient conn, string username, string password)
    {
        try
        {
            bool valid = DatabaseManager.ValidateUser(username, password);
            if (!valid)
            {
                SendLoginResponse(conn, false, "Invalid username or password.");
                return;
            }

            conn.authenticationData = username;

            Debug.Log($"[Server] User '{username}' logged in.");
            SendLoginResponse(conn, true, "Login successful.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerCommandHandler] Login error: {ex}");
            SendLoginResponse(conn, false, "Internal server error.");
        }
    }

    // ==========================================================
    // ========== RESPONSE TO CLIENTS ===========================
    // ==========================================================
    [TargetRpc]
    private void SendRegisterResponse(NetworkConnection target, bool success, string message)
    {
        Debug.Log($"[Server -> Client] Register result: {message}");
    }

    [TargetRpc]
    private void SendLoginResponse(NetworkConnection target, bool success, string message)
    {
        Debug.Log($"[Server -> Client] Login result: {message}");
    }
}
#endif
