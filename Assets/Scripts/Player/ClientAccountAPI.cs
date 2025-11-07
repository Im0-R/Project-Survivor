using Mirror;
using UnityEngine;

public class ClientAccountAPI : NetworkBehaviour
{
    public void TryRegister(string username, string password)
    {
        if (!NetworkClient.active)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        CmdRequestRegister(username, password);
    }

    public void TryLogin(string username, string password)
    {
        if (!NetworkClient.active)
        {
            Debug.LogError("[ClientAccountAPI] Not connected to server.");
            return;
        }

        CmdRequestLogin(username, password);
    }

    // ============================================
    // Commands — envoyées au serveur
    // ============================================
    [Command]
    private void CmdRequestRegister(string username, string password, NetworkConnectionToClient sender = null)
    {
#if UNITY_SERVER
        ServerCommandHandler.Instance?.HandleRegister(sender, username, password);
#endif
    }

    [Command]
    private void CmdRequestLogin(string username, string password, NetworkConnectionToClient sender = null)
    {
#if UNITY_SERVER
        ServerCommandHandler.Instance?.HandleLogin(sender, username, password);
#endif
    }
}
