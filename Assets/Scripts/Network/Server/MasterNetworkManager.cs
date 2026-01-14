using Mirror;
using UnityEngine;

public class MasterNetworkManager : NetworkManager
{
    void Awake()
    {
#if !UNITY_SERVER
        //we deactivate the MasterNetworkManager if not a server build
        gameObject.SetActive(false);
        return;
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[MASTER] Master server started.");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Debug.Log("[MASTER] a client has connected.");
        base.OnServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log("[MASTER] a client has disconnected.");
        base.OnServerDisconnect(conn);
    }
}
