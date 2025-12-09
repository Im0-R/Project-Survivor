using Mirror;
using UnityEngine;

public class InstanceNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[HUB] Hub instance started and ready for players!");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[HUB] Client connected to Hub instance!");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("[HUB] Spawning player in hub...");

        // This automatically instantiates playerPrefab
        base.OnServerAddPlayer(conn);

        Debug.Log("[HUB] Player spawned successfully.");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[HUB] Disconnected from hub.");
    }
}
