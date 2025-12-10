using Mirror;
using UnityEngine;

public class MasterNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[MASTER] Serveur principal démarré.");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Debug.Log("[MASTER] Un client vient de se connecter.");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log("[MASTER] Un client s'est déconnecté.");
        base.OnServerDisconnect(conn);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[CLIENT] Connecté au serveur MASTER !");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[CLIENT] Déconnecté du serveur MASTER.");
    }
}
