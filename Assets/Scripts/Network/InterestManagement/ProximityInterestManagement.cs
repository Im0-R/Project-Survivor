using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ProximityInterestManagement : InterestManagement
{
    [Tooltip("Visibility Range .")]
    public float visRange = 30f;

    [Tooltip("Global rebuild every X Seconds.")]
    public float rebuildInterval = 1f;

    double lastRebuildTime;

    //Called when a client/newObserver spawns:'
    public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient newObserver)
    {
        if (newObserver == null || newObserver.identity == null) return false;
        return Vector3.Distance(identity.transform.position,
                                newObserver.identity.transform.position) <= visRange;
    }

    // Rebuild the observers list for 'the NetworkIndentity'
    public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers)
    {
        Vector3 pos = identity.transform.position;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isAuthenticated || conn.identity == null) continue;

            // Add the client if within visibility range

            if (Vector3.Distance(conn.identity.transform.position, pos) <= visRange)
                newObservers.Add(conn);
        }
    }

    [ServerCallback]
    void Update()
    {
        if (NetworkTime.time >= lastRebuildTime + rebuildInterval)
        {
            RebuildAll();
            lastRebuildTime = NetworkTime.time;
        }
    }
}
