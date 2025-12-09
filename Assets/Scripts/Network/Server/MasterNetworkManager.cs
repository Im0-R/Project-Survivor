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

        // IMPORTANT :
        // On n’appelle PAS AddPlayerForConnection(conn)
        // Le Master ne spawn PAS de joueurs.
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
