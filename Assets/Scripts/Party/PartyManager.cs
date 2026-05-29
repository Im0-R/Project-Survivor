using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyManager : NetworkBehaviour
{
    public static PartyManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void InvitePlayer(PlayerEntity inviter, uint targetNetId)
    {
        if (inviter == null)
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            return;

        PlayerEntity target = targetIdentity.GetComponent<PlayerEntity>();
        if (target == null)
            return;

        string inviterName = inviter.connectionToClient.authenticationData as string;
        string targetName = target.connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(inviterName) || string.IsNullOrWhiteSpace(targetName))
            return;

        DatabaseManager.CreateParty(inviterName, targetName);

        RefreshPartyUI(inviterName);
        RefreshPartyUI(targetName);

        Debug.Log($"[Party] Created/updated party: {inviterName} + {targetName}");
    }

    [Server]
    public void TeleportToPartyMember(PlayerEntity requester, uint targetNetId)
    {
        if (requester == null)
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            return;

        PlayerEntity target = targetIdentity.GetComponent<PlayerEntity>();
        if (target == null)
            return;

        string requesterName = requester.connectionToClient.authenticationData as string;
        string targetName = target.connectionToClient.authenticationData as string;

        if (!DatabaseManager.AreInSameParty(requesterName, targetName))
        {
            Debug.Log("[Party] Players are not in same party.");
            return;
        }

        PlayerLocationData location = DatabaseManager.GetPlayerLocation(targetName);

        if (location == null)
        {
            Debug.Log("[Party] Target location not found.");
            return;
        }

        requester.TargetSwitchToInstance(
            requester.connectionToClient,
            location.CurrentPort,
            location.CurrentScene
        );
    }

    [Server]
    public static void UpdateLocationFor(PlayerEntity player, int port)
    {
        if (player == null || player.connectionToClient == null)
            return;

        string username = player.connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(username))
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        DatabaseManager.UpdatePlayerLocation(username, port, sceneName);
    }

    [Server]
    public void RefreshPartyUI(string username)
    {
        string[] members = DatabaseManager.GetPartyMembers(username);

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null)
                continue;

            string connUsername = conn.authenticationData as string;

            if (connUsername == username)
            {
                PlayerEntity player = conn.identity.GetComponent<PlayerEntity>();
                if (player != null)
                    player.TargetReceivePartyMembers(conn, members);
            }
        }
    }
    [Server]
    public void TeleportToPartyMemberByName(PlayerEntity requester, string memberName)
    {
        if (requester == null)
            return;

        PlayerEntity target = FindPartyMemberByName(requester, memberName);

        if (target == null)
        {
            Debug.LogWarning($"[Party] Teleport failed. Target not found: {memberName}");
            return;
        }

        if (target == requester)
        {
            Debug.LogWarning("[Party] Teleport failed. Player tried to teleport to self.");
            return;
        }

        requester.transform.position = target.transform.position + target.transform.right * 1.5f;

        Debug.Log($"[Party] {requester.StatComp.Name} teleported to {target.StatComp.Name}");
    }

    [Server]
    private PlayerEntity FindPartyMemberByName(PlayerEntity requester, string memberName)
    {
        foreach (PlayerEntity player in FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None))
        {
            if (player == null || player.StatComp == null)
                continue;

            if (player.StatComp.Name == memberName)
                return player;
        }

        return null;
    }
}