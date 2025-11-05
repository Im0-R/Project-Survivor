using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/*
    Authenticator Login System
    - Envoie un username + mot de passe
    - Vérifie côté serveur
    - Accepte ou refuse la connexion
*/

public class AuthenticatorLogin : NetworkAuthenticator
{
    #region Messages

    public struct AuthRequestMessage : NetworkMessage
    {
        public string username;
        public string password;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public bool success;
        public string message;
    }

    #endregion

    // Exemple : stockage d’utilisateurs valides
    // (tu remplaceras plus tard par une DB ou un fichier JSON)
    private Dictionary<string, string> validUsers = new Dictionary<string, string>()
    {
        { "Imogen", "1234" },
        { "Admin", "root" },
        { "TestUser", "password" }
    };

    #region SERVER

    public override void OnStartServer()
    {
        // On écoute les messages d’authentification
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        // Rien à faire ici, on attend que le client envoie le AuthRequestMessage
    }

    private void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        Debug.Log($"[Authenticator] Auth request from user: {msg.username}");

        bool success = false;
        string responseMessage;

        if (validUsers.TryGetValue(msg.username, out string correctPassword))
        {
            if (msg.password == correctPassword)
            {
                success = true;
                responseMessage = "Authentication successful.";
                Debug.Log($"[Authenticator] {msg.username} logged in successfully.");
            }
            else
            {
                responseMessage = "Incorrect password.";
                Debug.LogWarning($"[Authenticator] {msg.username} tried to connect with wrong password.");
            }
        }
        else
        {
            responseMessage = "Unknown username.";
            Debug.LogWarning($"[Authenticator] {msg.username} not found.");
        }

        // Envoi de la réponse au client
        conn.Send(new AuthResponseMessage
        {
            success = success,
            message = responseMessage
        });

        // Si validé -> acceptation
        if (success)
        {
            ServerAccept(conn);
        }
        else
        {
            // Sinon rejet (avec un petit délai pour éviter spam)
            _ = DelayedDisconnect(conn, 1.5f, responseMessage);
        }
    }

    private async System.Threading.Tasks.Task DelayedDisconnect(NetworkConnectionToClient conn, float delay, string reason)
    {
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delay));

        Debug.Log($"[Authenticator] Disconnecting {conn.address}: {reason}");
        conn.Disconnect();
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<AuthRequestMessage>();
    }

    #endregion

    #region CLIENT

    public override void OnStartClient()
    {
        //Register the handler for the response message
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }

    public override void OnClientAuthenticate()
    {
        string username = PlayerPrefs.GetString("username", "Imogen");
        string password = PlayerPrefs.GetString("password", "1234");

        Debug.Log($"[Authenticator] Sending credentials: {username}/{password}");

        NetworkClient.Send(new AuthRequestMessage
        {
            username = username,
            password = password
        });
    }

    private void OnAuthResponseMessage(AuthResponseMessage msg)
    {
        if (msg.success)
        {
            Debug.Log($"[Authenticator] Server accepted login: {msg.message}");
            ClientAccept();
        }
        else
        {
            Debug.LogWarning($"[Authenticator] Server rejected login: {msg.message}");
            ClientReject();
        }
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<AuthResponseMessage>();
    }

    #endregion
}
