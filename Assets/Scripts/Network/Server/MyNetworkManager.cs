using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Connecté au serveur !");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("Déconnecté du serveur.");
    }
}
