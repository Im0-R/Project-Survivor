#if !UNITY_CLIENT
using Mirror;
using UnityEngine;
using AuthMessages;
using System;

public class ServerCommandHandler : MonoBehaviour
{
    public static ServerCommandHandler Instance { get; private set; }

    private const string HUB_IP = "72.60.212.58";
    private const int HUB_PORT = 8000;

    private void Awake()
    {
        if (!NetworkServer.active && !Application.isBatchMode)
        {
            gameObject.SetActive(false);
            return;
        }

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

            SendAuthResponse(conn, true, "Login successful!");
            RedirectToHub(conn, msg.username);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Login Error] {ex}");
            SendAuthResponse(conn, false, "Internal server error.");
        }
    }

    private void RedirectToHub(NetworkConnectionToClient conn, string username)
    {
        Debug.Log($"[MASTER] Sending redirect to HUB {HUB_IP}:{HUB_PORT} for {username}...");

        conn.Send(new RedirectMessage
        {
            ip = HUB_IP,
            port = HUB_PORT,
            username = username
        });

        Debug.Log($"[MASTER -> CLIENT] RedirectMessage: {HUB_IP}:{HUB_PORT} username={username}");
    }

    private void SendAuthResponse(NetworkConnectionToClient conn, bool success, string message)
    {
        conn.Send(new AuthResponseMessage
        {
            success = success,
            message = message
        });

        Debug.Log($"[MASTER -> CLIENT] AuthResponse: {message}");
    }
}
#endif